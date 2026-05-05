import { api } from './axios.instance';

export interface LoginRequest {
    email: string;
    password: string;
}

export interface LoginResponse {
    accessToken: string;
    accessTokenExpiresAt: string;
}

export interface RegisterRequest {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
}

export interface RegisterResponse {
    userId: string;
    email: string;
}

export const authApi = {
    login: (data: LoginRequest) => api.post<LoginResponse>('/auth/login', data).then((r) => r.data),

    register: (data: RegisterRequest) =>
        api.post<RegisterResponse>('/auth/register', data).then((r) => r.data),
};
