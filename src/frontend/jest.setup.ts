import "@testing-library/jest-dom";

// Mock ESM lucide-react in Jest CJS environment
jest.mock("lucide-react", () =>
  new Proxy(
    { __esModule: true },
    {
      get: (target: Record<string | symbol, unknown>, key) =>
        key in target ? target[key] : (props: any) => null,
    }
  )
);
