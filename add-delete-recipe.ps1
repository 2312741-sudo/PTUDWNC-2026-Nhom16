# Thêm endpoint XOÁ công thức (soft delete) — Application + Domain + Infrastructure + API.
# Chạy từ D:\CulinaryBlog:
#   powershell -ExecutionPolicy Bypass -File .\add-delete-recipe.ps1
# File bị sửa đều có backup *.bak bên cạnh.

$ErrorActionPreference = 'Stop'
$root = 'D:\CulinaryBlog'
if (-not (Test-Path (Join-Path $root 'src\backend'))) { $root = (Get-Location).Path }
$be = Join-Path $root 'src\backend'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Backup($p) { if (Test-Path -LiteralPath $p) { Copy-Item -LiteralPath $p "$p.bak" -Force } }
function Save($p,$c) { Backup $p; [IO.File]::WriteAllText($p, $c, $utf8); Write-Host "[SỬA] $p" -ForegroundColor Yellow }

# ---------- 1. Domain: thêm SoftDelete() vào Recipe entity ----------
Write-Host "`n== Domain ==" -ForegroundColor Cyan
$rec = Join-Path $be 'CulinaryBlog.Domain\Entities\Recipe.cs'
$c = [IO.File]::ReadAllText($rec)
if ($c -match '\bSoftDelete\s*\(') {
  Write-Host "    Recipe đã có SoftDelete(), bỏ qua." -ForegroundColor DarkGray
} else {
  # Kiểm tra tên field xoá mềm ở BaseEntity: IsDeleted (theo ADR-0001)
  $method = @'
__NL__
    /// <summary>Xoá mềm (ADR-0001): đặt IsDeleted, global query filter tự ẩn bản ghi.</summary>
    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
'@ -replace '__NL__',''
  # Chèn trước dấu } cuối cùng của class (dấu } cuối file)
  $idx = $c.LastIndexOf('}')
  $c = $c.Substring(0, $idx) + $method + "`n}" + $c.Substring($idx+1)
  Save $rec $c
}

# Kiểm tra IsDeleted có setter dùng được trong entity không
$domHasSetter = ([IO.File]::ReadAllText($rec) -match 'IsDeleted\s*\{\s*get;\s*(private\s+)?set;')
if (-not $domHasSetter) {
  Write-Host "    CẢNH BÁO: không thấy 'IsDeleted { get; set; }' trong Recipe.cs." -ForegroundColor Red
  Write-Host "    IsDeleted có thể ở lớp cha BaseEntity với setter protected — nếu build lỗi CS0272, báo lại." -ForegroundColor Red
}

# ---------- 2. Application: interface + command/handler ----------
Write-Host "`n== Application ==" -ForegroundColor Cyan
$recipes = Join-Path $be 'CulinaryBlog.Application\Recipes.cs'
$c = [IO.File]::ReadAllText($recipes)

# 2a. thêm void Remove(Recipe) vào IRecipeRepository nếu chưa có
if ($c -notmatch 'void\s+Remove\s*\(\s*Recipe\b') {
  $anchor = 'void Add(Recipe recipe);'
  if ($c.Contains($anchor)) {
    $c = $c.Replace($anchor, "$anchor`r`n    void Remove(Recipe recipe);")
    Write-Host "    + IRecipeRepository.Remove(Recipe)"
  } else {
    Write-Host "    Không thấy 'void Add(Recipe recipe);' — thêm tay 'void Remove(Recipe recipe);' vào IRecipeRepository." -ForegroundColor Red
  }
} else { Write-Host "    IRecipeRepository.Remove đã có." -ForegroundColor DarkGray }

# 2b. thêm command + handler nếu chưa có
if ($c -notmatch '\bDeleteRecipeCommand\b') {
  $snippet = @'

#region C2.4 — Xoá công thức (FR-RCP-007) — soft delete (ADR-0001)

// Xoá mềm: set IsDeleted, global query filter tự ẩn khỏi mọi truy vấn.
// Con (ingredient/step/image) để nguyên — ON DELETE CASCADE chỉ chạy khi hard delete;
// với soft delete ta chỉ cần ẩn aggregate gốc là đủ (D-recipe không lộ qua filter).
public sealed record DeleteRecipeCommand(Guid Id, string? RowVersion) : IRequest;

public sealed class DeleteRecipeValidator : AbstractValidator<DeleteRecipeCommand>
{
    public DeleteRecipeValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class DeleteRecipeHandler(IRecipeRepository repo, ICurrentUser currentUser)
    : IRequestHandler<DeleteRecipeCommand>
{
    public async Task Handle(DeleteRecipeCommand cmd, CancellationToken ct)
    {
        var recipe = await RecipeGuard.LoadOwnedAsync(repo, currentUser, cmd.Id, ct);
        RecipeGuard.EnsureVersion(recipe, cmd.RowVersion);   // 422 nếu bản ghi đã đổi ở nơi khác

        recipe.SoftDelete();                 // set IsDeleted; interceptor cập nhật RowVersion
        await repo.SaveChangesAsync(ct);
    }
}

#endregion
'@
  $c = $c.TrimEnd() + "`r`n" + $snippet
  Write-Host "    + DeleteRecipeCommand / Handler / Validator"
} else { Write-Host "    DeleteRecipeCommand đã có." -ForegroundColor DarkGray }
Save $recipes $c

# ---------- 3. Infrastructure: implement Remove trong RecipeRepository ----------
Write-Host "`n== Infrastructure ==" -ForegroundColor Cyan
$repoFile = Get-ChildItem $be -Recurse -Filter *.cs |
  Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and ([IO.File]::ReadAllText($_.FullName) -match 'void\s+Add\s*\(\s*Recipe\b') } |
  Select-Object -First 1
if (-not $repoFile) {
  Write-Host "    Không tìm thấy class implement IRecipeRepository.Add — thêm tay method Remove." -ForegroundColor Red
} else {
  $rc = [IO.File]::ReadAllText($repoFile.FullName)
  if ($rc -match 'void\s+Remove\s*\(\s*Recipe\b') {
    Write-Host "    RecipeRepository.Remove đã có." -ForegroundColor DarkGray
  } else {
    # Chèn ngay sau method Add(Recipe ...) — soft delete nên chỉ đánh dấu, KHÔNG gọi EF Remove()
    $m = [regex]::Match($rc, 'public\s+void\s+Add\s*\(\s*Recipe\s+\w+\s*\)\s*=>[^;]*;')
    if (-not $m.Success) { $m = [regex]::Match($rc, 'public\s+void\s+Add\s*\(\s*Recipe\s+\w+\s*\)\s*\{[^}]*\}') }
    if ($m.Success) {
      $paramName = [regex]::Match($rc, 'void\s+Remove\s*\(\s*Recipe\s+(\w+)').Groups[1].Value
      $impl = @'

    // Soft delete (ADR-0001): entity.SoftDelete() đã đặt IsDeleted; ở đây chỉ đảm bảo EF theo dõi entity.
    // KHÔNG gọi _db.Recipes.Remove() vì đó là hard delete.
    public void Remove(Recipe recipe) => _db.Recipes.Update(recipe);
'@
      # đoán tên field DbContext trong repo (thường _db hoặc db)
      $ctxField = [regex]::Match($rc, '(?:private\s+readonly\s+\w+\s+|)(\b_?db)\b\.Recipes').Groups[1].Value
      if (-not $ctxField) { $ctxField = '_db' }
      $impl = $impl -replace '_db\.Recipes', "$ctxField.Recipes"
      $rc = $rc.Insert($m.Index + $m.Length, "`r`n$impl")
      Save $repoFile.FullName $rc
      Write-Host "    (DbContext field dùng: $ctxField)"
    } else {
      Write-Host "    Không định vị được method Add trong $($repoFile.Name) — thêm tay Remove." -ForegroundColor Red
    }
  }
}

# ---------- 4. API: map DELETE /api/v1/recipes/{id} ----------
Write-Host "`n== API ==" -ForegroundColor Cyan
$prog = Join-Path $be 'CulinaryBlog.API\Program.cs'
$pc = [IO.File]::ReadAllText($prog)
if ($pc -match 'MapDelete\("/\{id:guid\}"') {
  Write-Host "    DELETE /recipes/{id} đã có." -ForegroundColor DarkGray
} else {
  # chèn ngay sau block MapPut("/{id:guid}", ...UpdateRecipe...) — tức sau .ProducesProblem(422); của UpdateRecipe
  $anchor = [regex]::Match($pc, '\.RequireAuthorization\("AuthorPolicy"\)\.WithName\("UpdateRecipe"\)[\s\S]*?\.ProducesProblem\(422\);')
  if ($anchor.Success) {
    $endpoint = @'


recipes.MapDelete("/{id:guid}", async (Guid id, HttpRequest request, ISender sender, CancellationToken ct) =>
{
    // RowVersion qua header If-Match (frontend dashboard gửi kèm). Không có cũng cho xoá.
    var rowVersion = request.Headers.IfMatch.Count > 0 ? request.Headers.IfMatch.ToString().Trim('"') : null;
    await sender.Send(new DeleteRecipeCommand(id, rowVersion), ct);
    return Results.NoContent();
})
    .RequireAuthorization("AuthorPolicy").WithName("DeleteRecipe")
    .Produces(204).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(422);
'@
    $pc = $pc.Insert($anchor.Index + $anchor.Length, $endpoint)
    Save $prog $pc
    Write-Host "    + DELETE /api/v1/recipes/{id}"
  } else {
    Write-Host "    Không tìm thấy neo UpdateRecipe trong Program.cs — thêm tay endpoint DELETE." -ForegroundColor Red
  }
}

# ---------- 5. Build ----------
Write-Host "`n== Build ==" -ForegroundColor Cyan
Get-Process dotnet,CulinaryBlog.API -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet build (Join-Path $root 'CulinaryBlog.sln') --nologo -v q
if ($LASTEXITCODE -eq 0) { Write-Host "`nBuild OK. Endpoint xoá đã sẵn sàng." -ForegroundColor Green }
else { Write-Host "`nBuild lỗi — dán các dòng error CS... lên." -ForegroundColor Red }
