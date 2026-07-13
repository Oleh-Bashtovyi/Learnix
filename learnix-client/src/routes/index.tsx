import { Suspense, lazy } from 'react';
import { RouterProvider, createBrowserRouter } from 'react-router-dom';
import { RequireGuest } from '@/components/common/auth/RequireGuest';
import { RequireRole } from '@/components/common/auth/RequireRole';
import { PageFallback } from '@/components/common/system/PageFallback';
import { AdminLayout } from '@/components/layout/AdminLayout';
import { AuthLayout } from '@/components/layout/AuthLayout';
import { CourseLayout } from '@/components/layout/CourseLayout';
import { InstructorLayout } from '@/components/layout/InstructorLayout';
import { PublicLayout } from '@/components/layout/PublicLayout';
import { StudentDashboardLayout } from '@/components/layout/StudentDashboardLayout';
import { UserRole } from '@/enums/user.enums';
import { APP_ROUTES } from '@/routes/paths';

const LandingPage = lazy(() => import('@/pages/public/Landing/LandingPage'));
const CourseCatalogPage = lazy(() => import('@/pages/public/CourseCatalog/CourseCatalogPage'));
const CourseDetailPage = lazy(() => import('@/pages/public/CourseDetail/CourseDetailPage'));
const InstructorProfilePage = lazy(
    () => import('@/pages/public/InstructorProfile/InstructorProfilePage'),
);
const NotFoundPage = lazy(() => import('@/pages/public/NotFound/NotFoundPage'));
const FaqPage = lazy(() => import('@/pages/public/Faq/FaqPage'));
const AboutPage = lazy(() => import('@/pages/public/About/AboutPage'));
const CertificateVerifyPage = lazy(
    () => import('@/pages/public/VerifyCertificate/CertificateVerifyPage'),
);

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
const ForgotPasswordPage = lazy(() => import('@/pages/public/ForgotPassword/ForgotPasswordPage'));
const ResetPasswordPage = lazy(() => import('@/pages/public/ResetPassword/ResetPasswordPage'));
const MessagesPage = lazy(() => import('@/pages/student/Messages/MessagesPage'));
const NotificationsPage = lazy(() => import('@/pages/student/Notifications/NotificationsPage'));
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
    <RequireRole roles={[UserRole.Student]}>{el}</RequireRole>
);

const guardInstructor = (el: React.ReactElement) => (
    <RequireRole roles={[UserRole.Instructor]}>{el}</RequireRole>
);

const guardGuest = (el: React.ReactElement) => <RequireGuest>{el}</RequireGuest>;

const router = createBrowserRouter([
    {
        element: <AuthLayout />,
        children: [
            { path: APP_ROUTES.public.login, element: guardGuest(wrap(<LoginPage />)) },
            { path: APP_ROUTES.public.register, element: guardGuest(wrap(<RegisterPage />)) },
            { path: APP_ROUTES.public.forgotPassword, element: wrap(<ForgotPasswordPage />) },
            { path: APP_ROUTES.public.resetPassword, element: wrap(<ResetPasswordPage />) },
        ],
    },
    {
        element: <PublicLayout />,
        children: [
            { index: true, element: wrap(<LandingPage />) },
            { path: APP_ROUTES.public.courses, element: wrap(<CourseCatalogPage />) },
            { path: APP_ROUTES.public.courseDetailPattern, element: wrap(<CourseDetailPage />) },
            {
                path: APP_ROUTES.public.instructorProfilePattern,
                element: wrap(<InstructorProfilePage />),
            },
            { path: APP_ROUTES.public.faq, element: wrap(<FaqPage />) },
            { path: APP_ROUTES.public.about, element: wrap(<AboutPage />) },
            {
                path: APP_ROUTES.public.verifyCertificatePattern,
                element: wrap(<CertificateVerifyPage />),
            },
            { path: '*', element: wrap(<NotFoundPage />) },
            {
                path: APP_ROUTES.public.verifyEmail,
                element: wrap(<VerifyEmailPage />),
            },
            {
                path: APP_ROUTES.public.becomeInstructor,
                element: wrap(<BecomeInstructorPage />),
            },
            {
                path: APP_ROUTES.student.profile,
                element: guardStudent(wrap(<ProfilePage />)),
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
                    {
                        path: '/achievements',
                        element: wrap(<AchievementsPage />),
                    },
                ],
            },
            {
                path: APP_ROUTES.student.paymentPattern,
                element: guardStudent(wrap(<PaymentPage />)),
            },
            {
                path: APP_ROUTES.student.messages,
                element: guardStudent(wrap(<MessagesPage />)),
            },
            {
                path: APP_ROUTES.student.notifications,
                element: guardStudent(wrap(<NotificationsPage />)),
            },
        ],
    },
    {
        path: APP_ROUTES.instructor.dashboard,
        element: guardInstructor(wrap(<InstructorLayout />)),
        children: [
            { index: true, element: wrap(<InstructorDashboardPage />) },
            { path: 'courses', element: wrap(<InstructorMyCoursesPage />) },
            { path: 'courses/new', element: wrap(<CourseEditorPage />) },
            { path: APP_ROUTES.instructor.editCoursePattern, element: wrap(<CourseEditorPage />) },
            { path: 'earnings', element: wrap(<InstructorEarningsPage />) },
            { path: 'messages', element: wrap(<MessagesPage displayTitle={false} />) },
        ],
    },
    {
        path: APP_ROUTES.admin.dashboard,
        element: <RequireRole roles={[UserRole.Admin]}>{wrap(<AdminLayout />)}</RequireRole>,
        children: [
            { index: true, element: wrap(<AdminDashboardPage />) },
            { path: 'users', element: wrap(<UserManagementPage />) },
            { path: 'courses', element: wrap(<CourseModerationPage />) },
            { path: 'applications', element: wrap(<InstructorApplicationsPage />) },
            { path: 'payments', element: wrap(<PaymentHistoryPage />) },
            { path: 'categories', element: wrap(<CategoryManagementPage />) },
            { path: 'messages', element: wrap(<MessagesPage displayTitle={false} />) },
        ],
    },
    {
        path: APP_ROUTES.student.learnCoursePattern,
        element: guardStudent(<CourseLayout />),
        children: [
            { index: true, element: wrap(<CourseStartPage />) },
            { path: APP_ROUTES.student.learnLessonPattern, element: wrap(<CoursePlayerPage />) },
            { path: APP_ROUTES.student.testLessonPattern, element: wrap(<TestLessonPage />) },
        ],
    },
]);

export function AppRouter() {
    return <RouterProvider router={router} />;
}
