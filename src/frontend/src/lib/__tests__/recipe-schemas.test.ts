import {
  basicInfoSchema, emptyToNull, imageUploadSchema, ingredientSchema, stepSchema,
} from "../recipe-schemas";
import { IMAGE_MAX_BYTES, IMAGE_TYPES } from "../recipe-editor";

const validInfo = {
  title: "Canh chua", description: "", instructions: "",
  prepTimeMinutes: 10, cookTimeMinutes: 0, servings: 2,
  difficulty: "Easy", categoryId: "cat-1", nutrition: null,
};
const pathsOf = (r: { success: boolean; error?: { issues: { path: PropertyKey[] }[] } }) =>
  r.error?.issues.map(i => i.path.join(".")) ?? [];
const file = (bytes: number, type: string) => new File([new Uint8Array(bytes)], "a.img", { type });

describe("basicInfoSchema", () => {
  it("chap nhan du lieu hop le", () => {
    expect(basicInfoSchema.safeParse(validInfo).success).toBe(true);
  });
  it("tieu de 4 ky tu sai, 5 ky tu dung (sau khi trim)", () => {
    expect(pathsOf(basicInfoSchema.safeParse({ ...validInfo, title: "abcd" }))).toContain("title");
    expect(pathsOf(basicInfoSchema.safeParse({ ...validInfo, title: "  abcd  " }))).toContain("title");
    expect(basicInfoSchema.safeParse({ ...validInfo, title: "abcde" }).success).toBe(true);
  });
  it("o so de trong (NaN) bi chan, prep phai >= 1", () => {
    expect(pathsOf(basicInfoSchema.safeParse({ ...validInfo, prepTimeMinutes: NaN }))).toContain("prepTimeMinutes");
    expect(pathsOf(basicInfoSchema.safeParse({ ...validInfo, prepTimeMinutes: 0 }))).toContain("prepTimeMinutes");
    expect(pathsOf(basicInfoSchema.safeParse({ ...validInfo, servings: 1.5 }))).toContain("servings");
  });
  it("bat buoc chon danh muc", () => {
    expect(pathsOf(basicInfoSchema.safeParse({ ...validInfo, categoryId: "" }))).toContain("categoryId");
  });
  it("dinh duong khong duoc am", () => {
    const nutrition = { calories: -1, protein: null, carbohydrates: null, fat: null, fiber: null, sodium: null };
    expect(pathsOf(basicInfoSchema.safeParse({ ...validInfo, nutrition }))).toContain("nutrition.calories");
  });
});

describe("ingredientSchema", () => {
  it("so luong phai > 0, cho phep null", () => {
    expect(pathsOf(ingredientSchema.safeParse({ name: "Ca", quantity: 0, unit: "", notes: "" }))).toContain("quantity");
    expect(ingredientSchema.safeParse({ name: "Ca", quantity: null, unit: "", notes: "" }).success).toBe(true);
  });
  it("unit/notes rong chuyen thanh null", () => {
    const r = ingredientSchema.parse({ name: "Ca", quantity: 1, unit: "  ", notes: "" });
    expect(r.unit).toBeNull();
    expect(r.notes).toBeNull();
  });
});

describe("stepSchema", () => {
  it("tieu de rong va hen gio am bi chan", () => {
    const r = stepSchema.safeParse({ title: " ", description: "x", timerMinutes: -1 });
    expect(pathsOf(r)).toEqual(expect.arrayContaining(["title", "timerMinutes"]));
  });
});

describe("imageUploadSchema", () => {
  const okType = IMAGE_TYPES[0];
  it("anh hop le qua, alt rong thanh null", () => {
    const r = imageUploadSchema.parse({ file: file(10, okType), altText: "" });
    expect(r.altText).toBeNull();
  });
  it("sai MIME bi chan", () => {
    expect(pathsOf(imageUploadSchema.safeParse({ file: file(10, "image/gif"), altText: "" }))).toContain("file");
  });
  it("dung bang gioi han qua, vuot 1 byte bi chan", () => {
    expect(imageUploadSchema.safeParse({ file: file(IMAGE_MAX_BYTES, okType), altText: "" }).success).toBe(true);
    expect(pathsOf(imageUploadSchema.safeParse({ file: file(IMAGE_MAX_BYTES + 1, okType), altText: "" }))).toContain("file");
  });
  it("alt qua 200 ky tu bi chan", () => {
    expect(pathsOf(imageUploadSchema.safeParse({ file: file(10, okType), altText: "a".repeat(201) }))).toContain("altText");
  });
});

describe("emptyToNull", () => {
  it("chuoi rong/null/undefined -> null, so giu nguyen", () => {
    expect(emptyToNull("")).toBeNull();
    expect(emptyToNull(null)).toBeNull();
    expect(emptyToNull(undefined)).toBeNull();
    expect(emptyToNull("12.5")).toBe(12.5);
  });
});