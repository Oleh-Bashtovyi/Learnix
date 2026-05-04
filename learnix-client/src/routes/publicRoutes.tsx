/* eslint-disable react-refresh/only-export-components */
import { lazy, Suspense, type ReactElement } from 'react';
import type { RouteObject } from 'react-router-dom';
import { PageFallback } from '@/components/common/PageFallback';

const LandingPage = lazy(() => import('@/pages/public/Landing/LandingPage'));
const LoginPage = lazy(() => import('@/pages/public/Login/LoginPage'));
const RegisterPage = lazy(() => import('@/pages/public/Register/RegisterPage'));
const CourseCatalogPage = lazy(() => import('@/pages/public/CourseCatalog/CourseCatalogPage'));
const ProfilePage = lazy(() => import('@/pages/student/Profile/ProfilePage'));
const NotFoundPage = lazy(() => import('@/pages/public/NotFound/NotFoundPage'));

const wrap = (el: ReactElement) => <Suspense fallback={<PageFallback />}>{el}</Suspense>;

export const publicRoutes: RouteObject[] = [
    { index: true, element: wrap(<LandingPage />) },
    { path: '/login', element: wrap(<LoginPage />) },
    { path: '/register', element: wrap(<RegisterPage />) },
    { path: '/courses', element: wrap(<CourseCatalogPage />) },
    { path: '/profile', element: wrap(<ProfilePage />) },
    { path: '*', element: wrap(<NotFoundPage />) },
];
