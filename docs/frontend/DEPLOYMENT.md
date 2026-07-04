# Learnix — Frontend Architecture Decision Records (Deployment)

> Format: Decision → Why → Alternatives.
> For step-by-step deployment instructions, refer to `docs/AZURE_DEPLOY.md` in the project root.

---

## ADR-FRONT-DEPLOY-001: Static File Hosting vs Node.js Server

**Decision:**
- The frontend is built as a pure static Single Page Application (SPA) using `vite build` and `tsc -b`.
- The production artifacts (`dist/` folder) are hosted using **Azure Static Web Apps** (or an Nginx container serving static files), rather than a Node.js web server (like Express or Next.js custom server).

**Why:**
- Learnix is a client-side rendered (CSR) React app. It does not use Server-Side Rendering (SSR).
- Serving pure static files is extremely cheap, fast, and easily cacheable by CDNs.
- Azure Static Web Apps automatically provides global CDN distribution, free SSL certificates, and simple GitHub Actions integration.

**Alternatives:**
- Node.js (Express) server to serve static files: Discarded due to unnecessary compute overhead and scaling complexity.

---

## ADR-FRONT-DEPLOY-002: Client-Side Routing and Fallbacks

**Decision:**
- Since we use React Router v7 for client-side routing, the web server (Nginx or Azure Static Web Apps) must redirect all requests for non-existent files to `index.html`.
- In Azure Static Web Apps, this is configured via the `staticwebapp.config.json` file.
- In Docker deployments, this is configured via the `nginx.conf` file using `try_files $uri $uri/ /index.html;`.

**Why:**
- Without this fallback, users navigating directly to `/student/courses` would receive a 404 error from the server instead of allowing React Router to render the page.
