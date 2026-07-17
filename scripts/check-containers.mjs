/**
 * Terraform creates the blob containers in production; C# reads and writes them. Nothing connected the
 * two, and they cannot import each other — so a rename on one side, or a container provisioned with the
 * wrong access level, only shows up as 404s and dead SAS tokens after a deploy. It has happened once
 * already: `course-videos` was provisioned with anonymous read while `GetLessonContent` was handing out
 * 2-hour SAS tokens for it, making the expiry decoration (see the comment in infrastructure/storage.tf).
 *
 * This holds both sides to the same answer — which containers exist, and which of them answer anonymous
 * reads.
 *
 *   node scripts/check-containers.mjs     exits 1 on any disagreement
 */
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const CS = path.join(ROOT, 'Learnix.Backend/Learnix.Infrastructure/Storage/BlobContainers.cs');
const TF = path.join(ROOT, 'infrastructure/storage.tf');

/** `public const string Avatars = "avatars";` */
const CONST_RE = /public\s+const\s+string\s+(\w+)\s*=\s*"([^"]+)"\s*;/g;
/** `[Avatars] = BlobContainerAccess.Public,` */
const ACCESS_RE = /\[(\w+)\]\s*=\s*BlobContainerAccess\.(Public|Private)\s*,/g;
/** A whole `resource "azurerm_storage_container" "x" { ... }` block. */
const TF_BLOCK_RE = /resource\s+"azurerm_storage_container"\s+"(\w+)"\s*\{([^}]*)\}/g;
const TF_NAME_RE = /name\s*=\s*"([^"]+)"/;
const TF_ACCESS_RE = /container_access_type\s*=\s*"([^"]+)"/;

/** Terraform's vocabulary for "anonymous read is allowed". */
const TF_ACCESS_TO_MODEL = { blob: 'Public', container: 'Public', private: 'Private' };

const fail = (msg) => {
    console.error(`[containers] ${msg}`);
    process.exitCode = 1;
};

const csSource = await readFile(CS, 'utf8');
const tfSource = await readFile(TF, 'utf8');

// C# side
const constByMember = new Map();
for (const [, member, value] of csSource.matchAll(CONST_RE)) constByMember.set(member, value);

const csAccess = new Map();
for (const [, member, access] of csSource.matchAll(ACCESS_RE)) {
    const value = constByMember.get(member);
    if (!value) {
        fail(`BlobContainers.Access names '${member}', which is not a const in the same file.`);
        continue;
    }
    csAccess.set(value, access);
}

if (csAccess.size === 0) {
    fail(`No containers parsed from ${path.relative(ROOT, CS)} — the file's shape changed and this check is now blind.`);
    process.exit(1);
}

const declaredConsts = [...constByMember.values()];
for (const value of declaredConsts) {
    if (!csAccess.has(value)) {
        fail(`Container '${value}' is a const in BlobContainers but missing from its Access map, so its access level is undeclared.`);
    }
}

// Terraform side
const tfAccess = new Map();
for (const [, resource, body] of tfSource.matchAll(TF_BLOCK_RE)) {
    const name = body.match(TF_NAME_RE)?.[1];
    const rawAccess = body.match(TF_ACCESS_RE)?.[1];

    if (!name) {
        fail(`Terraform resource '${resource}' has no name attribute.`);
        continue;
    }
    if (!rawAccess) {
        fail(`Terraform container '${name}' does not set container_access_type.`);
        continue;
    }

    const mapped = TF_ACCESS_TO_MODEL[rawAccess];
    if (!mapped) {
        fail(`Terraform container '${name}' has unrecognised container_access_type '${rawAccess}'.`);
        continue;
    }
    tfAccess.set(name, mapped);
}

if (tfAccess.size === 0) {
    fail(`No containers parsed from ${path.relative(ROOT, TF)} — the file's shape changed and this check is now blind.`);
    process.exit(1);
}

// Compare
for (const [name, access] of csAccess) {
    if (!tfAccess.has(name)) {
        fail(`'${name}' is used by the code but Terraform never creates it — writes to it will fail in production.`);
        continue;
    }
    const tf = tfAccess.get(name);
    if (tf !== access) {
        const detail =
            access === 'Private'
                ? `the code hands out a time-limited SAS for it, which is worthless while the container answers anonymous reads`
                : `the code builds a plain URL for it via GetPublicUrl, which will 404 for every visitor`;
        fail(`'${name}': code says ${access}, Terraform says ${tf} — ${detail}.`);
    }
}

for (const name of tfAccess.keys()) {
    if (!csAccess.has(name)) {
        fail(`Terraform creates '${name}', but no code references it. Remove it, or add it to BlobContainers.`);
    }
}

if (process.exitCode === 1) {
    console.error(
        `\n[containers] ${path.relative(ROOT, CS)} and ${path.relative(ROOT, TF)} disagree.` +
            `\n[containers] Renaming a container is never just an edit: entities persist it inside their blob path` +
            `\n[containers] (ADR-BACK-BLOB-002), so it needs a data migration and a physical move of the blobs.`,
    );
} else {
    console.log(`[containers] ${csAccess.size} containers — code and Terraform agree on names and access.`);
}
