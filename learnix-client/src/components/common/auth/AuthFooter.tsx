import { TextLink } from '@/components/common/ui/TextLink';

interface AuthFooterProps {
    text?: string;
    linkText: string;
    linkTo: string;
    linkState?: unknown;
}

export function AuthFooter({ text, linkText, linkTo, linkState }: AuthFooterProps) {
    if (text) {
        return (
            <p className="mt-5 text-center text-sm text-muted-foreground">
                {text}{' '}
                <TextLink to={linkTo} state={linkState}>
                    {linkText}
                </TextLink>
            </p>
        );
    }

    return (
        <div className="mt-5 text-center">
            <TextLink to={linkTo} state={linkState} className="text-sm">
                {linkText}
            </TextLink>
        </div>
    );
}
