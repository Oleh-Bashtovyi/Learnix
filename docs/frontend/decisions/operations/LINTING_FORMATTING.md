# Learnix — Frontend Code Quality & Tooling Decision Records (ADR)

This document contains Architecture Decision Records (ADRs) related to frontend linting, code formatting, and automated tooling.

## ADR-FRONT-LINT-001: ESLint Flat Config & Strictness

**Decision:** We migrated the frontend to ESLint's new "Flat Config" architecture (`eslint.config.js`) and enforced strict TypeScript rules (e.g., forbidding explicit `any`). 

**Why:** 
- The legacy `.eslintrc` cascading resolution often led to invisible plugin conflicts and complex overrides. The Flat Config is explicit, deterministic, and natively supports modern ES Modules.
- Enforcing strict typings prevents downstream serialization errors when interacting with C# backend DTOs.
- Native integration with Vite via `eslint-plugin-react-refresh` ensures developers receive real-time feedback on Fast Refresh boundaries.

**Alternatives:** 
- Keeping the legacy `.eslintrc` was rejected because it is being deprecated by the ESLint core team.

**Consequences:** 
- All new linting rules must be defined in the `eslint.config.js` array format.
- Developers cannot use `any` and must properly type API responses using `unknown` or strictly typed DTO interfaces.

---

## ADR-FRONT-LINT-002: Prettier Integration as the Sole Formatter

**Decision:** We adopted Prettier as the absolute source of truth for code formatting, explicitly disabling all formatting-related ESLint rules using `eslint-config-prettier`.

**Why:**
- ESLint and Prettier often conflict over spacing, quotes, and line breaks. By separating concerns (ESLint for code quality/logic, Prettier for formatting), we eliminate developer friction.
- `eslint-config-prettier` acts as a fail-safe, traversing the ESLint config array and turning off conflicting stylistic rules.

**Alternatives:**
- Using ESLint for both logic and formatting (`eslint-plugin-prettier`) was rejected as it treats formatting issues as errors, unnecessarily failing CI pipelines for missing spaces.

**Consequences:**
- Formatting is guaranteed to be consistent across all IDEs and platforms.
- `eslintConfigPrettier` must always remain the final item in the `eslint.config.js` array to ensure it overrides all preceding plugins.

**Active Prettier config (`prettier.config.js`):**
- Standard rules: `singleQuote: true`, `semi: true`, `trailingComma: "all"`, `printWidth: 100`, `tabWidth: 4`
- Plugins enabled: `prettier-plugin-tailwindcss` and `@trivago/prettier-plugin-sort-imports`.

---

## ADR-FRONT-LINT-003: Automated Import Sorting

**Decision:** We implemented `@trivago/prettier-plugin-sort-imports` to automatically sort and group imports on save. We also explicitly configured `importOrderSeparation: false` to keep import blocks compact.

**Import order:**
1. `react` (and React ecosystem)
2. Third-party modules
3. Absolute aliases (`@/`)
4. Relative paths (`./`, `../`)

**Why:**
- Large files in React applications often suffer from chaotic import sections. Grouping imports logically drastically improves readability.
- Implementing this at the Prettier level ensures that import sorting is lightning-fast and occurs automatically during the formatting phase, rather than requiring a separate linting pass.

**Alternatives:**
- `eslint-plugin-import` or `eslint-plugin-simple-import-sort` were considered, but were rejected because they require running `eslint --fix`, whereas the Prettier plugin sorts imports instantly upon file save in modern IDEs.

**Consequences:**
- Developers no longer need to manually manage the order of imports.
- Absolute paths using the `@/` alias are naturally grouped together, reinforcing our architectural boundary rules.

---

## ADR-FRONT-LINT-004: Tailwind & React Strictness Plugins

**Decision:** We integrated `eslint-plugin-react`, `eslint-plugin-react-hooks`, and `eslint-plugin-tailwindcss` to enforce framework-specific best practices. We specifically utilize `tailwindcss/enforces-shorthand`.

**Why:**
- `eslint-plugin-react` catches common React pitfalls (e.g., missing `key` props, invalid HTML attributes).
- `eslint-plugin-react-hooks` enforces the Rules of Hooks and exhaustive deps in `useEffect`.
- `eslint-plugin-tailwindcss` prevents the usage of non-existent or conflicting utility classes. The `enforces-shorthand` rule automatically replaces verbose sizing classes (e.g., `h-4 w-4`) with modern Tailwind shorthand (e.g., `size-4`), keeping the DOM clean.

**Alternatives:**
- Relying solely on `prettier-plugin-tailwindcss` was rejected because Prettier only sorts classes; it does not validate if a class actually exists or if it conflicts with another.

**Consequences:**
- We disabled `'tailwindcss/no-custom-classname'` to allow integration with external animation libraries (like `tailwindcss-animate`).
- We disabled `'react/no-unescaped-entities'` to improve developer experience when writing plain text in JSX without being forced to use HTML entities (`&apos;`).
- `tailwindcss/enforces-shorthand` is active in the current config. It auto-fixes verbose sizing (e.g., `h-4 w-4` → `size-4`).

---

## ADR-FRONT-LINT-005: Unused Imports & Variables Enforcement

**Decision:** We use `eslint-plugin-unused-imports` to enforce removal of unused imports and variables. The native `@typescript-eslint/no-unused-vars` is disabled in favor of this plugin's stricter variant.

**Rules:**
- `unused-imports/no-unused-imports: "error"` — unused imports fail the build and are blocked by pre-commit hooks.
- `unused-imports/no-unused-vars: "error"` — unused variables fail the build. Variables and arguments prefixed with `_` are exempted (e.g., `_event`, `_index`).

**Why:**
- TypeScript's own unused variable checking (`noUnusedLocals`) only runs during `tsc`, not during `eslint`. This plugin catches unused imports during the lint phase, which runs in the pre-commit hook and is faster.
- The `_` prefix convention is a standard escape hatch for intentionally unused destructured parameters.
