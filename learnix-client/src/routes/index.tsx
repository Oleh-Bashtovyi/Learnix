import { lazy, Suspense } from 'react';
import { createBrowserRouter } from 'react-router-dom';
import { PublicLayout } from '@/components/layout/PublicLayout';
import { AuthLayout } from '@/components/layout/AuthLayout';
import { InstructorLayout } from '@/components/layout/InstructorLayout';
import { AdminLayout } from '@/components/layout/AdminLayout';
import { CourseLayout } from '@/components/layout/CourseLayout';
import { StudentDashboardLayout } from '@/components/layout/StudentDashboardLayout';
import { PageFallback } from '@/components/common/PageFallback';
import { RequireRole } from '@/components/common/RequireRole';
import { publicRoutes } from './publicRoutes';

const VerifyEmailPage = lazy(() => import('@/pages/public/VerifyEmail/VerifyEmailPage'));
const CoursePlayerPage = lazy(() => import('@/pages/student/CoursePlayer/CoursePlayerPage'));
const CourseStartPage = lazy(() => import('@/pages/student/CoursePlayer/CourseStartPage'));
const TestLessonPage = lazy(() => import('@/pages/student/TestLesson/TestLessonPage'));
const ProfilePage = lazy(() => import('@/pages/student/Profile/ProfilePage'));
const AchievementsPage = lazy(() => import('@/pages/student/Achievements/AchievementsPage'));
const CertificatesPage = lazy(() => import('@/pages/student/Certificates/CertificatesPage'));
const WishlistPage = lazy(() => import('@/pages/student/Wishlist/WishlistPage'));
const MyLearningPage = lazy(() => import('@/pages/student/MyLearning/MyLearningPage'));
const PaymentPage = lazy(() => import('@/pages/student/Payment/PaymentPage'));
const InstructorDashboardPage = lazy(
    () => import('@/pages/instructor/Dashboard/InstructorDashboardPage'),
);
const CourseEditorPage = lazy(() => import('@/pages/instructor/CourseEditor/CourseEditorPage'));
const InstructorMyCoursesPage = lazy(
    () => import('@/pages/instructor/MyCourses/InstructorMyCoursesPage'),
);
const BecomeInstructorPage = lazy(
    () => import('@/pages/public/BecomeInstructor/BecomeInstructorPage'),
);
const LoginPage = lazy(() => import('@/pages/public/Login/LoginPage'));
const RegisterPage = lazy(() => import('@/pages/public/Register/RegisterPage'));
const MessagesPage = lazy(() => import('@/pages/student/Messages/MessagesPage'));
const NotificationsPage = lazy(() => import('@/pages/student/Notifications/NotificationsPage'));
const InstructorMessagesPage = lazy(
    () => import('@/pages/instructor/Messages/InstructorMessagesPage'),
);
const InstructorEarningsPage = lazy(
    () => import('@/pages/instructor/Earnings/InstructorEarningsPage'),
);

// Admin pages
const AdminDashboardPage = lazy(() => import('@/pages/admin/Dashboard/AdminDashboardPage'));
const UserManagementPage = lazy(() => import('@/pages/admin/UserManagement/UserManagementPage'));
const CourseModerationPage = lazy(
    () => import('@/pages/admin/CourseModeration/CourseModerationPage'),
);
const InstructorApplicationsPage = lazy(
    () => import('@/pages/admin/InstructorApplications/InstructorApplicationsPage'),
);
const PaymentHistoryPage = lazy(() => import('@/pages/admin/PaymentHistory/PaymentHistoryPage'));
const CategoryManagementPage = lazy(
    () => import('@/pages/admin/Categories/CategoryManagementPage'),
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
        element: <AuthLayout />,
        children: [
            { path: '/login', element: wrap(<LoginPage />) },
            { path: '/register', element: wrap(<RegisterPage />) },
        ],
    },
    {
        element: <PublicLayout />,
        children: [
            ...publicRoutes,
            {
                path: '/verify-email',
                element: wrap(<VerifyEmailPage />),
            },
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
                element: guardStudent(<StudentDashboardLayout />),
                children: [
                    {
                        path: '/my-learning',
                        element: wrap(<MyLearningPage />),
                    },
                    {
                        path: '/wishlist',
                        element: wrap(<WishlistPage />),
                    },
                    {
                        path: '/certificates',
                        element: wrap(<CertificatesPage />),
                    },
                ],
            },
            {
                path: '/payment/:courseId',
                element: guardStudent(wrap(<PaymentPage />)),
            },
            {
                path: '/messages',
                element: guardStudent(wrap(<MessagesPage />)),
            },
            {
                path: '/notifications',
                element: guardStudent(wrap(<NotificationsPage />)),
            },
        ],
    },
    {
        path: '/instructor',
        element: guardInstructor(wrap(<InstructorLayout />)),
        children: [
            { index: true, element: wrap(<InstructorDashboardPage />) },
            { path: 'courses', element: wrap(<InstructorMyCoursesPage />) },
            { path: 'courses/new', element: wrap(<CourseEditorPage />) },
            { path: 'courses/:id/edit', element: wrap(<CourseEditorPage />) },
            { path: 'messages', element: wrap(<InstructorMessagesPage />) },
            { path: 'earnings', element: wrap(<InstructorEarningsPage />) },
        ],
    },
    {
        path: '/admin',
        element: <RequireRole roles={['Admin']}>{wrap(<AdminLayout />)}</RequireRole>,
        children: [
            { index: true, element: wrap(<AdminDashboardPage />) },
            { path: 'users', element: wrap(<UserManagementPage />) },
            { path: 'courses', element: wrap(<CourseModerationPage />) },
            { path: 'applications', element: wrap(<InstructorApplicationsPage />) },
            { path: 'payments', element: wrap(<PaymentHistoryPage />) },
            { path: 'categories', element: wrap(<CategoryManagementPage />) },
        ],
    },
    {
        path: '/courses/:courseId/learn',
        element: guardStudent(<CourseLayout />),
        children: [
            { index: true, element: wrap(<CourseStartPage />) },
            { path: ':lessonId', element: wrap(<CoursePlayerPage />) },
            { path: ':lessonId/test', element: wrap(<TestLessonPage />) },
        ],
    },
]);
