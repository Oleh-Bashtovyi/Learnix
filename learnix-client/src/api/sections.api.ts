import { api } from './axios.instance';

export interface ReorderItem {
    id: string;
    order: number;
}

export const sectionsApi = {
    create: (courseId: string, title: string) =>
        api.post<{ id: string }>(`/courses/${courseId}/sections`, { title }).then((r) => r.data),

    updateTitle: (courseId: string, sectionId: string, title: string) =>
        api.patch(`/courses/${courseId}/sections/${sectionId}`, { title }).then((r) => r.data),

    delete: (courseId: string, sectionId: string) =>
        api.delete(`/courses/${courseId}/sections/${sectionId}`).then((r) => r.data),

    reorder: (courseId: string, items: ReorderItem[]) =>
        api.post(`/courses/${courseId}/sections/reorder`, { items }).then((r) => r.data),
};
