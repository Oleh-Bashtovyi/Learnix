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
