import { Badge } from '@/components/ui/badge';
import { TableCell, TableRow } from '@/components/ui/table';
import type { MockPaymentDto } from '@/types/admin.types';
import { cn } from '@/utils/cn';

interface PaymentHistoryRowProps {
    payment: MockPaymentDto;
    statusLabel: string;
}

const STATUS_STYLES: Record<MockPaymentDto['status'], string> = {
    Completed: 'bg-success/20 text-success',
    Pending: 'bg-warning/20 text-warning',
    Failed: 'bg-destructive/10 text-destructive',
};

export function PaymentHistoryRow({ payment: p, statusLabel }: PaymentHistoryRowProps) {
    return (
        <TableRow className="hover:bg-secondary/30">
            <TableCell>
                <p className="font-medium text-foreground">{p.userName}</p>
                <p className="text-xs text-muted-foreground">{p.userEmail}</p>
            </TableCell>
            <TableCell className="text-foreground">{p.courseTitle}</TableCell>
            <TableCell className="font-medium text-foreground">${p.amount.toFixed(2)}</TableCell>
            <TableCell>
                <Badge
                    variant={
                        p.status === 'Completed'
                            ? 'outline'
                            : p.status === 'Failed'
                              ? 'destructive'
                              : 'secondary'
                    }
                    className={cn('border-transparent', STATUS_STYLES[p.status])}
                >
                    {statusLabel}
                </Badge>
            </TableCell>
            <TableCell className="text-muted-foreground">
                {new Date(p.createdAt).toLocaleDateString()}
            </TableCell>
        </TableRow>
    );
}
