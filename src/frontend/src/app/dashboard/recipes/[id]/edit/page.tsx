import { Suspense } from "react";
import EditRecipeClient from "./EditRecipeClient";

export default function EditRecipePage() {
  return (
    <Suspense fallback={<p className="mx-auto max-w-3xl p-6 text-gray-500">Đang tải...</p>}>
      <EditRecipeClient />
    </Suspense>
  );
}