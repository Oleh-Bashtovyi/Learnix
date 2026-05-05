import { createBrowserRouter } from 'react-router-dom';
import { PublicLayout } from '@/components/layout/PublicLayout';
import { publicRoutes } from './publicRoutes';

export const router = createBrowserRouter([
    {
        element: <PublicLayout />,
        children: publicRoutes,
    },
]);
