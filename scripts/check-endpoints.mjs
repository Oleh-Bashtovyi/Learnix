/**
 * Keeps docs/backend/ENDPOINTS.md honest.
 *
 * The API surface is defined by the controllers and nowhere else. This script reads it straight
 * out of them ([Route], [Http*], [Authorize], [AllowAnonymous], [EnableRateLimiting]) and compares
 * it with the table in the doc.
 *
 *   node scripts/check-endpoints.mjs           verify (CI) — exits 1 on any drift
 *   node scripts/check-endpoints.mjs --write   regenerate the doc, preserving hand-written descriptions
 *
 * Descriptions are prose and are never verified — only method, path, auth and rate limit are, since
 * those are the facts the code can contradict.
 */
import { readFile, readdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const CONTROLLERS_DIR = path.join(ROOT, 'Learnix.Backend/Learnix.API/Controllers');
const DOC_PATH = path.join(ROOT, 'docs/backend/ENDPOINTS.md');

const WRITE = process.argv.includes('--write');

// Attributes may be written fully qualified — `[Microsoft.AspNetCore.Authorization.Authorize]` is
// the same attribute as `[Authorize]`, and missing that cost two endpoints their auth in the first
// generated table.
const AUTHORIZE_RE = /\[(?:[\w.]+\.)?Authorize([^\]]*)\]/;
const ANONYMOUS_RE = /\[(?:[\w.]+\.)?AllowAnonymous\]/;

/** `[Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]` → `Instructor, Admin`. */
function readRoles(attr) {
    const roles = [...attr.matchAll(/Roles\.(\w+)/g)].map((m) => m[1]);
    return roles.length ? roles.join(', ') : null;
}

/** Both `[Authorize(Policy = "EmailConfirmed")]` and `[Authorize(Policy = AuthPolicies.EmailConfirmed)]`. */
function readPolicy(attr) {
    const match = /Policy\s*=\s*(?:"([^"]+)"|(?:[\w.]+\.)?(\w+))/.exec(attr);
    return match ? (match[1] ?? match[2]) : null;
}

/** How an endpoint is protected, as one human-readable cell. */
function describeAuth({ anonymous, authorize }) {
    // [AllowAnonymous], or no [Authorize] at all — either way the endpoint is open.
    if (anonymous || !authorize) return 'Anonymous';
    const roles = readRoles(authorize);
    const policy = readPolicy(authorize);
    if (roles && policy) return `${roles} + ${policy}`;
    if (roles) return roles;
    if (policy) return `Authenticated + ${policy}`;
    return 'Authenticated';
}

function joinRoute(base, suffix) {
    const b = (base ?? '').replace(/^\/|\/$/g, '');
    const s = (suffix ?? '').replace(/^\/|\/$/g, '');
    return '/' + [b, s].filter(Boolean).join('/');
}

/** Route constraints are an implementation detail of matching: `{id:guid}` and `{id}` are one URL. */
function normalizePath(route) {
    // The API version segment is a route token (`v{version:apiVersion}`), not a resource id — inline
    // it as the one version that exists today so the doc reads as a real URL clients can call.
    return route
        .replace(/v\{version:apiVersion\}/, 'v1')
        .replace(/\{(\w+)(:[^}]+)?\}/g, '{$1}');
}

async function parseControllers() {
    const files = (await readdir(CONTROLLERS_DIR)).filter((f) => f.endsWith('Controller.cs'));
    const endpoints = [];

    for (const file of files.toSorted((a, b) => a.localeCompare(b))) {
        const source = await readFile(path.join(CONTROLLERS_DIR, file), 'utf8');
        const name = file.replace('Controller.cs', '');

        // Everything above the class declaration is the class's own attribute block.
        const classIndex = source.search(/^public\s+(sealed\s+)?class\s+\w+Controller/m);
        const header = source.slice(0, classIndex);
        const body = source.slice(classIndex);

        // Routing is case-insensitive, but the URLs clients actually call are lowercase — the
        // `[controller]` token would otherwise render `/api/Courses` here.
        const baseRoute = /\[Route\("([^"]+)"\)\]/
            .exec(header)?.[1]
            ?.replace('[controller]', name.toLowerCase());
        if (!baseRoute) continue;

        const classAuthorize = AUTHORIZE_RE.exec(header)?.[0] ?? null;
        const classAnonymous = ANONYMOUS_RE.test(header);

        // An action is an attribute block ending in [Http*], followed by more attributes.
        const actionRe =
            /\[Http(Get|Post|Put|Patch|Delete)(?:\("([^"]*)"\))?\]([\s\S]*?)public\s+[\w<>,\s?]+\s+(\w+)\s*\(/g;

        for (const m of body.matchAll(actionRe)) {
            const [, verb, suffix, between, action] = m;
            const methodAuthorize = AUTHORIZE_RE.exec(between)?.[0] ?? null;
            const methodAnonymous = ANONYMOUS_RE.test(between);
            const rateLimit =
                /\[EnableRateLimiting\((?:RateLimitPolicies\.)?(\w+)\)\]/.exec(between)?.[1] ??
                null;

            endpoints.push({
                controller: name,
                action,
                method: verb.toUpperCase(),
                path: normalizePath(joinRoute(baseRoute, suffix)),
                auth: describeAuth({
                    anonymous: methodAnonymous || (classAnonymous && !methodAuthorize),
                    authorize: methodAuthorize ?? (methodAnonymous ? null : classAuthorize),
                }),
                rateLimit: rateLimit ?? 'Default',
            });
        }
    }
    return endpoints;
}

/** Reads the existing doc so that regeneration does not throw away hand-written descriptions. */
async function readDoc() {
    let text;
    try {
        text = await readFile(DOC_PATH, 'utf8');
    } catch {
        return { rows: new Map(), descriptions: new Map() };
    }

    const rows = new Map();
    const descriptions = new Map();
    for (const line of text.split('\n')) {
        const cells = line.match(/^\|(.+)\|$/)?.[1].split('|').map((c) => c.trim());
        if (!cells || cells.length < 5) continue;
        const method = cells[0].replace(/`/g, '');
        if (!/^(GET|POST|PUT|PATCH|DELETE)$/.test(method)) continue;

        const key = `${method} ${cells[1].replace(/`/g, '')}`;
        rows.set(key, { auth: cells[2], rateLimit: cells[3].replace(/`/g, '') });
        descriptions.set(key, cells[4]);
    }
    return { rows, descriptions };
}

function render(endpoints, descriptions) {
    const byController = new Map();
    for (const e of endpoints) {
        if (!byController.has(e.controller)) byController.set(e.controller, []);
        byController.get(e.controller).push(e);
    }

    const out = [
        '# Learnix — API Surface',
        '',
        '> **Generated from the controllers by `scripts/check-endpoints.mjs`.** CI fails if this file',
        '> and `Learnix.API/Controllers/` disagree. Do not hand-edit method, path, auth or rate limit —',
        '> change the controller. The **Description** column is prose: edit it freely, it is preserved',
        '> across regeneration and never verified.',
        '',
        '`Rate limit` values are the policies in `Learnix.API/RateLimiting/RateLimitPolicies.cs`;',
        '`Default` means no `[EnableRateLimiting]` attribute — the global limiter applies.',
        '',
        '**`Auth` is only what the attributes say.** Resource-level authorization lives in the handlers',
        '(ADR-BACK-ARCH: authorization in handlers, not controllers) — `Authenticated` on a lesson or',
        'section mutation still means *course owner or admin*, enforced by `course.IsOwnerOrAdmin(...)`.',
        'Where that applies, the Description column says so.',
        '',
        'The *decisions* behind these endpoints live in `docs/backend/decisions/`; this file is only',
        'the surface they add up to.',
        '',
    ];

    for (const [controller, list] of [...byController].toSorted(([a], [b]) => a.localeCompare(b))) {
        out.push(`## ${controller}`, '');
        out.push('| Method | Endpoint | Auth | Rate limit | Description |');
        out.push('|---|---|---|---|---|');
        for (const e of list) {
            const key = `${e.method} ${e.path}`;
            const description = descriptions.get(key) ?? '';
            out.push(
                `| \`${e.method}\` | \`${e.path}\` | ${e.auth} | \`${e.rateLimit}\` | ${description} |`,
            );
        }
        out.push('');
    }
    return out.join('\n');
}

const endpoints = await parseControllers();
const { rows, descriptions } = await readDoc();

if (WRITE) {
    await writeFile(DOC_PATH, render(endpoints, descriptions), 'utf8');
    console.log(`[endpoints] Wrote ${endpoints.length} endpoints to docs/backend/ENDPOINTS.md.`);
    process.exit(0);
}

const problems = [];
const seen = new Set();

for (const e of endpoints) {
    const key = `${e.method} ${e.path}`;
    seen.add(key);
    const documented = rows.get(key);
    if (!documented) {
        problems.push(`missing from the doc:      ${key}  (${e.controller}.${e.action})`);
        continue;
    }
    if (documented.auth !== e.auth) {
        problems.push(`auth differs:              ${key}  doc="${documented.auth}" code="${e.auth}"`);
    }
    if (documented.rateLimit !== e.rateLimit) {
        problems.push(
            `rate limit differs:        ${key}  doc="${documented.rateLimit}" code="${e.rateLimit}"`,
        );
    }
}

for (const key of rows.keys()) {
    if (!seen.has(key)) problems.push(`documented but not in code: ${key}`);
}

if (problems.length) {
    console.error('[endpoints] docs/backend/ENDPOINTS.md disagrees with the controllers:\n');
    for (const p of problems) console.error('  ' + p);
    console.error('\nRun `npm run docs:endpoints` to regenerate it.');
    process.exit(1);
}

console.log(`[endpoints] ${endpoints.length} endpoints — doc matches the controllers.`);
