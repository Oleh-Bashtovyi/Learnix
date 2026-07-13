/**
 * The look of an inline text link, shared by TextLink (which navigates) and TextButton (which
 * performs an action). They must be indistinguishable to the eye and distinct to the DOM: a thing
 * that goes somewhere is an <a>, a thing that does something is a <button>, and no amount of
 * styling changes which one a screen reader or a middle-click should get.
 */
export const TEXT_LINK_BASE =
    'rounded-sm font-medium text-link transition-colors hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-link focus-visible:ring-offset-2 focus-visible:ring-offset-background';
