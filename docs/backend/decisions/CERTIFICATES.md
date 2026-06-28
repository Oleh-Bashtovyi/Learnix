# Learnix — ADR: Certificates

> This file contains decisions regarding the certificate generation and validation subsystem (Phase 8).

## Endpoints summary

| HTTP Method | Endpoint | Description | Rate Limit | Auth Required |
|---|---|---|---|---|
| `GET` | `/api/certificates/mine` | Fetch user's earned certificates | Default | Yes |
| `GET` | `/api/certificates/courses/{courseId}` | Get certificate details for a specific course | Default | Yes |
| `POST` | `/api/certificates/courses/{courseId}/generate` | On-demand generate PDF certificate | Default | Yes |
| `GET` | `/api/certificates/verify/{code}` | Public verification of a certificate by code | Default | No |

---

## ADR-CERT-001: QuestPDF library for PDF generation

**Decision:** The **QuestPDF** library is used for creating layouts and rendering PDF certificates. The layout is described entirely via the C# Fluent API (code-first approach), without the use of intermediate HTML templates.

**Why:**
- **Performance and memory consumption:** QuestPDF is extremely fast (generating a simple certificate takes < 50 ms) and has very low RAM consumption, as it is a native .NET solution that uses SkiaSharp under the hood.
- **No external dependencies:** Unlike solutions based on wkhtmltopdf or Puppeteer, QuestPDF does not require the installation of browsers (Chrome/Edge) or heavy binaries on the host machine/in the Docker container.
- **Fluent API:** The layout is strictly typed. The compiler checks the layout code, which makes maintenance and refactoring much safer compared to HTML/CSS strings.
- **Layout accuracy:** Allows working with absolute coordinates, precise font sizes, and millimeters, which is critical for certificates that might be printed on A4 paper.

**Alternatives considered:**
- **HTML-to-PDF (DinkToPdf / wkhtmltopdf):** Outdated technology, often breaks layout, requires native C++ libraries on the system.
- **Puppeteer-Sharp (headless Chrome):** Renders HTML perfectly, but brings in hundreds of megabytes of Chromium. For an on-demand API, this is too slow and "heavy" (browser startup time).
- **iText7 / PDFsharp:** Powerful, but have more complex (sometimes outdated) APIs and strict licensing restrictions (AGPL for iText). QuestPDF (under the Community License) is perfectly suited for this project.

**Consequences:**
- The certificate layout is maintained as C# code (`CertificatePdfDocument`).
- The API server can generate PDFs synchronously without fear of thread pool exhaustion or memory leaks.

---

## ADR-CERT-002: QR code generation via QRCoder

**Decision:** The **QRCoder** library is used to generate the QR code on the certificate (which links to the public certificate validation page).

**Why:**
- **Integration with QuestPDF:** QuestPDF does not have a built-in QR code generator. It accepts ready-made images (byte arrays). QRCoder is a reliable, lightweight C# library that easily generates a QR code matrix and converts it to a PNG/Byte Array.
- **Offline generation:** Allows creating QR codes completely in-memory without calling external APIs, which guarantees stability, speed, and privacy.
- **No dependency on System.Drawing:** QRCoder supports generating basic graphic formats without using `System.Drawing.Common`, which is problematic on Linux/Docker (starting from .NET 6).

**Alternatives considered:**
- **External services (e.g., Google Chart API / goqr.me):** Require an internet connection, create network latency, might go down or change limits. Rejected for synchronous certificate generation.
- **ZXing.Net:** A very powerful library for reading/writing barcodes. Slightly overloaded (overkill) in terms of functionality if only a simple QR code is needed. QRCoder is much lighter.

**Consequences:**
- `QRCoder` dependency was added to `Learnix.Infrastructure`.
- A `GenerateQrCode()` method is implemented in `CertificatePdfDocument`, which is used to insert graphics into the QuestPDF layout.

---

## ADR-CERT-003: On-Demand (synchronous) certificate generation

> **Supersedes**: Previous decision "Asynchronous PDF certificate generation via BackgroundService".

**Decision:** The PDF certificate is generated synchronously directly during the user's HTTP request (`POST /api/certificates/courses/{courseId}/generate`). A background worker for generation is not used. The background service `CertificatePdfGenerationService` has been completely removed.

**Why:**
- **QuestPDF Speed:** Since generation in RAM takes milliseconds (under 50 ms), there is no sense in offloading this to a background queue. The synchronous call does not create significant load on the HTTP thread.
- **UX:** The asynchronous background service (which polled the database every 30 seconds) created bad UX: users clicked "Get certificate", saw a "Generating..." loader, and had to wait without controlling the process. Now they get the file instantly.
- **Simplified architecture:** Eliminates the need to manage certificate states (`Pending`, `Generated`, `Failed`) and store temporary files on disk before uploading to Azure Blob Storage. Also, in case of a generation failure or manual DB cleanup, the user can easily regenerate the certificate.

**Alternatives considered:**
- **Background Worker (old solution):** Rejected due to poor UX and the complexity of manual regeneration.
- **MassTransit Consumer:** Considered initially, but rejected as overkill after testing showed that QuestPDF handles the task "on the fly" perfectly, resolving all issues instantly and being architecturally simpler to implement.

**Consequences:**
- Added a single generation/regeneration endpoint `POST /api/certificates/courses/{courseId}/generate`.
- `CertificatePdfGenerationService` was completely removed from the codebase and `DependencyInjection.cs`.
- Frontend (the "Download Certificate" buttons) calls the mutation, generates the PDF, and immediately opens the generated link (`window.location.href`). There is no longer a waiting status `isReady: false`.


