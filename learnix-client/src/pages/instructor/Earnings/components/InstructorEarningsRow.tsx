import { TableCell, TableRow } from '@/components/ui/table';
import type { CourseEarningsDto } from '@/types/payment.types';

interface InstructorEarningsRowProps {
    course: CourseEarningsDto;
}

export function InstructorEarningsRow({ course }: InstructorEarningsRowProps) {
    return (
        <TableRow>
            <TableCell className="font-medium text-foreground">{course.courseTitle}</TableCell>
            <TableCell className="text-muted-foreground">{course.paymentsCount}</TableCell>
            <TableCell className="font-semibold text-foreground">
                ${course.totalAmount.toFixed(2)}
            </TableCell>
            <TableCell className="text-muted-foreground">
                {new Date(course.lastPaymentAt).toLocaleDateString()}
            </TableCell>
        </TableRow>
    );
}
