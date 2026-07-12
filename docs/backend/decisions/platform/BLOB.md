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

2. **A stored path is an address, not a copy of configuration.**
   `BlobStorageOptions.AvatarContainer` answers "where do *new* avatars go?". `User.AvatarBlobPath` answers "where does *this* avatar actually live?". They coincide right up until someone changes the config — at which point the stored addresses remain correct and the derived ones silently become wrong.

**Rejected alternative:** bare `{blobName}` + container supplied per call site. See above.

> [!WARNING]
> **Container names in `appsettings.json` must be treated as immutable once deployed.**
> They are consumed by `BlobStorageOptions` only when *writing* a new blob. Existing rows keep the container they were stored with, which is correct — the files are physically there. But nothing in the code enforces or checks this: rename `BlobStorage:AvatarContainer` and the application starts up cleanly, new uploads land in the new container, and every previously stored asset keeps resolving to the old one. Renaming a container therefore requires physically moving the blobs **and** a data migration rewriting the prefix in every blob-path column (`Users.AvatarBlobPath`, `Courses.CoverBlobPath`, `Categories.ImageBlobPath`, `VideoLessons.VideoBlobPath`, `Certificates.FilePath`).

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
     - Issues an internal Azure `StartCopyFromUriAsync` command to copy the file to the final permanent container (`avatars`, `course-videos`, etc.).
     - Deletes the file from `temp-uploads`.
     - Returns the new permanent `BlobPath` to the application layer.
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
- **Azure Lifecycle Management Limitations:** Official Azure documentation confirms that Lifecycle Policies can only filter by exact tag matches (e.g., `status == temp`). It is impossible to configure a policy that deletes blobs based on the *absence* of a tag (e.g., "delete if `confirmed` tag is missing").
- **SAS PUT Blob Tag Destruction:** An alternative proposed was to create an empty blob with a `confirmed=false` tag, generate a SAS, and let the client upload over it. However, the Azure Storage REST API dictates that the `PUT Blob` operation completely overwrites the target and **destroys all existing tags and metadata** unless explicitly provided in the request headers. Since malicious clients can omit these headers, the `confirmed=false` tag would be wiped out, leaving untagged, orphaned files forever.
- **The Temp Container Solution:** By dedicating a `temp-uploads` container, we can use a pure time-based Lifecycle Policy ("Delete all blobs in this container older than 24 hours") without relying on tags at all. It is 100% secure against malicious actors abandoning uploads.
- **Performance Trade-off:** The synchronous `StartCopyFromUriAsync` takes only milliseconds for images and 1-3 seconds for a 2 GB video (since it occurs internally within the Azure datacenter). This minor delay during a "Save" operation is completely acceptable given the immense security and maintainability benefits.

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
- **Fail-Safe Security:** Guaranteed protection against orphaned blobs, even if malicious actors upload terabytes of garbage data and never submit the form.
- **Architectural Simplicity:** Eliminates the need for background workers (HostedServices) or asynchronous Outbox processing for blob management.
- **Strong Consistency:** The application knows exactly when a file becomes "permanent," and validation/MIME checking happens synchronously before any database record is created.

**Cons:**
- **Latency Trade-off:** The user's "Save" request is delayed by the time it takes Azure to perform the internal copy. (Usually milliseconds for images, up to a few seconds for multi-gigabyte videos).
- **Double Storage (Temporarily):** For a short window (up to 24h), the file exists in both the temporary and permanent containers, slightly increasing storage usage.

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

