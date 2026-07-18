import { useTranslation } from 'react-i18next';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';

interface CourseFilterOption {
    id: string;
    title: string;
}

interface CourseFilterProps {
    courses: CourseFilterOption[];
    /** With `includeAll`, an empty string means "all courses". */
    value: string;
    onChange: (courseId: string) => void;
    /** Whether to offer an "all courses" option. False for per-course charts like the drop-off curve. */
    includeAll?: boolean;
    disabled?: boolean;
}

// Radix Select forbids an empty item value, so "all" is the sentinel for the all-courses option and is
// mapped back to an empty string at the boundary.
const ALL = 'all';

/** "All courses / <course>" selector shared by the analytics tabs. */
export function CourseFilter({
    courses,
    value,
    onChange,
    includeAll = true,
    disabled,
}: CourseFilterProps) {
    const { t } = useTranslation('instructorAnalytics');

    return (
        <Select
            value={includeAll ? value || ALL : value}
            onValueChange={(v) => onChange(v === ALL ? '' : v)}
            disabled={disabled}
        >
            <SelectTrigger className="w-64">
                <SelectValue />
            </SelectTrigger>
            <SelectContent>
                {includeAll && <SelectItem value={ALL}>{t('filter.allCourses')}</SelectItem>}
                {courses.map((course) => (
                    <SelectItem key={course.id} value={course.id}>
                        {course.title}
                    </SelectItem>
                ))}
            </SelectContent>
        </Select>
    );
}
