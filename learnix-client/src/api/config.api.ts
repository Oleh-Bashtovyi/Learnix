import { api } from './axios.instance';

export interface PublicConfigDto {
    aiProvider: string;
}

export const configApi = {
    getPublicConfig: () => api.get<PublicConfigDto>('/config/public').then((r) => r.data),
};
