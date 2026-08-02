# Learnix — Frontend Architecture Decision Records (File Uploads)

> Format: Decision → Why → Alternatives.
> The backend side (SAS issuance, synchronous commit/promote, blob path format) is
> `ADR-BACK-BLOB-003` in `docs/backend/decisions/platform/BLOB.md`.

---

## ADR-FRONT-UPLOAD-001: Direct-to-Blob Upload via a Shared Two-Call Hook

**Context:** The backend never accepts a file body directly (`ADR-BACK-BLOB-003`) — every upload is a
SAS URL handed to the client, followed by the client PUTting the file straight to Azure Blob Storage.
Every upload surface (avatar, course cover, category image, lesson video) needs that same two-call
sequence, not a reimplementation each.

**Decision:**
`useRequestUploadUrl` (`src/hooks/shared/useRequestUploadUrl.ts`) is the one hook that performs it:
`POST /uploads/request-url` for a SAS URL, then `PUT` the file straight to that URL via a bare Axios
call (no `Authorization` header — the SAS URL itself is the credential, issued through `uploads.api.ts`).
It resolves to the resulting `blobPath`, which the caller then submits as an ordinary field on whatever
form or mutation persists the owning entity (profile, course, lesson). That save is what triggers the
backend's synchronous commit/promote step (`ADR-BACK-BLOB-003`) — there is no third frontend call.

**Why:**
- One hook means the SAS-request-then-PUT sequence is identical everywhere a file is uploaded, instead
  of every upload surface hand-rolling its own pair of Axios calls.
- Never routing file bytes through our own API keeps the backend from having to buffer an upload in
  memory, which matters most for lesson video.

**Alternatives:**
- Server-side `IFormFile` upload — rejected for the same reason the backend ADR rejects it: memory and
  bandwidth pressure on the API for large files.

**Consequences:**
- A new upload surface calls `useRequestUploadUrl` (or one of the target-specific wrappers in
  ADR-FRONT-UPLOAD-002) instead of posting a file to a bespoke endpoint.

---

## ADR-FRONT-UPLOAD-002: Client-Side Validation and Crop Before the Network Call

**Context:** Content-type, file size, and — for images — minimum source dimensions are all things the
client already knows before spending a round trip on a SAS URL that would otherwise be requested for a
file that was always going to be rejected (or, worse, silently accepted and stretched too thin).

**Decision:**
`useImageCropUpload` (`src/hooks/shared/useImageCropUpload.ts`) validates content-type, byte size and
pixel dimensions — against per-target rules in `const/upload.constants.ts` — before requesting a SAS
URL at all, then opens a crop dialog and only calls `useRequestUploadUrl` once the crop is confirmed; a
user who backs out of cropping leaves no temp blob behind. `VideoUploader` runs the analogous
content-type/size check for lesson video and reads its duration client-side (via a hidden `<video>`
element) before uploading.

**Why:**
- Validating upfront means a rejected file never reaches the network step, which matters most for
  video, where the wasted upload itself is the expensive part.
- Cropping before upload means the stored blob is always exactly what the user confirmed, and a
  cancelled crop costs nothing server-side.

**Alternatives:**
- Validate only after the blob lands (server-side) — rejected: wastes a full upload on a file that was
  always going to be rejected.

**Consequences:**
- A new image upload target is added as a new entry in `IMAGE_CROP_RULES` (`upload.constants.ts`), not
  as new validation logic in the hook itself.
