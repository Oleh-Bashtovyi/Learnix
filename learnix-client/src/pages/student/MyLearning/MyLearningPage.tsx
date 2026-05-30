import { Link } from 'react-router-dom';
import { BookOpen } from 'lucide-react';

import { useMyEnrollments } from '@/hooks/useMyEnrollments';
import { MY_LEARNING } from '@/const/localization/myLearning';
import { EnrolledCourseCard } from './components/EnrolledCourseCard';

export default function MyLearningPage() {
    const { data, isLoading } = useMyEnrollments();

    return (
        <div className="mx-auto max-w-7xl px-6 py-10">
            <h1 className="font-heading text-3xl font-bold md:text-4xl">{MY_LEARNING.title}</h1>

            {isLoading ? (
                <div className="mt-8 grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                    {[1, 2, 3, 4].map((i) => (
                        <div key={i} className="h-[280px] animate-pulse rounded-xl bg-card" />
                    ))}
                </div>
            ) : data?.items.length === 0 ? (
                <div className="mt-16 text-center">
                    <div className="mx-auto flex h-24 w-24 items-center justify-center rounded-full bg-accent/10">
                        <BookOpen className="h-12 w-12 text-accent" />
                    </div>
                    <h2 className="mt-6 font-heading text-2xl font-bold">
                        {MY_LEARNING.emptyTitle}
                    </h2>
                    <p className="mt-2 text-muted-foreground">{MY_LEARNING.emptyDescription}</p>
                    <Link
                        to="/courses"
                        className="mt-8 inline-flex h-11 items-center justify-center rounded-md bg-primary px-8 font-medium text-primary-foreground hover:bg-primary/90"
                    >
                        {MY_LEARNING.browseCourses}
                    </Link>
                </div>
            ) : (
                <div className="mt-8 grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                    {data?.items.map((enrollment) => (
                        <EnrolledCourseCard key={enrollment.enrollmentId} enrollment={enrollment} />
                    ))}
                </div>
            )}
        </div>
    );
}
