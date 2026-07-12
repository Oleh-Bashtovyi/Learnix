/**
 * Every ADR id that is mentioned must exist as a heading somewhere.
 *
 * ADRs are referenced from prose, from other ADRs, and from C# comments. Nothing enforced that the
 * id being referenced still existed — so `ADR-BACK-USERS-001` was cited in TECH_DEBT.md while the
 * heading said `ADR-USERS-001`, and `ADR-INFRA-020` was cited while INFRA stopped at 014.
 *
 *   node scripts/check-adr-ids.mjs     exits 1 on any reference with no heading behind it
 */
import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const SEARCH = ['docs', '.claude', 'Learnix.Backend', 'CLAUDE.md', 'README.md'];
const SKIP_DIRS = new Set(['node_modules', 'bin', 'obj', '.git', 'dist']);
const EXTENSIONS = new Set(['.md', '.cs']);

/** `ADR-BACK-INFRA-006`, `ADR-FRONT-UI-002`, `ADR-REPO-001`. */
const ID_RE = /ADR-[A-Z]+(?:-[A-Z]+)*-\d{3}/g;
const HEADING_RE = /^#{1,4}\s+(ADR-[A-Z]+(?:-[A-Z]+)*-\d{3})/gm;

/**
 * ADRs that were removed because the mechanism they described no longer exists. Their numbers are
 * never reused, and the registers name them so the gaps are explained rather than mysterious — so
 * these ids are legitimately referenced with no heading behind them.
 */
const REMOVED = new Set([
    'ADR-BACK-INFRA-006', // auto-migrations on API startup → ADR-BACK-MIGR-001
    'ADR-BACK-INFRA-009', // seed assets in Infrastructure → ADR-BACK-MIGR-002
    'ADR-BACK-LMS-001', // duplicate of ADR-BACK-DOMAIN-004 (Course as aggregate root)
    'ADR-BACK-LMS-003', // duplicate of ADR-BACK-DOMAIN-008 (test value objects in JSONB)
]);

async function collectFiles(entry, out = []) {
    const full = path.join(ROOT, entry);
    let stat;
    try {
        stat = await readdir(full, { withFileTypes: true });
    } catch {
        if (EXTENSIONS.has(path.extname(full))) out.push(full);
        return out;
    }
    for (const e of stat) {
        if (e.isDirectory()) {
            if (SKIP_DIRS.has(e.name)) continue;
            await collectFiles(path.join(entry, e.name), out);
        } else if (EXTENSIONS.has(path.extname(e.name))) {
            out.push(path.join(full, e.name));
        }
    }
    return out;
}

const files = (await Promise.all(SEARCH.map((s) => collectFiles(s)))).flat();

const defined = new Set();
const referenced = new Map(); // id -> [where]

for (const f of files) {
    const text = await readFile(f, 'utf8');
    for (const m of text.matchAll(HEADING_RE)) defined.add(m[1]);
    for (const m of text.matchAll(ID_RE)) {
        const where = path.relative(ROOT, f);
        if (!referenced.has(m[0])) referenced.set(m[0], new Set());
        referenced.get(m[0]).add(where);
    }
}

const dangling = [...referenced].filter(([id]) => !defined.has(id) && !REMOVED.has(id));

console.log(`[adr] ${defined.size} ADRs defined, ${referenced.size} distinct ids referenced.`);

if (dangling.length) {
    console.error('\n[adr] referenced but no such ADR heading exists:\n');
    for (const [id, where] of dangling) {
        console.error(`  ${id}  ←  ${[...where].join(', ')}`);
    }
    process.exit(1);
}

console.log('[adr] every referenced ADR id resolves to a heading.');
