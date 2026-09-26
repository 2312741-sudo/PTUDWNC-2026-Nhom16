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
        router.replace("/auth/login?returnUrl=/dashboard/recipes");
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