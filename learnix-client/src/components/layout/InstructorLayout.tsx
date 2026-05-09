import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { AiChatWidget } from '@/components/common/AiChatWidget/AiChatWidget';
import {
    LayoutDashboard,
    BookOpen,
    PlusCircle,
    MessageSquare,
    TrendingUp,
    ArrowLeft,
    LogOut,
} from 'lucide-react';
import { cn } from '@/utils/cn';
import { useAuthStore } from '@/store/auth.store';
import { INSTRUCTOR } from '@/const/localization/instructor';

interface NavItem {
    to: string;
    label: string;
    icon: React.ReactNode;
    disabled?: boolean;
    end?: boolean;
}

const navItems: NavItem[] = [
    {
        to: '/instructor',
        label: INSTRUCTOR.NAV_DASHBOARD,
        icon: <LayoutDashboard size={16} />,
        end: true,
    },
    { to: '/instructor/courses', label: INSTRUCTOR.NAV_MY_COURSES, icon: <BookOpen size={16} /> },
    {
        to: '/instructor/courses/new',
        label: INSTRUCTOR.NAV_NEW_COURSE,
        icon: <PlusCircle size={16} />,
    },
    { to: '#', label: INSTRUCTOR.NAV_MESSAGES, icon: <MessageSquare size={16} />, disabled: true },
    { to: '#', label: INSTRUCTOR.NAV_EARNINGS, icon: <TrendingUp size={16} />, disabled: true },
];

export function InstructorLayout() {
    const navigate = useNavigate();
    const { logout } = useAuthStore();

    function handleSignOut() {
        logout();
        navigate('/login');
    }

    return (
        <>
            <div className="grid min-h-screen grid-cols-[240px_1fr] bg-background">
                {/* Sidebar */}
                <aside className="flex flex-col border-r border-border bg-card">
                    <div className="flex items-center gap-2 px-4 py-5">
                        <Link
                            to="/"
                            className="flex items-center gap-2 font-heading font-bold text-foreground"
                        >
                            <div className="grid h-8 w-8 place-items-center rounded-lg bg-primary text-sm font-bold text-primary-foreground">
                                L
                            </div>
                            <span>Learnix</span>
                        </Link>
                    </div>

                    <div className="flex-1 px-3 py-2">
                        <p className="mb-2 px-2 text-xs uppercase tracking-wider text-muted-foreground">
                            Instructor
                        </p>
                        <nav className="space-y-1 text-sm">
                            {navItems.map((item) =>
                                item.disabled ? (
                                    <span
                                        key={item.label}
                                        className="flex cursor-not-allowed items-center gap-2.5 rounded-lg px-3 py-2 text-muted-foreground opacity-50"
                                    >
                                        {item.icon}
                                        {item.label}
                                    </span>
                                ) : (
                                    <NavLink
                                        key={item.to}
                                        to={item.to}
                                        end={item.end}
                                        className={({ isActive }) =>
                                            cn(
                                                'flex items-center gap-2.5 rounded-lg px-3 py-2 transition-colors',
                                                isActive
                                                    ? 'bg-primary/10 font-medium text-primary'
                                                    : 'text-foreground hover:bg-secondary',
                                            )
                                        }
                                    >
                                        {item.icon}
                                        {item.label}
                                    </NavLink>
                                ),
                            )}
                        </nav>
                    </div>

                    <div className="border-t border-border px-3 py-4">
                        <p className="mb-2 px-2 text-xs uppercase tracking-wider text-muted-foreground">
                            Account
                        </p>
                        <nav className="space-y-1 text-sm">
                            <Link
                                to="/"
                                className="flex items-center gap-2.5 rounded-lg px-3 py-2 text-foreground transition-colors hover:bg-secondary"
                            >
                                <ArrowLeft size={16} />
                                {INSTRUCTOR.NAV_BACK_TO_CATALOG}
                            </Link>
                            <button
                                onClick={handleSignOut}
                                className="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                            >
                                <LogOut size={16} />
                                {INSTRUCTOR.NAV_SIGN_OUT}
                            </button>
                        </nav>
                    </div>
                </aside>

                {/* Main content */}
                <main className="min-h-screen overflow-y-auto">
                    <Outlet />
                </main>
            </div>
            <AiChatWidget />
        </>
    );
}
