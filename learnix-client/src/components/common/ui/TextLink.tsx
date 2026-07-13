import { Link, type LinkProps } from 'react-router-dom';
import { TEXT_LINK_BASE } from '@/components/common/ui/textLinkStyles';
import { cn } from '@/utils/cn';

export function TextLink({ className, children, ...props }: LinkProps) {
    return (
        <Link className={cn(TEXT_LINK_BASE, className)} {...props}>
            {children}
        </Link>
    );
}
