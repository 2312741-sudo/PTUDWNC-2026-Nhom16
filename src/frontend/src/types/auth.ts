export interface ApiResponse<T> {
  data: T;
}

export interface User {
  id: string;
  email: string;
  fullName: string;
  userName: string;
  displayName: string;
  roles: string[];
  avatarUrl?: string | null;
  bio?: string | null;
  emailConfirmed?: boolean;
  createdAt?: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken?: string | null;
  tokenType: string;
  expiresIn: number;
  expiresAt: string;
  user: User;
}

export interface UpdateProfileRequest {
  displayName?: string;
  fullName?: string;
  avatarUrl?: string | null;
  bio?: string | null;
}
