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

type MobileApiBody<T> = {
  success: boolean;
  message?: string | null;
  data?: T;
};

export async function login(req: LoginRequest): Promise<LoginResponse> {
  const res = await client.post<MobileApiBody<{
    token: string;
    expiresAt: string;
    userId: string;
    email: string;
    displayName: string;
    departments: string[];
  }>>('/auth/login', {
    email: req.email.trim(),
    password: req.password,
  });

  const body = res.data;
  if (!body.success || !body.data?.token) {
    throw new Error(body.message ?? 'Login failed.');
  }
  const data = body.data;
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
