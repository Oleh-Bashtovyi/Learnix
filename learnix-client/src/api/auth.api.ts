import { api } from './axios.instance';

export interface LoginRequest {
    email: string;
    password: string;
}

export interface LoginResponse {
    accessToken: string;
    accessTokenExpiresAt: string;
    avatarUrl: string | null;
}

export interface RegisterRequest {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
}

export interface GoogleLoginRequest {
    idToken: string;
}

export interface ResendConfirmationRequest {
    email: string;
}

export interface VerifyEmailRequest {
    email: string;
    token: string;
}

export interface ForgotPasswordRequest {
    email: string;
}

export interface ResetPasswordRequest {
    email: string;
    token: string;
    newPassword: string;
}

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
}

export interface SetPasswordRequest {
    newPassword: string;
}

export const authApi = {
    logout: () => api.post('/auth/logout').then((r) => r.data),

    login: (data: LoginRequest) => api.post<LoginResponse>('/auth/login', data).then((r) => r.data),

    register: (data: RegisterRequest) =>
        api.post<LoginResponse>('/auth/register', data).then((r) => r.data),

    resendConfirmation: (data: ResendConfirmationRequest) =>
        api.post('/auth/resend-confirmation', data).then((r) => r.data),

    verifyEmail: (data: VerifyEmailRequest) =>
        api.post<LoginResponse>('/auth/confirm-email', data).then((r) => r.data),

    googleLogin: (data: GoogleLoginRequest) =>
        api.post<LoginResponse>('/auth/google', data).then((r) => r.data),

    forgotPassword: (data: ForgotPasswordRequest) =>
        api.post('/auth/forgot-password', data).then((r) => r.data),

    resetPassword: (data: ResetPasswordRequest) =>
        api.post('/auth/reset-password', data).then((r) => r.data),

    changePassword: (data: ChangePasswordRequest) =>
        api.post('/auth/change-password', data).then((r) => r.data),

    setPassword: (data: SetPasswordRequest) =>
        api.post('/auth/set-password', data).then((r) => r.data),
};
