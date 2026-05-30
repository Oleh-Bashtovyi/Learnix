import { Heart } from 'lucide-react';
import { Link } from 'react-router-dom';
import { cn } from '@/utils/cn';
import { HEADER } from '@/const/localization/header';

export function WishlistButton() {
    return (
        <Link
            to="/wishlist"
            aria-label={HEADER.WISHLIST_ARIA_LABEL}
            className={cn(
                'relative inline-flex items-center justify-center rounded-md p-2',
                'text-muted-foreground transition-colors hover:bg-muted hover:text-foreground',
            )}
        >
            <Heart className="h-5 w-5" />
        </Link>
    );
}
