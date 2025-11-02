import { describe, it, expect, beforeEach, vi } from "vitest";
import { render } from "@/test/test-utils";
import { LoginPage } from "./LoginPage";
import * as useAuthModule from "@/hooks/useAuth";

// Mock the useAuth hook
vi.mock("@/hooks/useAuth", () => ({
  useAuth: vi.fn(),
}));

// Helper to set viewport size
const setViewport = (width: number, height: number) => {
  Object.defineProperty(window, "innerWidth", {
    writable: true,
    configurable: true,
    value: width,
  });
  Object.defineProperty(window, "innerHeight", {
    writable: true,
    configurable: true,
    value: height,
  });
};

describe("LoginPage", () => {
  beforeEach(() => {
    // Reset to default viewport
    setViewport(1024, 768);

    // Mock useAuth hook with default values
    vi.mocked(useAuthModule.useAuth).mockReturnValue({
      user: null,
      isAuthenticated: false,
      login: vi.fn(),
      loginAsync: vi.fn(),
      loginLoading: false,
      loginError: null,
      register: vi.fn(),
      registerAsync: vi.fn(),
      registerLoading: false,
      registerError: null,
      logout: vi.fn(),
    });
  });

  describe("Snapshot Tests", () => {
    it("matches snapshot on mobile (375px)", () => {
      setViewport(375, 667);
      const { container } = render(<LoginPage />);
      expect(container).toMatchSnapshot();
    });

    it("matches snapshot on tablet (768px)", () => {
      setViewport(768, 1024);
      const { container } = render(<LoginPage />);
      expect(container).toMatchSnapshot();
    });

    it("matches snapshot on desktop (1280px)", () => {
      setViewport(1280, 720);
      const { container } = render(<LoginPage />);
      expect(container).toMatchSnapshot();
    });

    it("matches snapshot with loading state", () => {
      vi.mocked(useAuthModule.useAuth).mockReturnValue({
        user: null,
        isAuthenticated: false,
        login: vi.fn(),
        loginAsync: vi.fn(),
        loginLoading: true,
        loginError: null,
        register: vi.fn(),
        registerAsync: vi.fn(),
        registerLoading: false,
        registerError: null,
        logout: vi.fn(),
      });

      const { container } = render(<LoginPage />);
      expect(container).toMatchSnapshot();
    });

    it("matches snapshot with error state", () => {
      vi.mocked(useAuthModule.useAuth).mockReturnValue({
        user: null,
        isAuthenticated: false,
        login: vi.fn(),
        loginAsync: vi.fn(),
        loginLoading: false,
        loginError: new Error("Invalid email or password"),
        register: vi.fn(),
        registerAsync: vi.fn(),
        registerLoading: false,
        registerError: null,
        logout: vi.fn(),
      });

      const { container } = render(<LoginPage />);
      expect(container).toMatchSnapshot();
    });
  });

  describe("Content Tests", () => {
    it("renders the login form", () => {
      const { getByText, getByLabelText } = render(<LoginPage />);
      expect(getByText("Welcome Back")).toBeInTheDocument();
      expect(getByLabelText("Email")).toBeInTheDocument();
      expect(getByLabelText("Password")).toBeInTheDocument();
      expect(getByText("Sign In")).toBeInTheDocument();
    });

    it("renders link to registration", () => {
      const { getByText } = render(<LoginPage />);
      expect(getByText("Sign up")).toBeInTheDocument();
    });
  });
});
