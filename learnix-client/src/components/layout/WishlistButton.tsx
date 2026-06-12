import { Heart } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { cn } from '@/utils/cn';

export function WishlistButton() {
    const { t } = useTranslation('header');

    return (
        <Link
            to="/wishlist"
            aria-label={t('wishlistAriaLabel')}
            className={cn(
                'relative inline-flex items-center justify-center rounded-md p-2',
                'text-muted-foreground transition-colors hover:bg-muted hover:text-foreground',
            )}
        >
            <Heart className="h-5 w-5" />
        </Link>
    );
}
