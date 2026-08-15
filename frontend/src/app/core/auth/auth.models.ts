/**
 * Client field limits; must stay in sync with Shared DTO `[StringLength]` and FluentValidation.
 * Password min 8 applies to signup only; max 128 applies to login too.
 */
export const AUTH_FIELD_LIMITS = {
  licenseKey: 100,
  email: 320,
  password: 128,
  passwordMin: 8,
  shopName: 200,
  displayName: 200,
  phone: 50
} as const;

export type UserRole = 'Owner' | 'Assistant';

/** Plan snapshot from Platform `planName`. Null max values mean unlimited. */
export interface PlanInfo {
  name: string;
  originalName?: string | null;
  /** True when Platform returned an unknown plan; show an amber warning. */
  isUnrecognized: boolean;
  maxProducts?: number | null;
  maxOrdersPerMonth?: number | null;
  maxUsers: number;
  aiFeatures: boolean;
  expiresAt?: string | null;
}

/** Mirrors `AuthResponse` — includes the OrderFlow JWT (not a Platform token). */
export interface AuthResponse {
  token: string;
  expiresAt: string;
  shopId: string;
  shopName: string;
  userId: string;
  email: string;
  displayName: string;
  role: UserRole | string;
  plan: PlanInfo;
}

/** Mirrors `MeResponse` — session without a new token. */
export interface MeResponse {
  shopId: string;
  shopName: string;
  userId: string;
  email: string;
  displayName: string;
  role: UserRole | string;
  plan: PlanInfo;
}

/** Mirrors OrderFlow.Shared.DTOs.Auth.SignUpRequest */
export interface SignUpRequest {
  licenseKey: string;
  email: string;
  password: string;
  shopName: string;
  displayName?: string;
  phone?: string;
}

/** Mirrors OrderFlow.Shared.DTOs.Auth.LoginRequest */
export interface LoginRequest {
  email: string;
  password: string;
}
