# Thêm dashboard /dashboard/recipes vào repo CulinaryBlog.
# Cách chạy (từ thư mục repo):
#   cd D:\CulinaryBlog
#   powershell -ExecutionPolicy Bypass -File .\add-dashboard.ps1
# File nào bị ghi đè hoặc sửa đều được backup thành *.bak bên cạnh.

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
if (-not (Test-Path (Join-Path $root 'src\backend'))) { $root = (Get-Location).Path }
$be = Join-Path $root 'src\backend'
if (-not (Test-Path $be)) { throw "Không thấy src\backend. Hãy chạy script trong D:\CulinaryBlog." }
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Backup($path) {
  if (Test-Path -LiteralPath $path) {
    Copy-Item -LiteralPath $path -Destination "$path.bak" -Force
    Write-Host "    backup: $path.bak" -ForegroundColor DarkGray
  }
}
function Write-Code($path, $content) {
  $dir = Split-Path $path -Parent
  if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
  Backup $path
  [IO.File]::WriteAllText($path, ($content -replace "`r?`n", "`r`n"), $utf8)
  Write-Host "[TẠO] $path" -ForegroundColor Green
}
function Save-Text($path, $content) {
  Backup $path
  [IO.File]::WriteAllText($path, $content, $utf8)
  Write-Host "[SỬA] $path" -ForegroundColor Yellow
}
function Find-Cs($dir, $pattern) {
  # Đọc cả file (không theo từng dòng) để bắt được khai báo class xuống dòng
  Get-ChildItem -LiteralPath $dir -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and ([IO.File]::ReadAllText($_.FullName) -match $pattern) }
}
function Get-Ns($text) { [regex]::Match($text, '(?m)^\s*namespace\s+([\w\.]+)').Groups[1].Value }

# ------------------------------------------------------------------ nội dung file
$appCode = @'
using CulinaryBlog.Domain;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application;

// DTO riêng cho dashboard tác giả: khác RecipeSummaryDto ở chỗ có UpdatedAt và RowVersion
// (cần RowVersion để xoá/sửa từ dashboard mà không ghi đè thay đổi của phiên khác).
public sealed record MyRecipeSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    Guid CategoryId,
    string CategoryName,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    string Difficulty,
    string Status,
    string? PrimaryImageUrl,
    int IngredientCount,
    int StepCount,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string RowVersion);

public sealed record MyRecipeCountsDto(int All, IReadOnlyDictionary<string, int> ByStatus);

// AuthorId KHÔNG bind từ query string: endpoint gán từ claim của token.
public sealed record GetMyRecipesQuery(
    string AuthorId,
    int Page = 1,
    int PageSize = 20,
    string SortBy = "updatedAt",
    string SortOrder = "desc",
    string? Status = null,
    string? Q = null) : IRequest<PagedResult<MyRecipeSummaryDto>>;

public sealed record GetMyRecipeCountsQuery(string AuthorId) : IRequest<MyRecipeCountsDto>;

public interface IMyRecipesRepository
{
    Task<PagedResult<MyRecipeSummaryDto>> GetByAuthorAsync(GetMyRecipesQuery query, CancellationToken ct);
    Task<MyRecipeCountsDto> CountByAuthorAsync(string authorId, CancellationToken ct);
}

public sealed class GetMyRecipesHandler(IMyRecipesRepository repository)
    : IRequestHandler<GetMyRecipesQuery, PagedResult<MyRecipeSummaryDto>>
{
    public Task<PagedResult<MyRecipeSummaryDto>> Handle(GetMyRecipesQuery request, CancellationToken ct) =>
        repository.GetByAuthorAsync(request, ct);
}

public sealed class GetMyRecipeCountsHandler(IMyRecipesRepository repository)
    : IRequestHandler<GetMyRecipeCountsQuery, MyRecipeCountsDto>
{
    public Task<MyRecipeCountsDto> Handle(GetMyRecipeCountsQuery request, CancellationToken ct) =>
        repository.CountByAuthorAsync(request.AuthorId, ct);
}

public sealed class GetMyRecipesValidator : AbstractValidator<GetMyRecipesQuery>
{
    public static readonly string[] AllowedSortFields = ["updatedAt", "createdAt", "title", "publishedAt"];
    private static readonly string[] AllowedSortOrders = ["asc", "desc"];

    public GetMyRecipesValidator()
    {
        RuleFor(x => x.AuthorId)
            .NotEmpty().WithMessage("Không xác định được tác giả.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Trang phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Số bản ghi trên trang phải từ 1 đến 50.");

        RuleFor(x => x.SortBy)
            .Must(x => AllowedSortFields.Contains(x.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Trường sắp xếp không hợp lệ. Cho phép: {string.Join(", ", AllowedSortFields)}.");

        RuleFor(x => x.SortOrder)
            .Must(x => AllowedSortOrders.Contains(x.Trim().ToLowerInvariant()))
            .WithMessage("Thứ tự sắp xếp chỉ chấp nhận 'asc' hoặc 'desc'.");

        When(x => !string.IsNullOrWhiteSpace(x.Status), () =>
        {
            RuleFor(x => x.Status)
                .Must(x => Enum.TryParse<RecipeStatus>(x!.Trim(), ignoreCase: true, out _))
                .WithMessage($"Trạng thái không hợp lệ. Cho phép: {string.Join(", ", Enum.GetNames<RecipeStatus>())}.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Q), () =>
        {
            RuleFor(x => x.Q)
                .MaximumLength(200).WithMessage("Từ khóa tìm kiếm không được vượt quá 200 ký tự.");
        });
    }
}
'@
$infCode = @'
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure;

// Các dòng đánh dấu "ĐỐI CHIẾU" là chỗ giả định tên DbContext / navigation.
// Nếu khác, lấy đúng tên từ RecipeDiscoveryRepository (projection của discovery đã chạy được).
public sealed class MyRecipesRepository(ApplicationDbContext db) : IMyRecipesRepository // ĐỐI CHIẾU: tên DbContext
{
    public async Task<PagedResult<MyRecipeSummaryDto>> GetByAuthorAsync(GetMyRecipesQuery query, CancellationToken ct)
    {
        // Global query filter IsDeleted (ADR-0001) tự loại bản ghi đã xoá mềm.
        var recipes = db.Recipes.AsNoTracking().Where(r => r.AuthorId == query.AuthorId);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = Enum.Parse<RecipeStatus>(query.Status.Trim(), ignoreCase: true);
            recipes = recipes.Where(r => r.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = "%" + EscapeLike(query.Q.Trim()) + "%";
            recipes = recipes.Where(r => EF.Functions.ILike(r.Title, pattern, "\\"));
        }

        var total = await recipes.CountAsync(ct);

        var desc = query.SortOrder.Trim().Equals("desc", StringComparison.OrdinalIgnoreCase);
        recipes = query.SortBy.Trim().ToLowerInvariant() switch
        {
            "title" => desc ? recipes.OrderByDescending(r => r.Title) : recipes.OrderBy(r => r.Title),
            "createdat" => desc ? recipes.OrderByDescending(r => r.CreatedAt) : recipes.OrderBy(r => r.CreatedAt),
            "publishedat" => desc ? recipes.OrderByDescending(r => r.PublishedAt) : recipes.OrderBy(r => r.PublishedAt),
            // UpdatedAt nullable (SRS 7.1): bản chưa sửa lần nào lấy CreatedAt để xếp.
            _ => desc
                ? recipes.OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
                : recipes.OrderBy(r => r.UpdatedAt ?? r.CreatedAt),
        };
        recipes = ((IOrderedQueryable<Recipe>)recipes).ThenBy(r => r.Id); // phân trang ổn định

        // Chiếu enum/bytea thô trước, chuyển sang chuỗi sau khi materialize
        // để không phụ thuộc vào việc EF dịch được Enum.ToString() hay không.
        var rows = await recipes
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.Slug,
                r.CategoryId,
                CategoryName = r.Category.Name,
                r.PrepTimeMinutes,
                r.CookTimeMinutes,
                r.Servings,
                r.Difficulty,
                r.Status,
                // ĐỐI CHIẾU: copy đúng biểu thức PrimaryImageUrl từ RecipeDiscoveryRepository
                PrimaryImageUrl = r.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                IngredientCount = r.Ingredients.Count(),
                StepCount = r.Steps.Count(),
                r.PublishedAt,
                r.CreatedAt,
                r.UpdatedAt,
                r.RowVersion,
            })
            .ToListAsync(ct);

        var items = rows.Select(x => new MyRecipeSummaryDto(
            x.Id, x.Title, x.Slug, x.CategoryId, x.CategoryName,
            x.PrepTimeMinutes, x.CookTimeMinutes, x.Servings,
            x.Difficulty.ToString(), x.Status.ToString(),
            x.PrimaryImageUrl, x.IngredientCount, x.StepCount,
            x.PublishedAt, x.CreatedAt, x.UpdatedAt,
            Convert.ToBase64String(x.RowVersion))).ToList();

        return new PagedResult<MyRecipeSummaryDto>(items, query.Page, query.PageSize, total); // ĐỐI CHIẾU: ctor PagedResult
    }

    public async Task<MyRecipeCountsDto> CountByAuthorAsync(string authorId, CancellationToken ct)
    {
        var grouped = await db.Recipes.AsNoTracking()
            .Where(r => r.AuthorId == authorId)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Trả đủ mọi trạng thái (kể cả 0) để frontend không phải tự điền.
        var byStatus = Enum.GetValues<RecipeStatus>()
            .ToDictionary(
                s => s.ToString(),
                s => grouped.FirstOrDefault(g => g.Status == s)?.Count ?? 0);

        return new MyRecipeCountsDto(byStatus.Values.Sum(), byStatus);
    }

    private static string EscapeLike(string input) =>
        input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
'@
$apiCode = @'
using System.Security.Claims;
using CulinaryBlog.Application;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CulinaryBlog.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me/recipes")] // ĐỐI CHIẾU: prefix route với các controller C2 (vd. api/v1/...)
public sealed class MyRecipesController(ISender sender) : ControllerBase
{
    // GET /api/me/recipes?page=1&pageSize=20&sortBy=updatedAt&sortOrder=desc&status=Draft&q=phở
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "updatedAt",
        [FromQuery] string sortOrder = "desc",
        [FromQuery] string? status = null,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        var authorId = CurrentUserId();
        if (authorId is null) return Unauthorized();

        var result = await sender.Send(
            new GetMyRecipesQuery(authorId, page, pageSize, sortBy, sortOrder, status, q), ct);

        return Ok(new { data = result }); // C08
    }

    // GET /api/me/recipes/counts
    [HttpGet("counts")]
    public async Task<IActionResult> Counts(CancellationToken ct)
    {
        var authorId = CurrentUserId();
        if (authorId is null) return Unauthorized();

        var result = await sender.Send(new GetMyRecipeCountsQuery(authorId), ct);
        return Ok(new { data = result });
    }

    // ĐỐI CHIẾU: nếu C2 đã có ICurrentUserService thì dùng nó thay cho hàm này.
    private string? CurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
}
'@
$libCode = @'
// Kiểu khớp với MyRecipeSummaryDto / MyRecipeCountsDto bên backend.
// difficulty/status là chuỗi (backend đã ToString()).

export type RecipeStatus = string; // lấy danh sách thực tế từ /counts, không hardcode
export type Difficulty = "Easy" | "Medium" | "Hard" | "Expert";

export interface MyRecipeSummary {
  id: string;
  title: string;
  slug: string;
  categoryId: string;
  categoryName: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: Difficulty;
  status: RecipeStatus;
  primaryImageUrl: string | null;
  ingredientCount: number;
  stepCount: number;
  publishedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
  rowVersion: string;
}

// ĐỐI CHIẾU: tên field theo PagedResult<T> thực tế (items/totalCount/...).
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface MyRecipeCounts {
  all: number;
  byStatus: Record<string, number>;
}

export interface MyRecipesParams {
  page: number;
  pageSize: number;
  sortBy: "updatedAt" | "createdAt" | "title" | "publishedAt";
  sortOrder: "asc" | "desc";
  status?: string;
  q?: string;
}

export class ApiError extends Error {
  constructor(public status: number, public code: string | undefined, message: string) {
    super(message);
  }
}

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

// ĐỐI CHIẾU: nếu đã có client dùng chung từ C5 (tự refresh khi 401 rồi retry),
// thay hàm này bằng client đó. Bản dưới chỉ gắn Bearer và unwrap { data }.
async function authFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = typeof window !== "undefined" ? localStorage.getItem("accessToken") : null;
  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      Accept: "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  });

  if (res.status === 204) return undefined as T;

  const body = await res.json().catch(() => null);
  if (!res.ok) {
    throw new ApiError(res.status, body?.code ?? body?.error?.code, body?.message ?? body?.title ?? res.statusText);
  }
  return body?.data as T; // C08
}

export function getMyRecipes(p: MyRecipesParams) {
  const qs = new URLSearchParams({
    page: String(p.page),
    pageSize: String(p.pageSize),
    sortBy: p.sortBy,
    sortOrder: p.sortOrder,
  });
  if (p.status) qs.set("status", p.status);
  if (p.q?.trim()) qs.set("q", p.q.trim());
  return authFetch<PagedResult<MyRecipeSummary>>(`/api/me/recipes?${qs}`);
}

export function getMyRecipeCounts() {
  return authFetch<MyRecipeCounts>("/api/me/recipes/counts");
}

// ĐỐI CHIẾU: cách endpoint DELETE của C2 nhận rowVersion (header If-Match hay query/body).
export function deleteRecipe(id: string, rowVersion: string) {
  return authFetch<void>(`/api/recipes/${id}`, {
    method: "DELETE",
    headers: { "If-Match": rowVersion },
  });
}
'@
$pageCode = @'
"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import {
  ApiError,
  deleteRecipe,
  getMyRecipeCounts,
  getMyRecipes,
  type MyRecipeCounts,
  type MyRecipesParams,
  type MyRecipeSummary,
  type PagedResult,
} from "@/lib/my-recipes";

const STATUS_LABEL: Record<string, string> = {
  Draft: "Bản nháp",
  Published: "Đã đăng",
  Archived: "Lưu trữ",
};
const DIFFICULTY_LABEL: Record<string, string> = {
  Easy: "Dễ",
  Medium: "Trung bình",
  Hard: "Khó",
  Expert: "Chuyên gia",
};
const STATUS_STYLE: Record<string, string> = {
  Draft: "bg-amber-100 text-amber-900",
  Published: "bg-emerald-100 text-emerald-900",
  Archived: "bg-stone-200 text-stone-700",
};

const PAGE_SIZE = 20;
const fmtDate = (iso: string | null) =>
  iso ? new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" }) : "—";

export default function DashboardRecipesPage() {
  const router = useRouter();

  const [params, setParams] = useState<MyRecipesParams>({
    page: 1,
    pageSize: PAGE_SIZE,
    sortBy: "updatedAt",
    sortOrder: "desc",
  });
  const [searchInput, setSearchInput] = useState("");
  const [data, setData] = useState<PagedResult<MyRecipeSummary> | null>(null);
  const [counts, setCounts] = useState<MyRecipeCounts | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const handleAuth = useCallback(
    (e: unknown) => {
      if (e instanceof ApiError && e.status === 401) {
        router.replace("/login?returnUrl=/dashboard/recipes");
        return true;
      }
      return false;
    },
    [router],
  );

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [list, c] = await Promise.all([getMyRecipes(params), getMyRecipeCounts()]);
      setData(list);
      setCounts(c);
    } catch (e) {
      if (!handleAuth(e)) setError(e instanceof Error ? e.message : "Không tải được danh sách công thức.");
    } finally {
      setLoading(false);
    }
  }, [params, handleAuth]);

  useEffect(() => {
    void load();
  }, [load]);

  // Debounce ô tìm kiếm 350ms
  useEffect(() => {
    const t = setTimeout(() => {
      setParams((p) => (p.q === searchInput ? p : { ...p, q: searchInput, page: 1 }));
    }, 350);
    return () => clearTimeout(t);
  }, [searchInput]);

  const setStatus = (status?: string) => setParams((p) => ({ ...p, status, page: 1 }));

  const toggleSort = (field: MyRecipesParams["sortBy"]) =>
    setParams((p) => ({
      ...p,
      sortBy: field,
      sortOrder: p.sortBy === field && p.sortOrder === "desc" ? "asc" : "desc",
      page: 1,
    }));

  async function onDelete(r: MyRecipeSummary) {
    if (!confirm(`Xoá công thức "${r.title}"?`)) return;
    setDeletingId(r.id);
    setNotice(null);
    try {
      await deleteRecipe(r.id, r.rowVersion);
      setNotice(`Đã xoá "${r.title}".`);
      // Nếu xoá bản ghi cuối của trang > 1 thì lùi về trang trước
      if (data && data.items.length === 1 && params.page > 1) {
        setParams((p) => ({ ...p, page: p.page - 1 }));
      } else {
        await load();
      }
    } catch (e) {
      if (handleAuth(e)) return;
      if (e instanceof ApiError && e.status === 409) {
        setNotice(`"${r.title}" vừa được sửa ở nơi khác. Danh sách đã tải lại, hãy thử xoá lần nữa.`);
        await load();
      } else {
        setNotice(e instanceof Error ? e.message : "Xoá không thành công.");
      }
    } finally {
      setDeletingId(null);
    }
  }

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;
  const statuses = counts ? Object.keys(counts.byStatus) : [];
  const sortMark = (f: MyRecipesParams["sortBy"]) =>
    params.sortBy === f ? (params.sortOrder === "desc" ? " ↓" : " ↑") : "";

  return (
    <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
      <header className="mb-6 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-stone-900">Công thức của tôi</h1>
          <p className="mt-1 text-sm text-stone-600">
            {counts ? `${counts.all} công thức` : "Đang tải…"}
          </p>
        </div>
        <Link
          href="/dashboard/recipes/new"
          className="rounded-md bg-stone-900 px-4 py-2 text-sm font-medium text-white hover:bg-stone-700 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-stone-900"
        >
          Viết công thức mới
        </Link>
      </header>

      {/* Tabs trạng thái lấy từ /counts để khớp enum backend */}
      <nav className="mb-4 flex flex-wrap gap-2" aria-label="Lọc theo trạng thái">
        <TabButton active={!params.status} onClick={() => setStatus(undefined)}>
          Tất cả {counts && <Count n={counts.all} />}
        </TabButton>
        {statuses.map((s) => (
          <TabButton key={s} active={params.status === s} onClick={() => setStatus(s)}>
            {STATUS_LABEL[s] ?? s} <Count n={counts!.byStatus[s]} />
          </TabButton>
        ))}
      </nav>

      <div className="mb-4">
        <label htmlFor="recipe-search" className="sr-only">Tìm theo tên</label>
        <input
          id="recipe-search"
          type="search"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          placeholder="Tìm theo tên công thức"
          maxLength={200}
          className="w-full max-w-sm rounded-md border border-stone-300 px-3 py-2 text-sm focus:border-stone-900 focus:outline-none focus:ring-1 focus:ring-stone-900"
        />
      </div>

      {notice && (
        <p role="status" className="mb-4 rounded-md bg-stone-100 px-3 py-2 text-sm text-stone-800">
          {notice}
        </p>
      )}

      {error ? (
        <div role="alert" className="rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-800">
          {error}{" "}
          <button onClick={() => void load()} className="font-medium underline">
            Tải lại
          </button>
        </div>
      ) : loading && !data ? (
        <p className="py-12 text-center text-sm text-stone-500">Đang tải danh sách…</p>
      ) : data && data.items.length === 0 ? (
        <EmptyState filtered={!!params.status || !!params.q} />
      ) : data ? (
        <div className={loading ? "opacity-60 transition-opacity" : ""}>
          <div className="overflow-x-auto rounded-lg border border-stone-200">
            <table className="w-full min-w-[720px] text-left text-sm">
              <thead className="bg-stone-50 text-stone-600">
                <tr>
                  <Th onClick={() => toggleSort("title")}>Công thức{sortMark("title")}</Th>
                  <th className="px-4 py-3 font-medium">Trạng thái</th>
                  <th className="px-4 py-3 font-medium">Nội dung</th>
                  <Th onClick={() => toggleSort("updatedAt")}>Cập nhật{sortMark("updatedAt")}</Th>
                  <th className="px-4 py-3 font-medium"><span className="sr-only">Thao tác</span></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-stone-100">
                {data.items.map((r) => (
                  <tr key={r.id} className="align-top">
                    <td className="px-4 py-3">
                      <div className="flex gap-3">
                        {r.primaryImageUrl ? (
                          // eslint-disable-next-line @next/next/no-img-element
                          <img src={r.primaryImageUrl} alt="" className="h-12 w-16 shrink-0 rounded object-cover" />
                        ) : (
                          <div className="h-12 w-16 shrink-0 rounded bg-stone-100" aria-hidden />
                        )}
                        <div>
                          <p className="font-medium text-stone-900">{r.title}</p>
                          <p className="text-xs text-stone-500">
                            {r.categoryName} | {DIFFICULTY_LABEL[r.difficulty] ?? r.difficulty} |{" "}
                            {r.prepTimeMinutes + r.cookTimeMinutes} phút
                          </p>
                        </div>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <span className={`inline-block rounded px-2 py-0.5 text-xs font-medium ${STATUS_STYLE[r.status] ?? "bg-stone-100 text-stone-700"}`}>
                        {STATUS_LABEL[r.status] ?? r.status}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-stone-700">
                      {r.ingredientCount} nguyên liệu, {r.stepCount} bước
                      {/* C02: publish cần >=1 nguyên liệu và >=1 bước */}
                      {r.status === "Draft" && (r.ingredientCount === 0 || r.stepCount === 0) && (
                        <p className="mt-0.5 text-xs text-amber-700">Chưa đủ điều kiện đăng</p>
                      )}
                    </td>
                    <td className="px-4 py-3 text-stone-700">{fmtDate(r.updatedAt ?? r.createdAt)}</td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end gap-3 whitespace-nowrap">
                        {r.status === "Published" && (
                          <Link href={`/recipes/${r.slug}`} className="text-stone-700 underline-offset-2 hover:underline">
                            Xem
                          </Link>
                        )}
                        <Link href={`/dashboard/recipes/${r.id}/edit`} className="font-medium text-stone-900 underline-offset-2 hover:underline">
                          Sửa
                        </Link>
                        <button
                          onClick={() => void onDelete(r)}
                          disabled={deletingId === r.id}
                          className="text-red-700 underline-offset-2 hover:underline disabled:opacity-50"
                        >
                          {deletingId === r.id ? "Đang xoá…" : "Xoá"}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <footer className="mt-4 flex items-center justify-between text-sm text-stone-600">
            <span>
              Trang {data.page}/{totalPages}, {data.totalCount} kết quả
            </span>
            <div className="flex gap-2">
              <PageButton disabled={params.page <= 1} onClick={() => setParams((p) => ({ ...p, page: p.page - 1 }))}>
                Trang trước
              </PageButton>
              <PageButton disabled={params.page >= totalPages} onClick={() => setParams((p) => ({ ...p, page: p.page + 1 }))}>
                Trang sau
              </PageButton>
            </div>
          </footer>
        </div>
      ) : null}
    </main>
  );
}

function TabButton({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      onClick={onClick}
      aria-pressed={active}
      className={`rounded-full px-3 py-1.5 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-stone-900 ${
        active ? "bg-stone-900 text-white" : "bg-stone-100 text-stone-700 hover:bg-stone-200"
      }`}
    >
      {children}
    </button>
  );
}

function Count({ n }: { n: number }) {
  return <span className="ml-1 tabular-nums opacity-70">{n}</span>;
}

function Th({ onClick, children }: { onClick: () => void; children: React.ReactNode }) {
  return (
    <th className="px-4 py-3 font-medium">
      <button onClick={onClick} className="hover:text-stone-900">{children}</button>
    </th>
  );
}

function PageButton({ disabled, onClick, children }: { disabled: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className="rounded-md border border-stone-300 px-3 py-1.5 hover:bg-stone-50 disabled:cursor-not-allowed disabled:opacity-40"
    >
      {children}
    </button>
  );
}

function EmptyState({ filtered }: { filtered: boolean }) {
  return (
    <div className="rounded-lg border border-dashed border-stone-300 px-6 py-12 text-center">
      {filtered ? (
        <p className="text-sm text-stone-600">Không có công thức nào khớp bộ lọc. Thử bỏ bớt điều kiện tìm kiếm.</p>
      ) : (
        <>
          <p className="text-stone-800">Bạn chưa có công thức nào.</p>
          <Link href="/dashboard/recipes/new" className="mt-3 inline-block text-sm font-medium underline">
            Viết công thức đầu tiên
          </Link>
        </>
      )}
    </div>
  );
}
'@

# ------------------------------------------------------------------ 1. Application
Write-Host "`n== Application ==" -ForegroundColor Cyan
$appDir = Join-Path $be 'CulinaryBlog.Application'
$discovery = Get-ChildItem -LiteralPath $appDir -Recurse -Filter Discovery.cs | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-Object -First 1
if (-not $discovery) { throw "Không tìm thấy Discovery.cs trong $appDir" }
Write-Code (Join-Path $discovery.DirectoryName 'MyRecipes.cs') $appCode

# ------------------------------------------------------------------ 2. Infrastructure
Write-Host "`n== Infrastructure ==" -ForegroundColor Cyan
$infDir = Join-Path $be 'CulinaryBlog.Infrastructure'
# Ưu tiên repository discovery; nếu không có thì lấy repository recipe của C2 làm mẫu
$implRe = '(?s)class\s+\w+[^{;]*?:[^{;]*?\b{0}\b'
$discRepo = $null
foreach ($iface in @('IRecipeDiscoveryRepository', 'I\w*Recipe\w*Repository')) {
  $discRepo = Find-Cs $be ($implRe -replace '\{0\}', $iface) |
    Where-Object { $_.FullName -notmatch '\\CulinaryBlog\.Application\\' -and $_.Name -notmatch '^MyRecipes' } |
    Sort-Object { if ($_.FullName -like "$infDir*") { 0 } else { 1 } } |
    Select-Object -First 1
  if ($discRepo) { break }
}
if (-not $discRepo) {
  # Không có mẫu nào: đặt vào thư mục gốc Infrastructure, giữ tên mặc định
  Write-Host "    Không tìm thấy repository mẫu, dùng thư mục gốc Infrastructure." -ForegroundColor Yellow
  $discRepo = [pscustomobject]@{ FullName = $null; DirectoryName = $infDir }
}
Write-Host "    Mẫu: $($discRepo.FullName)"
$discText = if ($discRepo.FullName) { [IO.File]::ReadAllText($discRepo.FullName) } else { '' }

# Lấy đúng namespace + tên DbContext + các using từ repository discovery đang chạy được
$ns = Get-Ns $discText
if ($ns) { $infCode = $infCode -replace '(?m)^namespace\s+[\w\.]+;', "namespace $ns;" }
$ctx = [regex]::Match($discText, '\b(\w+DbContext)\b').Groups[1].Value
if ($ctx) { $infCode = $infCode -replace '\bApplicationDbContext\b', $ctx; Write-Host "    DbContext: $ctx" }
$usingRe = '(?m)^using\s+[^;=]+;[ \t]*\r?\n?'
$usings = @([regex]::Matches($infCode, $usingRe) | ForEach-Object { $_.Value.Trim() }) +
          @([regex]::Matches($discText, $usingRe) | ForEach-Object { $_.Value.Trim() })
$usings = $usings | Where-Object { $_ -notmatch "^using\s+$([regex]::Escape($ns));" } | Sort-Object -Unique
$infCode = ($usings -join "`n") + "`n" + ([regex]::Replace($infCode, $usingRe, '')).TrimStart()
Write-Code (Join-Path $discRepo.DirectoryName 'MyRecipesRepository.cs') $infCode

# ------------------------------------------------------------------ 3. Đăng ký DI
Write-Host "`n== Dependency Injection ==" -ForegroundColor Cyan
$diFile = @(Find-Cs $be 'Add\w+\s*<\s*I\w*Recipe\w*Repository\s*,') | Select-Object -First 1
if ($diFile) {
  $diText = Get-Content -LiteralPath $diFile.FullName -Raw -Encoding UTF8
  if ($diText -match 'IMyRecipesRepository') {
    Write-Host "    Đã có đăng ký IMyRecipesRepository, bỏ qua."
  } else {
    $m = [regex]::Match($diText, '(?m)^[^\r\n]*Add\w+\s*<\s*(I\w*Recipe\w*Repository)\s*,\s*(\w+)\s*>[^\r\n]*\r?\n')
    $newLine = $m.Value -replace "\b$($m.Groups[1].Value)\b", 'IMyRecipesRepository' -replace "\b$($m.Groups[2].Value)\b", 'MyRecipesRepository'
    Save-Text $diFile.FullName ($diText.Insert($m.Index, $newLine))
  }
} else {
  Write-Host "    Không tìm thấy chỗ đăng ký IRecipeDiscoveryRepository. Tự thêm dòng này vào file DI của Infrastructure:" -ForegroundColor Red
  Write-Host "    services.AddScoped<IMyRecipesRepository, MyRecipesRepository>();"
}

# ------------------------------------------------------------------ 4. API controller
Write-Host "`n== API ==" -ForegroundColor Cyan
$apiDir = Join-Path $be 'CulinaryBlog.API'
$prefix = 'api'
$existingCtl = Find-Cs $apiDir ':\s*(ControllerBase|Controller)\b' | Select-Object -First 1
if ($existingCtl) {
  $ctlText = Get-Content -LiteralPath $existingCtl.FullName -Raw -Encoding UTF8
  $ctlDir = $existingCtl.DirectoryName
  $ctlNs = Get-Ns $ctlText
  if ($ctlNs) { $apiCode = $apiCode -replace '(?m)^namespace\s+[\w\.]+;', "namespace $ctlNs;" }
  $rm = [regex]::Match($ctlText, '\[Route\("\s*/?(api(/v\d+)?)/', 'IgnoreCase')
  if ($rm.Success) { $prefix = $rm.Groups[1].Value }
} else {
  $ctlDir = Join-Path $apiDir 'Controllers'
}
Write-Host "    Route prefix: /$prefix"
$apiCode = $apiCode -replace 'Route\("api/me/recipes"\)', "Route(`"$prefix/me/recipes`")"
Write-Code (Join-Path $ctlDir 'MyRecipesController.cs') $apiCode

# Program.cs phải map controller (nếu dự án đang dùng Minimal API)
$program = Get-ChildItem -LiteralPath $apiDir -Filter Program.cs | Select-Object -First 1
if ($program) {
  $pt = Get-Content -LiteralPath $program.FullName -Raw -Encoding UTF8
  $changed = $false
  if ($pt -notmatch 'AddControllers\s*\(') {
    $pt = $pt.Replace('var app = builder.Build();', "builder.Services.AddControllers();`r`nvar app = builder.Build();"); $changed = $true
  }
  if ($pt -notmatch 'MapControllers\s*\(') {
    $pt = $pt.Replace('app.Run();', "app.MapControllers();`r`napp.Run();"); $changed = $true
  }
  if ($changed) { Save-Text $program.FullName $pt } else { Write-Host "    Program.cs đã có AddControllers/MapControllers." }
}

# ------------------------------------------------------------------ 5. Frontend
Write-Host "`n== Frontend ==" -ForegroundColor Cyan
function Find-SlugDir($dir, $depth) {
  if ($depth -gt 8) { return $null }
  foreach ($d in (Get-ChildItem -LiteralPath $dir -Directory -ErrorAction SilentlyContinue)) {
    if (@('node_modules', '.next', '.git', 'bin', 'obj', 'backend') -contains $d.Name) { continue }
    if ($d.Name -eq '[slug]' -and $d.Parent.Name -eq 'recipes' -and (Test-Path -LiteralPath (Join-Path $d.FullName 'page.tsx'))) { return $d }
    $r = Find-SlugDir $d.FullName ($depth + 1)
    if ($r) { return $r }
  }
  return $null
}
$slugDir = Find-SlugDir $root 0
if (-not $slugDir) { throw "Không tìm thấy trang recipes\[slug]\page.tsx để xác định thư mục Next.js." }
$cur = $slugDir.Parent
while ($cur -and $cur.Name -ne 'app') { $cur = $cur.Parent }
if (-not $cur) { throw "Không tìm thấy thư mục app của Next.js phía trên $($slugDir.FullName)" }
$nextAppDir = $cur.FullName
$webRoot = $cur.Parent.FullName     # thư mục frontend, hoặc frontend\src
Write-Host "    Next.js app dir: $nextAppDir"

$libCode = $libCode.Replace('`/api/me/recipes', "``/$prefix/me/recipes").Replace('"/api/me/recipes/counts"', "`"/$prefix/me/recipes/counts`"").Replace('`/api/recipes/', "``/$prefix/recipes/")
Write-Code (Join-Path $webRoot 'lib\my-recipes.ts') $libCode
Write-Code (Join-Path $nextAppDir 'dashboard\recipes\page.tsx') $pageCode

# ------------------------------------------------------------------ 6. Build thử
Write-Host "`n== dotnet build ==" -ForegroundColor Cyan
dotnet build $apiDir --nologo -v q
if ($LASTEXITCODE -eq 0) {
  Write-Host "`nXong. Backend build thành công." -ForegroundColor Green
} else {
  Write-Host "`nBuild lỗi. Dán phần lỗi (error CS...) lên để sửa tiếp." -ForegroundColor Red
}
