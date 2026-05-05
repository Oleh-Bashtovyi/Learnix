import { Link } from 'react-router-dom';
import type { CourseSummaryDto } from '@/types/course.types';
import { CourseCard } from '@/components/common/CourseCard';
import { LANDING_PAGE } from '@/const/localization/landingPage';

interface FeaturedCoursesSectionProps {
    courses: CourseSummaryDto[];
    isLoading?: boolean;
}

const { FEATURED_COURSES } = LANDING_PAGE;

export function FeaturedCoursesSection({ courses, isLoading }: FeaturedCoursesSectionProps) {
    return (
        <section id="courses" className="bg-secondary/40 py-20">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-10 flex items-end justify-between">
                    <div>
                        <span className="text-sm font-semibold text-primary">
                            {FEATURED_COURSES.tag}
                        </span>
                        <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">
                            {FEATURED_COURSES.heading}
                        </h2>
                        <p className="mt-2 text-muted-foreground">{FEATURED_COURSES.subtitle}</p>
                    </div>
                    <Link to="/courses" className="text-sm text-primary hover:underline">
                        {FEATURED_COURSES.viewAll}
                    </Link>
                </div>

                {isLoading ? (
                    <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                        {Array.from({ length: 6 }).map((_, i) => (
                            <div
                                key={i}
                                className="h-72 animate-pulse rounded-xl border border-border bg-card"
                            />
                        ))}
                    </div>
                ) : (
                    <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                        {courses.map((course) => (
                            <CourseCard key={course.id} course={course} />
                        ))}
                    </div>
                )}

                <div className="mt-10 text-center">
                    <Link
                        to="/courses"
                        className="inline-flex items-center gap-2 font-medium text-primary hover:underline"
                    >
                        {FEATURED_COURSES.viewMore}
                    </Link>
                </div>
            </div>
        </section>
    );
}
