export const ADMIN = {
    // ── Layout ─────────────────────────────────────────────────────────────────

    NAV_DASHBOARD: 'Dashboard',
    NAV_USERS: 'User management',
    NAV_COURSES: 'Course moderation',
    NAV_APPLICATIONS: 'Instructor applications',
    NAV_PAYMENTS: 'Payment history',
    NAV_BACK_TO_SITE: 'Back to site',
    NAV_SIGN_OUT: 'Sign out',

    // ── Dashboard ──────────────────────────────────────────────────────────────

    DASHBOARD_TITLE: 'Admin dashboard',
    DASHBOARD_SUBTITLE: 'Platform overview',

    STAT_TOTAL_USERS: 'Total users',
    STAT_TOTAL_COURSES: 'Total courses',
    STAT_PENDING_APPS: 'Pending applications',
    STAT_REVENUE: 'Mock revenue',

    QUICK_USERS_TITLE: 'User management',
    QUICK_USERS_DESC: 'Manage users, roles, and access control',
    QUICK_COURSES_TITLE: 'Course moderation',
    QUICK_COURSES_DESC: 'Review, unpublish, and delete platform courses',
    QUICK_APPLICATIONS_TITLE: 'Instructor applications',
    QUICK_APPLICATIONS_DESC: 'Approve or reject pending instructor applications',
    QUICK_PAYMENTS_TITLE: 'Payment history',
    QUICK_PAYMENTS_DESC: 'View mock payment transactions',

    // ── User management ────────────────────────────────────────────────────────

    USERS_TITLE: 'User management',
    USERS_SUBTITLE: 'Search, ban, and manage platform users',
    USERS_SEARCH: 'Search by name or email...',

    COL_USER: 'User',
    COL_ROLES: 'Roles',
    COL_STATUS: 'Status',
    COL_JOINED: 'Joined',
    COL_ACTIONS: '',

    STATUS_ACTIVE: 'Active',
    STATUS_BANNED: 'Banned',
    STATUS_DELETED: 'Deleted',

    BTN_BAN: 'Ban user',
    BTN_UNBAN: 'Unban user',
    BTN_DELETE: 'Delete user',
    BTN_RECOVER: 'Recover user',
    BTN_CHANGE_ROLE: 'Manage roles',

    EMPTY_USERS: 'No users found.',

    CONFIRM_BAN: (name: string) => `Ban "${name}"? They will not be able to log in.`,
    CONFIRM_UNBAN: (name: string) => `Unban "${name}"?`,
    CONFIRM_DELETE: (name: string) => `Soft-delete "${name}"? This action can be recovered later.`,
    CONFIRM_RECOVER: (name: string) => `Recover "${name}"?`,

    TOAST_BANNED: 'User banned',
    TOAST_UNBANNED: 'User unbanned',
    TOAST_USER_DELETED: 'User deleted',
    TOAST_USER_RECOVERED: 'User recovered',
    TOAST_ROLE_ASSIGNED: (role: string) => `Role "${role}" assigned`,
    TOAST_ROLE_REMOVED: (role: string) => `Role "${role}" removed`,

    // ── Change role dialog ─────────────────────────────────────────────────────

    ROLE_DIALOG_TITLE: 'Manage roles',
    ROLE_DIALOG_CURRENT: 'Current roles',
    ROLE_DIALOG_ADD_LABEL: 'Assign role',
    ROLE_DIALOG_ADD_BTN: 'Assign',
    ROLE_DIALOG_CLOSE: 'Close',
    ROLE_DIALOG_NO_ROLES: 'No roles assigned',

    // ── Course moderation ──────────────────────────────────────────────────────

    COURSES_TITLE: 'Course moderation',
    COURSES_SUBTITLE: 'Review, unpublish, and delete platform courses',
    COURSES_SEARCH: 'Search courses...',
    COURSES_SHOW_DELETED: 'Show deleted',

    COL_COURSE: 'Course',
    COL_INSTRUCTOR: 'Instructor ID',
    COL_COURSE_STATUS: 'Status',
    COL_ENROLLMENTS: 'Enrolled',
    COL_PRICE: 'Price',

    COURSE_FREE: 'Free',
    COURSE_STATUS_PUBLISHED: 'Published',
    COURSE_STATUS_DRAFT: 'Draft',
    COURSE_STATUS_ARCHIVED: 'Archived',
    COURSE_STATUS_DELETED: 'Deleted',

    BTN_UNPUBLISH: 'Unpublish',
    BTN_DELETE_COURSE: 'Delete',
    BTN_RECOVER_COURSE: 'Recover',

    EMPTY_COURSES: 'No courses found.',

    CONFIRM_UNPUBLISH: (title: string) => `Unpublish "${title}"?`,
    CONFIRM_DELETE_COURSE: (title: string) =>
        `Delete course "${title}"? This can be recovered later.`,
    CONFIRM_RECOVER_COURSE: (title: string) => `Recover course "${title}"?`,

    TOAST_UNPUBLISHED: 'Course unpublished',
    TOAST_COURSE_DELETED: 'Course deleted',
    TOAST_COURSE_RECOVERED: 'Course recovered',

    // ── Instructor applications ────────────────────────────────────────────────

    APPLICATIONS_TITLE: 'Instructor applications',
    APPLICATIONS_SUBTITLE:
        'Review pending applications from students who want to become instructors',
    APPLICATIONS_LOADING: 'Loading...',
    APPLICATIONS_EMPTY: 'No pending applications.',
    APPLICATIONS_EMPTY_SUB: 'All applications have been reviewed.',

    APP_MOTIVATION_LABEL: 'Motivation',
    APP_PORTFOLIO_LABEL: 'Portfolio',
    APP_SUBMITTED: 'Submitted',

    BTN_APPROVE: 'Approve',
    BTN_REJECT: 'Reject',

    TOAST_APPROVED: 'Application approved',
    TOAST_REJECTED: 'Application rejected',

    // ── Reject dialog ──────────────────────────────────────────────────────────

    REJECT_DIALOG_TITLE: 'Reject application',
    REJECT_DIALOG_SUBTITLE: (name: string) => `Reject ${name}'s instructor application?`,
    REJECT_REASON_LABEL: 'Rejection reason (optional)',
    REJECT_REASON_PLACEHOLDER:
        'Explain why the application was rejected. The applicant will see this message.',
    REJECT_BTN_CANCEL: 'Cancel',
    REJECT_BTN_CONFIRM: 'Reject application',

    // ── Payment history ────────────────────────────────────────────────────────

    PAYMENTS_TITLE: 'Payment history',
    PAYMENTS_SUBTITLE: 'All payment transactions on the platform',
    PAYMENTS_MOCK_BADGE: 'Mock data — no Stripe integration yet',

    COL_PAYER: 'User',
    COL_COURSE_TITLE: 'Course',
    COL_AMOUNT: 'Amount',
    COL_PAY_STATUS: 'Status',
    COL_DATE: 'Date',

    PAY_FILTER_ALL: 'All',
    PAY_FILTER_COMPLETED: 'Completed',
    PAY_FILTER_PENDING: 'Pending',
    PAY_FILTER_FAILED: 'Failed',

    PAY_STATUS_COMPLETED: 'Completed',
    PAY_STATUS_PENDING: 'Pending',
    PAY_STATUS_FAILED: 'Failed',

    EMPTY_PAYMENTS: 'No payments found.',

    // ── Pagination ─────────────────────────────────────────────────────────────

    PREV: '← Previous',
    NEXT: 'Next →',
    PAGE_OF: (page: number, total: number) => `Page ${page} of ${total}`,
} as const;
