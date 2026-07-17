# Learnix — ADR: Blob Storage

> Format: decision → why → rejected alternatives.
> Covers the backend blob storage architecture and asset management.

> **Endpoints:** see [`docs/backend/ENDPOINTS.md`](../../ENDPOINTS.md) — one generated table for
> the whole API, verified against the controllers in CI. An ADR records a decision; it is not the
> place to keep a copy of the API surface.

---
## ADR-BACK-BLOB-001: Azure Blob Storage Integration & SDK

**Decision:** The platform uses Azure Blob Storage for all file assets (avatars, course covers, videos, category images, and certificates). The integration is implemented in the `Learnix.Infrastructure` layer using the official `Azure.Storage.Blobs` SDK. 

**Why:**
- Provides a robust, highly scalable storage backend natively supported by Azure.
- The `Azure.Storage.Blobs` SDK allows generating Shared Access Signatures (SAS) easily, enabling secure, time-limited direct client uploads and downloads.

---

## ADR-BACK-BLOB-002: Relative Paths in the Database

**Decision:** The database does NOT store absolute URLs for blob assets. Instead, it stores a relative path in the format `{containerName}/{blobName}` (e.g., `avatars/9f2c4a1b8e7d40f3a5c6b2d1e0f34567`).

The **container prefix is mandatory**: `AzureBlobStorageService.ParseBlobPath()` splits on the first `/` and throws `ArgumentException` without it, so `DeleteAsync`, `GenerateReadUrl` and `GetPublicUrl` all depend on it. This is what lets a domain event carry nothing but the path — the container is derivable from the value itself.

The `{blobName}` segment is opaque to the application and its shape depends on which code produced it. The path is flat in every case — there is no per-user or per-entity nesting:

| Producer | `{blobName}` | Example |
|---|---|---|
| `CommitUploadAsync` (all user uploads) | bare GUID, `N` format, no extension | `avatars/9f2c4a1b8e7d40f3a5c6b2d1e0f34567` |
| `CourseSeeder` (demo data) | `{Guid}-cover.webp` | `course-covers/3f1a…-cover.webp` |
| `GenerateCertificate` (server-side) | `{certificateCode}.pdf` | `certificates/ABC123.pdf` |

**Why not an absolute URL:**
- Avoids vendor lock-in and prevents database updates if the storage account name, domain, or environment (Dev vs Prod) changes.
- The application layer can dynamically construct the necessary URL (public or private) based on the context.

**Why the container is part of the stored value, rather than a parameter passed at call time:**

The obvious alternative is to store the bare `{blobName}` and let every caller supply the container, e.g. `DeleteAsync(user.AvatarBlobPath, UploadTarget.Avatar)`. Every read site knows its target statically, so this would compile. It was rejected for two reasons.

1. **It would push blob-storage knowledge into the Domain.**
   A stored path is self-describing, so a domain event only ever needs to carry a string:
   ```csharp
   RaiseDomainEvent(new UserAvatarRemovedDomainEvent(Id, AvatarBlobPath));
   ```
   The Outbox message it produces is equally opaque — `DeleteBlobPayload(string BlobPath)` — and one `DeleteBlob` message type serves avatars, course covers, category images and lesson videos alike. The background worker resolves the container by parsing the path; it has no entity, no type, no context, and needs none.

   Drop the container from the path, and that knowledge must reappear somewhere. Either the domain event carries an `ImageType` / `UploadTarget` enum — which teaches `Learnix.Domain` that blob storage is partitioned into containers, a pure infrastructure concern — or every `*Removed` event needs its own Outbox payload and handler to re-attach the container. Today `Learnix.Domain` contains **zero** references to any container name. That is the property being protected.

2. **A stored path is an address, not a copy of a constant.**
   `BlobContainers.Avatars` answers "where do *new* avatars go?". `User.AvatarBlobPath` answers "where does *this* avatar actually live?". They coincide right up until someone changes the name — at which point the stored addresses remain correct and the derived ones silently become wrong. That asymmetry is why the name is not a setting at all (ADR-BACK-BLOB-004).

**Rejected alternative:** bare `{blobName}` + container supplied per call site. See above.

> [!WARNING]
> **Container names are immutable once deployed.**
> They are read only when *writing* a new blob. Existing rows keep the container they were stored with, which is correct — the files are physically there. Rename one and the application starts up cleanly, new uploads land in the new container, and every previously stored asset keeps resolving to the old one. Renaming therefore requires physically moving the blobs **and** a data migration rewriting the prefix in every blob-path column (`Users.AvatarBlobPath`, `Courses.CoverBlobPath`, `Categories.ImageBlobPath`, `VideoLessons.VideoBlobPath`, `Certificates.FilePath`).
>
> This is why they are `BlobContainers` constants and not settings — see ADR-BACK-BLOB-004.

**Public containers vs private ones — and why the difference is load-bearing:**

| Container | Access | How a URL is produced |
|---|---|---|
| `avatars`, `course-covers`, `category-images` | public (anonymous read) | `GetPublicUrl(path)` — a plain URL, no token. These are decorations on a public catalog; hiding them behind SAS would buy nothing and cost a signature per thumbnail. |
| `course-videos` | **private** | `GenerateReadUrl(path, 2 h)` — a SAS, issued by `GetLessonContent` **after** it verifies the enrollment. |
| `certificates` | **private** | `GenerateReadUrl(path, 24 h)` — a SAS. |
| `temp-uploads` | private | write-only SAS (`Create`, 15 min) from `GenerateUploadUrlAsync`. |

The SAS on a lesson video is the *only* thing standing between a paid course and the open internet, and it
is worth exactly as much as the container's access level lets it be worth. If `course-videos` allows
anonymous reads, the plain URL works — forever, for anyone the link is ever forwarded to — and the
two-hour expiry is decoration. Terraform declared that container `blob` (public) while `StorageSeeder`
created it private locally; the code was written against the private model, so production was the one that
was wrong. Fixed in `infrastructure/storage.tf`.

**Implementation Details:**
- Public URL: `!string.IsNullOrWhiteSpace(c.CoverBlobPath) ? blobStorage.GetPublicUrl(c.CoverBlobPath) : null`
- Private read URL: `blobStorage.GenerateReadUrl(blobPath, ttl)`, with the TTLs in `BlobUrlTtlConstants`.

---

## ADR-BACK-BLOB-003: Two-Phase Upload Pattern (Temp → Final)

**Decision:** The entire lifecycle of file uploads is divided into three clear phases using the **"Temp-to-Final"** pattern (Pattern 1) to ensure reliability and strictly prevent orphan files:

1. **Direct Upload to Temporary Container:** 
   - The client asks the backend for a secure, temporary link (`POST /api/uploads/request-url`).
   - The backend responds with a SAS URL pointing strictly to a `temp-uploads` container.
   - The client uploads the file directly to Azure using this link, taking the heavy lifting (bandwidth/memory) off our API servers.
2. **Synchronous Validation & Commit:** 
   - When the client submits a form (e.g., updating a profile or lesson), they send the temporary file's path.
   - The backend calls `IBlobStorageService.CommitUploadAsync()`. This method synchronously:
     - Verifies the file size in the `temp-uploads` container.
     - Reads the "magic bytes" (first 512 bytes) to guarantee the MIME type wasn't spoofed.
     - Issues an internal Azure `StartCopyFromUriAsync` command to copy the file to the final permanent container (`avatars`, `course-videos`, etc.), **under the same blob name it had in `temp-uploads`**.
     - Leaves the temp blob alone — the lifecycle policy reaps it (see "Why the commit is idempotent" below).
     - Returns the permanent `BlobPath` to the application layer.
3. **Database Persistence:** 
   - The application layer saves the new permanent path to the database within the same request.
4. **Automated Cleanup (Azure Lifecycle Management):**
   - A native Azure Lifecycle Policy is configured to automatically delete any blob in the `temp-uploads` container older than 24 hours.

**Why Pre-signed Upload:**
- Files do not pass through the API server — this removes memory and bandwidth pressure, especially for large videos (up to 2 GB).
- The API doesn't need file streaming middleware or multipart parsing.

**Why Magic Byte Validation (not Content-Type header):**
- The `Content-Type` header on a SAS PUT can be spoofed by the client.
- Magic bytes cannot be spoofed without actually rewriting the file.
- It is implemented and verified for `jpeg` (`FF D8 FF`), `png` (`89 50 4E 47`), `webp` (`52 49 46 46...57 45 42 50`), `mp4` (`ftyp` box), `webm` (`1A 45 DF A3`), and `pdf` (`%PDF`).

**Why Temp → Final Copy instead of Outbox tags (Pattern 2):**
*The system was originally built using an Outbox pattern where files were uploaded directly to their final containers and later tagged `confirmed=true` via an asynchronous background worker. This was abandoned due to several critical limitations discovered during an architectural audit:*
- **Tags cannot express "unconfirmed".** A pending file is one *without* a `confirmed` tag, and Azure has no way to ask for that. Lifecycle filters support only equality, and [the policy-structure docs](https://learn.microsoft.com/en-us/azure/storage/blobs/lifecycle-management-policy-structure) say it outright: *"a filter provides a means to specify which blobs to **include**, but a filter provides no means to specify which blobs to exclude."* The same hole exists in the query API — [`Find Blobs by Tags`](https://learn.microsoft.com/en-us/rest/api/storageservices/find-blobs-by-tags) supports exactly `=`, `>`, `>=`, `<`, `<=`, `AND` and `@container`. There is no `NOT`, no `!=`, no `OR`. So neither the cleanup policy nor a custom sweeper could ever find the files that need cleaning.
- **SAS PUT Blob Tag Destruction:** An alternative proposed was to create an empty blob with a `confirmed=false` tag, generate a SAS, and let the client upload over it. However, the Azure Storage REST API dictates that the `PUT Blob` operation completely overwrites the target and **destroys all existing tags and metadata** unless explicitly provided in the request headers. Since malicious clients can omit these headers, the `confirmed=false` tag would be wiped out, leaving untagged, orphaned files forever.
- **The Temp Container Solution:** By dedicating a `temp-uploads` container, we can use a pure time-based Lifecycle Policy ("Delete all blobs in this container older than 24 hours") without relying on tags at all. It is 100% secure against malicious actors abandoning uploads.
- **Performance Trade-off — what is actually guaranteed, which is less than it looks.** The bytes never
  touch the API: the client PUTs them straight to Azure, and the commit request carries only a path. The
  copy runs server-side, inside the datacenter, between two containers of the same account, and in
  practice a same-account block-blob copy comes back already `success`, so `WaitForCompletionAsync`
  returns without polling. **Microsoft does not promise this.** [The `Copy Blob` docs](https://learn.microsoft.com/en-us/rest/api/storageservices/copy-blob)
  say the operation *"copies blobs on a best-effort basis… so a copy is not guaranteed to start
  immediately or complete in a specified timeframe"*, that *"multiple pending Copy Blob operations
  within an account might be processed sequentially"*, and that a pending copy has a two-week ceiling.
  An earlier version of this ADR claimed "1–3 seconds for a 2 GB video" — that number was invented; no
  such figure exists in the documentation, and the documentation declines to give one.

  What follows is a real but small risk: if a copy ever does go pending, `WaitForCompletionAsync` polls
  inside the request and has no ceiling of its own, so the request occupies a slot until the caller's
  token is cancelled. It is not a two-week hang — the two weeks bound Azure's operation, not our
  request. Accepted as-is: a bounded wait (a linked `CancellationTokenSource` with a timeout, rolling
  back the destination blob on expiry) is the known fix if it is ever observed.

**Why the commit is idempotent — and why that is what closes the orphan problem:**

The destination blob keeps the name the upload already had in `temp-uploads`, so
`temp-uploads/abc123` always becomes `avatars/abc123`, never a fresh GUID. Two consequences follow, and
together they are worth more than any compensating cleanup:

- **Committing twice is harmless.** A double-submit copies over the same destination instead of minting
  a second blob and stranding the first. The earlier design generated a new `Guid` per commit, which is
  precisely what made a second call leave an orphan.
- **A failed save heals itself on retry.** If `SaveChangesAsync` fails after the copy, the blob in the
  final container is unreferenced — but the retry copies to *the same path* and saves the *same* value,
  so the would-be orphan simply becomes the live file. Nothing needs to be deleted.

This is why the temp blob is not deleted on commit. Deleting it saves under a day of storage on a file
the lifecycle policy is about to reap anyway, and costs the caller their only copy: a failed save would
mean pushing 2 GB again. Left in place, the caller retries the same path for free. Files rejected by
validation *are* deleted immediately — a retry of a file that failed its magic-byte check can never
succeed.

The residual orphan is now narrow: it survives only if the user never retries, and it costs a few cents.
See TECH_DEBT.md for the compensating-delete idea that was deliberately not built.

**Blob path naming convention:**
```text
(Temporary)  temp-uploads/{guid}
(Permanent)  avatars/{guid}
(Permanent)  course-covers/{guid}
(Permanent)  course-videos/{guid}
(Permanent)  category-images/{guid}
(Permanent)  certificates/{certificateCode}.pdf   ← written by the server, not by this flow
```

**Limits per `UploadTarget`** (`MaxSizes` / `AllowedContentTypes` in `AzureBlobStorageService`):

| Target | Max size | Allowed types | Who may request an upload URL |
|---|---|---|---|
| Avatar | 5 MB | jpeg, png, webp | any authenticated user |
| CourseCover | 10 MB | jpeg, png, webp | Instructor / Admin |
| LessonVideo | 2 GB | mp4, webm | Instructor / Admin |
| CategoryImage | 2 MB | jpeg, png, webp | Admin |
| Certificate | 5 MB | pdf | **nobody** |

`Certificate` is a target of the *commit* pipeline, not of the upload flow: the PDF is generated
server-side and pushed through the same size/magic-byte validation, which is why it has limits at all.
`RequestUploadUrlCommandHandler` rejects it outright with a `ForbiddenError` — *no role at all*, not even
Admin. A certificate is the platform's own signature, and one uploaded by hand would be indistinguishable
from one it issued.

---

## Detailed Comparison of Upload Patterns

To provide full context on why Pattern 1 was chosen, here is a breakdown of the three main architectural approaches considered for handling direct-to-cloud uploads.

### Approach 1: Temp-to-Final with Lifecycle Cleanup (Chosen)
*Upload to `temp-uploads` container. Synchronous `StartCopyFromUriAsync` to final container upon form submission. Azure Lifecycle Policy deletes everything in `temp-uploads` older than 24h.*

**Pros:**
- **Zero-Cost Cleanup:** Relies natively on Azure Storage Lifecycle policies, which execute at the infrastructure level with no compute cost to our API.
- **Fail-Safe Security against *abandoned uploads*:** an upload that is never submitted — a closed tab, a rejected validation, a malicious actor pushing terabytes of garbage — is cleaned up unconditionally, because it is still sitting in `temp-uploads` and age alone is enough to condemn it there. This is the frequent case, and the policy closes it completely. It is **not** protection against every orphan: see the Cons.
- **Architectural Simplicity:** Eliminates the need for background workers (HostedServices) or asynchronous Outbox processing for blob management.
- **Strong Consistency:** The application knows exactly when a file becomes "permanent," and validation/MIME checking happens synchronously before any database record is created.

**Cons:**
- **Latency Trade-off:** the "Save" request waits for the server-side copy. Usually imperceptible, but not guaranteed — see the Performance note in ADR-BACK-BLOB-003.
- **Double Storage (Temporarily):** For a short window (up to 24h), the file exists in both the temporary and permanent containers, slightly increasing storage usage.
- **An orphan in the *final* container if the commit succeeds, the save fails, and the user gives up.** `CommitUploadAsync` copies to `course-videos/`; only then does the handler call `SaveChangesAsync`. If that save fails — database down, request cancelled — the file sits in its final container with no row referencing it, and **nothing will ever remove it**: the lifecycle policy only reaps `temp-uploads`, and it must, because there age means "abandoned", while in a final container an old blob is usually a lesson someone is still watching. Two containers, two rules; one mechanism cannot serve both.

  This is the [dual-write problem](https://en.wikipedia.org/wiki/Two-phase_commit_protocol): Azure and PostgreSQL share no transaction, so the only choice is which way to fail. This design fails the right way — an invisible orphan costing pennies, rather than a row pointing at a file that is not there.

  The idempotent commit above shrinks this to almost nothing: a retry reuses the same destination path, so the would-be orphan becomes the live file. It only persists when the user abandons the retry. A compensating delete would close even that; see TECH_DEBT.md for why it was not built.

### Approach 2: Direct-to-Final with Tagging & Outbox (Rejected)
*Upload directly to the final container (`avatars/`). Use an Outbox message to asynchronously add a `confirmed=true` tag. Rely on Lifecycle Management to delete untagged blobs.*

**Pros:**
- **Zero Latency on Save:** The API request completes instantly since it only inserts a record into the Outbox and doesn't wait for Azure.
- **No Double Storage:** The file only ever exists in its final destination.

**Cons:**
- **Tag Destruction Vulnerability:** As discovered during implementation, the Azure `PUT Blob` operation overwrites all existing tags. A malicious client can bypass initial `confirmed=false` tags, making the file effectively invisible to tag-based lifecycle rules.
- **Lifecycle Limitations:** Azure Lifecycle rules cannot target blobs *missing* a tag; they can only target exact tag matches.
- **Outbox Complexity:** Requires a robust background processor, retry logic, and exponential backoff to handle transient Azure API failures when applying the confirmation tag.

### Approach 3: Direct-to-Final with HostedService Cleanup (Rejected)
*Upload directly to the final container. The backend runs an `IHostedService` (Background Worker) that periodically scans Azure Storage, compares all blobs against the PostgreSQL database, and deletes files that have no corresponding database record.*

> **What is rejected here is the *scan*, not the idea of a sweeper.** Every objection below follows from
> "list the whole container and diff it against the database". A sweeper driven by a `blob_gc` table
> (see the Cons of Approach 1) has none of them: it never lists Azure, it reads a short list of
> candidate paths out of its own database, and a grace period removes the race. That variant is not
> rejected — it is simply not needed at this size.

**Pros:**
- **No Azure Lifecycle Dependency:** Complete control over the cleanup logic in C# code.
- **Zero User Latency:** File remains exactly where it was uploaded; the user doesn't wait for copies or tag updates.

**Cons:**
- **Extreme API Cost & Throttling:** To find orphaned files, the backend must list *all* blobs in the container and cross-reference them with the database. At scale (millions of files), listing blobs becomes slow, expensive, and risks hitting Azure Storage rate limits.
- **Concurrency Risks:** If a user is currently uploading a large video while the HostedService runs, the service might see a blob in Azure that isn't in the database *yet*, and incorrectly delete it midway through the upload. Complex "grace period" logic must be implemented to prevent this.
- **Compute Overhead:** Scanning and comparing databases against remote storage accounts consumes significant CPU and memory on the API servers.

---

**Consequences of the Final Decision:**
- The Application Layer is now aware that file identities (paths) change during the "Commit" phase. It must update entities using the returned path from `CommitUploadAsync()`.
- The Outbox pattern is no longer used for blob confirmation, drastically reducing database load and infrastructure complexity.


---

## ADR-BACK-BLOB-004: Container names are constants, held to Terraform by a CI check

**Decision:** The container names and their access levels live in one place — `BlobContainers`
(`Learnix.Infrastructure/Storage/`) — as constants. They are **not** configuration; the
`BlobStorage` section is gone from `appsettings.json` and `BlobStorageOptions` is deleted.

Terraform still creates the containers, and it cannot read C#. `npm run check:containers`
(`scripts/check-containers.mjs`, wired into CI) parses both `BlobContainers.cs` and
`infrastructure/storage.tf` and fails when they disagree on **either** the set of names or an access
level.

**Why they are not configuration:**
- **It could never be configured.** Nothing overrode `BlobStorage:*` in any environment — not
  `appsettings.Development.json`, not `.env`, not the Container App's settings. Terraform gives each
  environment its own storage account, so the names never needed to vary.
- **Turning the knob corrupts data, silently.** The container is persisted inside every blob path
  (ADR-BACK-BLOB-002). Change the setting and the app starts up clean, new uploads go to the new
  container, and every stored row still resolves to the old one. This ADR already carried a WARNING
  saying the values must be treated as immutable once deployed — a setting whose documentation forbids
  setting it is not a setting, it is a footgun with a label. As a constant the same mistake requires a
  code edit, which a reviewer sees.
- **The value was written three times** — `appsettings.json`, the `BlobStorageOptions` property
  defaults, and `storage.tf` — and only the last one actually creates anything.

**Why the check, and why it covers access level too:**
- Moving to constants removes one of the three copies. The remaining duplication, C# against Terraform,
  is the only one that can break production, and it is unfixable by refactoring: the two languages
  cannot share a symbol. A CI check is the substitute for a compiler here, in the same vein as
  `check:endpoints` and `check:adr`.
- Access level is not decoration. `course-videos` was once provisioned with anonymous read while
  `GetLessonContent` was issuing 2-hour SAS tokens for it — the tokens were theatre, and the drift was
  invisible until someone read the Terraform. The check compares Terraform's `container_access_type`
  against `BlobContainers.Access` for exactly this.
- `StorageSeeder` (local Azurite) now derives both names and access from the same constants, so the
  local and provisioned accounts cannot disagree either.

**Alternatives:**
- **Keep the settings.** Rejected: see above — never varied, unsafe to vary.
- **Generate `storage.tf` from the constants, or the constants from `storage.tf`.** Single-sources it
  for real rather than checking after the fact. Rejected as disproportionate: six names that must never
  change do not justify a codegen step in the build, and a generator is itself a thing that breaks. The
  check is cheap and fails at the same moment a generator would have.
- **Terraform `for_each` over a shared JSON/tfvars file the app also reads.** Would single-source the
  names, but reintroduces exactly what this ADR removes: the app reading container names from a file at
  runtime, which is the shape that invites the edit.

**Consequences:**
- `AzureBlobStorageService` no longer takes `IOptions<BlobStorageOptions>`; nor do `StorageSeeder`,
  `CourseSeeder`, `StudentSeeder` or `CategorySeeder`, which each injected it only to read one name.
- `ConfigurationSectionNameConstants.BlobStorage` is gone.
- Adding a container means three edits that CI keeps in step: the constant, its `Access` entry, and the
  Terraform resource. Forgetting any of them fails `check:containers` rather than production.
- The check parses source with regexes, so it is coupled to the shape of both files. It fails loudly if
  it parses zero containers out of either, rather than passing vacuously.
