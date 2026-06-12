import { useParams, Link } from 'react-router-dom';
import { ArrowLeft, User } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useUserProfile } from '@/hooks/useUserProfile';
import { useInstructorCourses } from '@/hooks/useInstructorCourses';
import { CourseCard } from '@/components/common/CourseCard';

export default function InstructorProfilePage() {
    const { t } = useTranslation('instructorProfile');
    const { instructorId } = useParams<{ instructorId: string }>();

    const { data: profile, isLoading: profileLoading } = useUserProfile(instructorId!);
    const { data: coursesData, isLoading: coursesLoading } = useInstructorCourses(instructorId!);

    const isLoading = profileLoading || coursesLoading;
    const courses = coursesData?.items ?? [];

    if (isLoading) {
        return (
            <div className="mx-auto max-w-5xl px-6 py-12">
                <div className="animate-pulse space-y-6">
                    <div className="flex items-center gap-5">
                        <div className="h-20 w-20 rounded-full bg-muted" />
                        <div className="space-y-2">
                            <div className="h-6 w-48 rounded bg-muted" />
                            <div className="h-4 w-64 rounded bg-muted" />
                        </div>
                    </div>
                    <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
                        {Array.from({ length: 3 }).map((_, i) => (
                            <div key={i} className="aspect-[4/3] rounded-xl bg-muted" />
                        ))}
                    </div>
                </div>
            </div>
        );
    }

    if (!profile) {
        return (
            <div className="mx-auto max-w-5xl px-6 py-20 text-center">
                <p className="text-muted-foreground">{t('notFound')}</p>
                <Link to="/courses" className="mt-4 inline-block text-primary hover:underline">
                    {t('backToCatalog')}
                </Link>
            </div>
        );
    }

    const fullName = `${profile.firstName} ${profile.lastName}`;

    return (
        <div className="mx-auto max-w-5xl px-6 py-12">
            <Link
                to="/courses"
                className="mb-8 inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
            >
                <ArrowLeft className="h-4 w-4" />
                {t('backToCatalog')}
            </Link>

            {/* Profile header */}
            <div className="mb-10 flex items-start gap-6">
                <div className="shrink-0">
                    {profile.avatarUrl ? (
                        <img
                            src={profile.avatarUrl}
                            alt={fullName}
                            className="h-20 w-20 rounded-full object-cover ring-2 ring-border"
                        />
                    ) : (
                        <div className="flex h-20 w-20 items-center justify-center rounded-full bg-muted ring-2 ring-border">
                            <User className="h-10 w-10 text-muted-foreground" />
                        </div>
                    )}
                </div>
                <div>
                    <h1 className="font-heading text-2xl font-bold text-foreground">{fullName}</h1>
                    {profile.bio && (
                        <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
                            {profile.bio}
                        </p>
                    )}
                    <p className="mt-2 text-sm text-muted-foreground">
                        {courses.length} {courses.length === 1 ? 'course' : 'courses'}
                    </p>
                </div>
            </div>

            {/* Courses */}
            <section>
                <h2 className="mb-5 font-heading text-xl font-semibold text-foreground">
                    {t('coursesHeading')}
                </h2>

                {courses.length === 0 ? (
                    <p className="text-sm text-muted-foreground">{t('noCourses')}</p>
                ) : (
                    <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
                        {courses.map((course) => (
                            <CourseCard key={course.id} course={course} />
                        ))}
                    </div>
                )}
            </section>
        </div>
    );
}
