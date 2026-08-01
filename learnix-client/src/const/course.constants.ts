export const COURSE_LIMITS = {
    TITLE_MIN: 3,
    TITLE_MAX: 200,
    DESCRIPTION_MIN: 10,
    DESCRIPTION_MAX: 5000,
    PRICE_MIN: 0,
    TAG_MAX_LENGTH: 50,
    TAGS_MAX_COUNT: 10,
} as const;

/** One-click price points offered under the price field. 0 is rendered as "Free". */
export const COURSE_PRICE_PRESETS = [0, 20, 50, 100] as const;
