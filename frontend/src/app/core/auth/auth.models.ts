export interface PlanInfo {
  name: string;
  originalName?: string | null;
  isUnrecognized: boolean;
  maxProducts?: number | null;
  maxOrdersPerMonth?: number | null;
  maxUsers: number;
  aiFeatures: boolean;
  expiresAt?: string | null;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  shopId: string;
  shopName: string;
  userId: string;
  email: string;
  displayName: string;
  role: string;
  plan: PlanInfo;
}

export interface MeResponse {
  shopId: string;
  shopName: string;
  userId: string;
  email: string;
  displayName: string;
  role: string;
  plan: PlanInfo;
}

export interface SignUpRequest {
  licenseKey: string;
  email: string;
  password: string;
  shopName: string;
  displayName?: string;
  phone?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}
