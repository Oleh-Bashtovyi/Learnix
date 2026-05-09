import { lazy, Suspense } from 'react';
import { createBrowserRouter } from 'react-router-dom';
import { PublicLayout } from '@/components/layout/PublicLayout';
import { InstructorLayout } from '@/components/layout/InstructorLayout';
import { PageFallback } from '@/components/common/PageFallback';
import { RequireRole } from '@/components/common/RequireRole';
import { publicRoutes } from './publicRoutes';

const CoursePlayerPage = lazy(() => import('@/pages/student/CoursePlayer/CoursePlayerPage'));
const TestLessonPage = lazy(() => import('@/pages/student/TestLesson/TestLessonPage'));
const ProfilePage = lazy(() => import('@/pages/student/Profile/ProfilePage'));
const AchievementsPage = lazy(() => import('@/pages/student/Achievements/AchievementsPage'));
const CertificatesPage = lazy(() => import('@/pages/student/Certificates/CertificatesPage'));
const InstructorDashboardPage = lazy(
    () => import('@/pages/instructor/Dashboard/InstructorDashboardPage'),
);
const CourseEditorPage = lazy(() => import('@/pages/instructor/CourseEditor/CourseEditorPage'));
const BecomeInstructorPage = lazy(
    () => import('@/pages/public/BecomeInstructor/BecomeInstructorPage'),
);
const MessagesPage = lazy(() => import('@/pages/student/Messages/MessagesPage'));
const InstructorMessagesPage = lazy(
    () => import('@/pages/instructor/Messages/InstructorMessagesPage'),
);

const wrap = (el: React.ReactElement) => <Suspense fallback={<PageFallback />}>{el}</Suspense>;

const guardStudent = (el: React.ReactElement) => (
    <RequireRole roles={['Student', 'Instructor', 'Admin']}>{el}</RequireRole>
);

const guardInstructor = (el: React.ReactElement) => (
    <RequireRole roles={['Instructor', 'Admin']}>{el}</RequireRole>
);

export const router = createBrowserRouter([
    {
        element: <PublicLayout />,
        children: [
            ...publicRoutes,
            {
                path: '/become-instructor',
                element: wrap(<BecomeInstructorPage />),
            },
            {
                path: '/profile',
                element: guardStudent(wrap(<ProfilePage />)),
            },
            {
                path: '/achievements',
                element: guardStudent(wrap(<AchievementsPage />)),
            },
            {
                path: '/certificates',
                element: guardStudent(wrap(<CertificatesPage />)),
            },
            {
                path: '/messages',
                element: guardStudent(wrap(<MessagesPage />)),
            },
        ],
    },
    {
        path: '/instructor',
        element: guardInstructor(wrap(<InstructorLayout />)),
        children: [
            { index: true, element: wrap(<InstructorDashboardPage />) },
            { path: 'courses/new', element: wrap(<CourseEditorPage />) },
            { path: 'courses/:id/edit', element: wrap(<CourseEditorPage />) },
            { path: 'messages', element: wrap(<InstructorMessagesPage />) },
        ],
    },
    {
        path: '/courses/:courseId/learn/:lessonId',
        element: wrap(<CoursePlayerPage />),
    },
    {
        path: '/courses/:courseId/learn/:lessonId/test',
        element: wrap(<TestLessonPage />),
    },
]);
