import { revalidatePath } from "next/cache";
import { NextResponse } from "next/server";

const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080/api/v1";
const SLUG = /^[a-z0-9-]{1,200}$/;

/**
 * C4 detail invalidation (K12/K16): trang chi tiết dùng ISR 5 phút; sau khi tác giả lưu,
 * wizard gọi route này để xoá cache ngay. Chỉ người đã đăng nhập hợp lệ (xác thực qua backend) được gọi.
 */
export async function POST(req: Request) {
  const auth = req.headers.get("authorization");
  if (!auth?.startsWith("Bearer ")) {
    return NextResponse.json({ message: "Chưa đăng nhập" }, { status: 401 });
  }
  const me = await fetch(`${API}/auth/me`, { headers: { Authorization: auth }, cache: "no-store" }).catch(() => null);
  if (!me?.ok) {
    return NextResponse.json({ message: "Token không hợp lệ" }, { status: 401 });
  }

  const body = (await req.json().catch(() => null)) as { slugs?: unknown } | null;
  const raw = body?.slugs;
  const slugs = Array.isArray(raw)
    ? raw.filter((s): s is string => typeof s === "string" && SLUG.test(s)).slice(0, 5)
    : [];

  for (const slug of slugs) revalidatePath(`/recipes/${slug}`);
  revalidatePath("/recipes");
  revalidatePath("/");
  return NextResponse.json({ data: { revalidated: slugs } });
}