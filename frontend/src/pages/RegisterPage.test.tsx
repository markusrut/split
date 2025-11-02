import { describe, it, expect, beforeEach, vi } from "vitest";
import { render } from "@/test/test-utils";
import { RegisterPage } from "./RegisterPage";
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

describe("RegisterPage", () => {
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
      const { container } = render(<RegisterPage />);
      expect(container).toMatchSnapshot();
    });

    it("matches snapshot on tablet (768px)", () => {
      setViewport(768, 1024);
      const { container } = render(<RegisterPage />);
      expect(container).toMatchSnapshot();
    });

    it("matches snapshot on desktop (1280px)", () => {
      setViewport(1280, 720);
      const { container } = render(<RegisterPage />);
      expect(container).toMatchSnapshot();
    });

    it("matches snapshot with loading state", () => {
      vi.mocked(useAuthModule.useAuth).mockReturnValue({
        user: null,
        isAuthenticated: false,
        login: vi.fn(),
        loginAsync: vi.fn(),
        loginLoading: false,
        loginError: null,
        register: vi.fn(),
        registerAsync: vi.fn(),
        registerLoading: true,
        registerError: null,
        logout: vi.fn(),
      });

      const { container } = render(<RegisterPage />);
      expect(container).toMatchSnapshot();
    });

    it("matches snapshot with error state", () => {
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
        registerError: new Error("Email already exists"),
        logout: vi.fn(),
      });

      const { container } = render(<RegisterPage />);
      expect(container).toMatchSnapshot();
    });
  });

  describe("Content Tests", () => {
    it("renders the registration form", () => {
      const { getByRole, getByLabelText } = render(<RegisterPage />);
      expect(
        getByRole("heading", { name: "Create Account" })
      ).toBeInTheDocument();
      expect(getByLabelText("First Name")).toBeInTheDocument();
      expect(getByLabelText("Last Name")).toBeInTheDocument();
      expect(getByLabelText("Email")).toBeInTheDocument();
      expect(getByLabelText("Password")).toBeInTheDocument();
      expect(getByLabelText("Confirm Password")).toBeInTheDocument();
    });

    it("renders link to login", () => {
      const { getByText } = render(<RegisterPage />);
      expect(getByText("Sign in")).toBeInTheDocument();
    });

    it("renders password requirements helper text", () => {
      const { getByText } = render(<RegisterPage />);
      expect(
        getByText("Must contain uppercase, lowercase, and number")
      ).toBeInTheDocument();
    });
  });
});
