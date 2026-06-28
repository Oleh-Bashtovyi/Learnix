# Architectural Decision Records (ADR)

This directory contains records of significant architectural and technical decisions made for the backend of the Learnix platform.

We use these documents to keep track of *why* certain choices were made, the context around them, and the alternatives considered. 

## Decisions Index

### Core Architecture & Domain
- [Architecture & CQRS](ARCHITECTURE.md)
- [Domain Model Design](DOMAIN.md)
- [Infrastructure & Data Access](INFRA.md)
- [Migrations Strategy](MIGRATIONS.md)

### Features & Integrations
- [Achievements System](ACHIEVEMENTS.md)
- [Authentication & Security](AUTH.md)
- [Blob Storage & Uploads](BLOB.md)
- [Certificates](CERTIFICATES.md)
- [AI Chat Integration](CHAT.md)
- [Email System](EMAILS.md)
- [LMS & Course Structure](LMS.md)
- [Direct Messaging](MESSAGING.md)
- [Payments Integration](PAYMENT.md)
- [Course Reviews](REVIEWS.md)

### Deployment & Operations
- [CI/CD Pipelines](CICD.md)
- [Forwarded Headers (Proxies)](FORWARDED_HEADERS.md)

## Proposing a New Decision

When introducing a major change in tools, libraries, or architectural patterns:

1. Copy the `TEMPLATE.md` file in this directory.
2. Name it descriptively (e.g., `CACHING.md`).
3. Fill out the sections following the template.
4. Submit a Pull Request to review the decision.
