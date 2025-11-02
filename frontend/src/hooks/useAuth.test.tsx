import { describe, it, expect, beforeEach, vi } from "vitest";
import { renderHook, waitFor } from "@/test/test-utils";
import { useAuth } from "./useAuth";
import * as authApi from "@/api/auth";
import * as authStore from "@/store/authStore";

// Mock the API and store
vi.mock("@/api/auth");
vi.mock("@/store/authStore");

describe("useAuth", () => {
  const mockUser = {
    id: "1",
    email: "test@example.com",
    firstName: "Test",
    lastName: "User",
    createdAt: "2024-01-01T00:00:00Z",
  };

  const mockAuthResponse = {
    token: "mock-token",
    user: mockUser,
  };

  beforeEach(() => {
    vi.clearAllMocks();

    // Mock authStore
    vi.mocked(authStore.useAuthStore).mockReturnValue({
      user: null,
      token: null,
      isAuthenticated: false,
      login: vi.fn(),
      logout: vi.fn(),
      setUser: vi.fn(),
    });
  });

  it("should return initial auth state", () => {
    const { result } = renderHook(() => useAuth());

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.loginLoading).toBe(false);
    expect(result.current.registerLoading).toBe(false);
    expect(result.current.loginError).toBeNull();
    expect(result.current.registerError).toBeNull();
  });

  it("should handle successful login", async () => {
    const mockStoreLogin = vi.fn();
    vi.mocked(authStore.useAuthStore).mockReturnValue({
      user: null,
      token: null,
      isAuthenticated: false,
      login: mockStoreLogin,
      logout: vi.fn(),
      setUser: vi.fn(),
    });

    vi.mocked(authApi.authApi.login).mockResolvedValue(mockAuthResponse);

    const { result } = renderHook(() => useAuth());

    result.current.login({
      email: "test@example.com",
      password: "password123",
    });

    await waitFor(() => {
      expect(result.current.loginLoading).toBe(false);
    });

    expect(authApi.authApi.login).toHaveBeenCalledWith({
      email: "test@example.com",
      password: "password123",
    });

    expect(mockStoreLogin).toHaveBeenCalledWith(
      mockAuthResponse.token,
      mockAuthResponse.user
    );
  });

  it("should handle login error", async () => {
    const mockError = new Error("Invalid credentials");
    vi.mocked(authApi.authApi.login).mockRejectedValue(mockError);

    const { result } = renderHook(() => useAuth());

    result.current.login({
      email: "test@example.com",
      password: "wrong-password",
    });

    await waitFor(() => {
      expect(result.current.loginError).toBeTruthy();
    });

    expect(result.current.loginLoading).toBe(false);
  });

  it("should handle successful registration", async () => {
    const mockStoreLogin = vi.fn();
    vi.mocked(authStore.useAuthStore).mockReturnValue({
      user: null,
      token: null,
      isAuthenticated: false,
      login: mockStoreLogin,
      logout: vi.fn(),
      setUser: vi.fn(),
    });

    vi.mocked(authApi.authApi.register).mockResolvedValue(mockAuthResponse);

    const { result } = renderHook(() => useAuth());

    result.current.register({
      email: "test@example.com",
      password: "password123",
      firstName: "Test",
      lastName: "User",
    });

    await waitFor(() => {
      expect(result.current.registerLoading).toBe(false);
    });

    expect(authApi.authApi.register).toHaveBeenCalledWith({
      email: "test@example.com",
      password: "password123",
      firstName: "Test",
      lastName: "User",
    });

    expect(mockStoreLogin).toHaveBeenCalledWith(
      mockAuthResponse.token,
      mockAuthResponse.user
    );
  });

  it("should handle registration error", async () => {
    const mockError = new Error("Email already exists");
    vi.mocked(authApi.authApi.register).mockRejectedValue(mockError);

    const { result } = renderHook(() => useAuth());

    result.current.register({
      email: "existing@example.com",
      password: "password123",
      firstName: "Test",
      lastName: "User",
    });

    await waitFor(() => {
      expect(result.current.registerError).toBeTruthy();
    });

    expect(result.current.registerLoading).toBe(false);
  });

  it("should handle logout", () => {
    const mockStoreLogout = vi.fn();
    vi.mocked(authStore.useAuthStore).mockReturnValue({
      user: mockUser,
      token: "mock-token",
      isAuthenticated: true,
      login: vi.fn(),
      logout: mockStoreLogout,
      setUser: vi.fn(),
    });

    const { result } = renderHook(() => useAuth());

    result.current.logout();

    expect(mockStoreLogout).toHaveBeenCalled();
  });
});
