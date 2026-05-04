import { Link } from 'react-router-dom';
import type { CourseSummaryDto } from '@/types/course.types';
import { CourseCard } from '@/components/common/CourseCard';

interface FeaturedCoursesSectionProps {
    courses: CourseSummaryDto[];
}

export function FeaturedCoursesSection({ courses }: FeaturedCoursesSectionProps) {
    return (
        <section id="courses" className="bg-secondary/40 py-20">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-10 flex items-end justify-between">
                    <div>
                        <span className="text-sm font-semibold text-primary">Popular</span>
                        <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">
                            Top courses this week
                        </h2>
                        <p className="mt-2 text-muted-foreground">Hand-picked by our community of learners</p>
                    </div>
                    <Link to="/courses" className="text-sm text-primary hover:underline">
                        View all courses →
                    </Link>
                </div>

                <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                    {courses.map((course) => (
                        <CourseCard key={course.id} course={course} />
                    ))}
                </div>

                <div className="mt-10 text-center">
                    <Link
                        to="/courses"
                        className="inline-flex items-center gap-2 font-medium text-primary hover:underline"
                    >
                        View 1,200+ more courses →
                    </Link>
                </div>
            </div>
        </section>
    );
}
