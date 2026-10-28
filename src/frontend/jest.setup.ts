import "@testing-library/jest-dom";

// lucide-react bản ESM không nạp được trong Jest (CJS); mock icon components toàn cục
jest.mock("lucide-react", () =>
  new Proxy(
    { __esModule: true },
    {
      get: (target: Record<string | symbol, unknown>, key) =>
        key in target ? target[key] : () => null,
    }
  )
);
