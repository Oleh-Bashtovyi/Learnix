import { Link, NavLink } from 'react-router-dom';
import { cn } from '@/utils/cn';

const navItems = [{ to: '/courses', label: 'Courses' }];

export function Header() {
    return (
        <header className="sticky top-0 z-40 border-b border-border bg-background/90 backdrop-blur">
            <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-6">
                <div className="flex items-center gap-10">
                    <Link to="/" className="flex items-center gap-2">
                        <div className="grid h-8 w-8 place-items-center rounded-lg bg-primary font-heading font-bold text-primary-foreground">
                            L
                        </div>
                        <span className="font-heading text-lg font-bold">Learnix</span>
                    </Link>
                    <nav className="hidden items-center gap-7 text-sm md:flex">
                        {navItems.map((item) => (
                            <NavLink
                                key={item.to}
                                to={item.to}
                                className={({ isActive }) =>
                                    cn(
                                        'transition-colors hover:text-primary',
                                        isActive ? 'text-foreground' : 'text-muted-foreground',
                                    )
                                }
                            >
                                {item.label}
                            </NavLink>
                        ))}
                    </nav>
                </div>
                <div className="flex items-center gap-3">
                    <Link
                        to="/login"
                        className="hidden text-sm text-foreground hover:text-primary md:block"
                    >
                        Log in
                    </Link>
                    <Link
                        to="/register"
                        className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                    >
                        Get started
                    </Link>
                </div>
            </div>
        </header>
    );
}
