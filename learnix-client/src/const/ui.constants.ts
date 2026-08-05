/**
 * staleTime for queries whose data rarely changes mid-session — a user's own profile, an
 * instructor's public profile, the category list, featured courses, the catalog course count.
 * Named once so five hooks read as one deliberate choice instead of five coincidentally equal
 * magic numbers.
 */
export const RARELY_CHANGING_STALE_TIME = 1000 * 60 * 5;

export const PAGINATION = {
    DEFAULT: 20,
    CATALOG: 12,
    APPLICATIONS: 10,
    DASHBOARD_RECENT: 5,
} as const;

/**
 * Page-size options for the course catalog. Mobile is capped so a small screen never has to
 * render (and scroll through) 48+ cards at once.
 */
export const CATALOG_PAGE_SIZES = {
    desktop: [12, 24, 48],
    mobile: [12, 24],
} as const;

/**
 * Courses per page on an instructor's public profile. Smaller on a phone: the grid collapses to one
 * card per row there, so a desktop page of twelve becomes twelve screens of scrolling.
 */
export const INSTRUCTOR_COURSES_PAGE_SIZE = {
    desktop: 12,
    mobile: 6,
} as const;

/**
 * Fraction of a field's maxLength at which the character counter switches from muted to the
 * warning tone. Below it the counter is quiet; at 100% it turns destructive.
 */
export const CHAR_COUNTER_WARNING_RATIO = 0.9;

/**
 * Fraction at which an opt-in (`showCharLimit="nearLimit"`) counter appears at all.
 *
 * A limit generous enough that nobody reaches it — a 1000-character review — turns its counter into
 * furniture: it reads `0/1000` before a word is typed, which states a target rather than a ceiling.
 * Fields like that reveal the counter only once the ceiling is in range, leaving a muted stretch
 * here before CHAR_COUNTER_WARNING_RATIO takes it yellow.
 */
export const CHAR_COUNTER_REVEAL_RATIO = 0.8;

/**
 * lucide-react `size` values for the Header and its mobile drawer (MobileMenu), keyed by role
 * rather than one flat number — a dropdown-list icon and a menu-toggle icon are visually
 * distinct elements that happen to share a component, and collapsing them into a single
 * constant would force an unrelated change every time only one of them needs to move.
 */
export const HEADER_ICON_SIZE = {
    menuToggle: 24,
    action: 20,
    dropdownItem: 14,
} as const;

/**
 * lucide-react `size` values shared by DashboardLayout and the role layouts built on it
 * (AdminLayout, InstructorLayout) — the sidebar nav items, the account block, and the mobile
 * header's menu toggle.
 */
export const SIDEBAR_ICON_SIZE = {
    navItem: 16,
    mobileToggle: 20,
} as const;

/**
 * Icon size for AdminLayout's sidebar badge (ShieldCheck). Matches BrandLogo's own default icon
 * size (`size-6` = 24px) so the admin area's mark reads the same scale as the Header logo and
 * InstructorLayout's (which renders BrandLogo with no override, so it already gets this for free).
 */
export const SIDEBAR_LOGO_ICON_SIZE = 24;

/**
 * BrandLogo's icon size in the course player header — smaller than the default (24px) because the
 * player's top bar is a compact utility strip, not the primary navigation.
 */
export const COURSE_PLAYER_LOGO_ICON_SIZE = 20;

/**
 * lucide-react `size` values for NotificationsPage — the per-type icon on each list item, and the
 * muted icon shown in the empty state.
 */
export const NOTIFICATION_ICON_SIZE = {
    typeBadge: 16,
    emptyState: 24,
} as const;
