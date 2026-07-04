/**
 * Prettier configuration for Learnix frontend.
 *
 * Related ADRs:
 * - ADR-FRONT-LINT-002: Prettier Integration as the Sole Formatter
 * - ADR-FRONT-LINT-003: Automated Import Sorting
 *
 * When modifying this file, review the decisions documented in:
 * docs/frontend/decisions/LINTING_FORMATTING.md
 */

/** @type {import("prettier").Config} */
const config = {
    semi: true,
    singleQuote: true,
    trailingComma: 'all',
    printWidth: 100,
    tabWidth: 4,
    importOrder: ['^react', '<THIRD_PARTY_MODULES>', '^@/(.*)$', '^[./]'],
    importOrderSeparation: false,
    importOrderSortSpecifiers: true,
    plugins: ['@trivago/prettier-plugin-sort-imports', 'prettier-plugin-tailwindcss'],
};

export default config;
