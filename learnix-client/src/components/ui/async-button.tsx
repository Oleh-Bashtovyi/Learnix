import * as React from 'react';
import { Check, Loader2 } from 'lucide-react';
import { Button, type ButtonProps } from '@/components/ui/button';
import { cn } from '@/utils/cn';

export interface AsyncButtonProps extends ButtonProps {
    isLoading?: boolean;
    isSuccess?: boolean;
    loadingText?: React.ReactNode;
}

const AsyncButton = React.forwardRef<HTMLButtonElement, AsyncButtonProps>(
    (
        { className, isLoading, isSuccess, loadingText, children, disabled, onClick, ...props },
        ref,
    ) => {
        const handleClick = (e: React.MouseEvent<HTMLButtonElement>) => {
            if (isLoading || isSuccess) {
                e.preventDefault();
                return;
            }
            if (onClick) {
                onClick(e);
            }
        };

        return (
            <Button
                className={cn(
                    isSuccess &&
                        'border-success bg-success text-white hover:bg-success/90 focus-visible:ring-success disabled:opacity-100',
                    isLoading && 'opacity-80',
                    className,
                )}
                ref={ref}
                disabled={isLoading || isSuccess || disabled}
                onClick={handleClick}
                {...props}
            >
                {isLoading && <Loader2 className="mr-2 size-4 animate-spin" />}
                {isSuccess ? (
                    <Check className="size-5" />
                ) : isLoading && loadingText ? (
                    loadingText
                ) : (
                    children
                )}
            </Button>
        );
    },
);
AsyncButton.displayName = 'AsyncButton';

export { AsyncButton };
