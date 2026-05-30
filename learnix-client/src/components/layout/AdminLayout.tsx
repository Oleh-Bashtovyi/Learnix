import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { AiChatWidget } from '@/components/common/AiChatWidget/AiChatWidget';
import {
    LayoutDashboard,
    Users,
    BookOpen,
    FileCheck,
    CreditCard,
    ArrowLeft,
    LogOut,
} from 'lucide-react';
import { cn } from '@/utils/cn';
import { useAuthStore } from '@/store/auth.store';
import { ADMIN } from '@/const/localization/admin';

interface NavItem {
    to: string;
    label: string;
    icon: React.ReactNode;
    end?: boolean;
}

const navItems: NavItem[] = [
    {
        to: '/admin',
        label: ADMIN.NAV_DASHBOARD,
        icon: <LayoutDashboard size={16} />,
        end: true,
    },
    { to: '/admin/users', label: ADMIN.NAV_USERS, icon: <Users size={16} /> },
    { to: '/admin/courses', label: ADMIN.NAV_COURSES, icon: <BookOpen size={16} /> },
    {
        to: '/admin/applications',
        label: ADMIN.NAV_APPLICATIONS,
        icon: <FileCheck size={16} />,
    },
    { to: '/admin/payments', label: ADMIN.NAV_PAYMENTS, icon: <CreditCard size={16} /> },
];

export function AdminLayout() {
    const navigate = useNavigate();
    const { logout } = useAuthStore();
    const queryClient = useQueryClient();

    function handleSignOut() {
        logout();
        queryClient.clear();
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
                            <div className="grid h-8 w-8 place-items-center rounded-lg bg-destructive text-sm font-bold text-destructive-foreground">
                                A
                            </div>
                            <span>Learnix Admin</span>
                        </Link>
                    </div>

                    <div className="flex-1 px-3 py-2">
                        <p className="mb-2 px-2 text-xs uppercase tracking-wider text-muted-foreground">
                            Admin
                        </p>
                        <nav className="space-y-1 text-sm">
                            {navItems.map((item) => (
                                <NavLink
                                    key={item.to}
                                    to={item.to}
                                    end={item.end}
                                    className={({ isActive }) =>
                                        cn(
                                            'flex items-center gap-2.5 rounded-lg px-3 py-2 transition-colors',
                                            isActive
                                                ? 'bg-destructive/10 font-medium text-destructive'
                                                : 'text-foreground hover:bg-secondary',
                                        )
                                    }
                                >
                                    {item.icon}
                                    {item.label}
                                </NavLink>
                            ))}
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
                                {ADMIN.NAV_BACK_TO_SITE}
                            </Link>
                            <button
                                onClick={handleSignOut}
                                className="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                            >
                                <LogOut size={16} />
                                {ADMIN.NAV_SIGN_OUT}
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
