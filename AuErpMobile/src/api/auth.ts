import client from './client';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiration: string;
  userId: string;
  email: string;
  fullName: string;
  department: string;
}

export async function login(req: LoginRequest): Promise<LoginResponse> {
  const res = await client.post<{data: any}>('/auth/login', req);
  const data = res.data.data;
  return {
    token: data.token,
    expiration: data.expiresAt,
    userId: data.userId,
    email: data.email,
    fullName: data.displayName,
    department: Array.isArray(data.departments) && data.departments.length > 0 ? data.departments.join(', ') : 'General',
  };
}

export interface UserProfile {
  userId: string;
  email: string;
  fullName: string;
  department: string;
  roles: string[];
}

export async function getMe(): Promise<UserProfile> {
  const res = await client.get<{data: any}>('/auth/me');
  const data = res.data.data;
  return {
    userId: data.userId,
    email: data.email,
    fullName: data.displayName,
    department: Array.isArray(data.departments) && data.departments.length > 0 ? data.departments.join(', ') : 'General',
    roles: data.departments ?? [],
  };
}
