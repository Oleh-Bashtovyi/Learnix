# Learnix — ADR: Email Notifications

> Covers the design and implementation of the email delivery and localization subsystem.

## Endpoints summary

*Note: The Email subsystem operates primarily as a background infrastructure service driven by Domain Events and Outbox processors. It does not expose direct public API endpoints.*

| Trigger | Description | Subsystem |
|---|---|---|
| User Registration | Sends "Confirm your email" message | Auth |
| Password Reset Request | Sends "Reset your password" message | Auth |

---

## ADR-EMAIL-001: Email delivery — MailKit (SMTP) + RazorLight (.cshtml templates) + PreMailer.Net

**Decision:** Email sending is implemented using `MailKit` (SMTP client) and `RazorLight` for rendering `.cshtml` templates. For CSS inlining, `PreMailer.Net` is used, which converts CSS classes from `styles.css` (included in `_Layout.cshtml`) into inline styles (`style="..."`). Locally, Mailpit is used via Docker (SMTP :1025, Web UI :8025). On Azure, SendGrid SMTP relay is used. A console-logging `ConsoleEmailSender` is also available for development.

**Why:**
- MailKit is the recommended .NET SMTP client, supporting TLS/StartTLS and async operations.
- RazorLight is a standalone Razor engine that works outside the ASP.NET MVC pipeline, allowing `.cshtml` rendering in the Infrastructure layer.
- PreMailer.Net solves the problem of CSS support in email clients by taking HTML and applying the linked CSS as inline styles directly on the elements. This enables clean templates with a shared `_Layout.cshtml`.
- Abstraction via `SmtpEmailSender` and `IEmailSender` allows easy swapping of the transport layer in the future without vendor lock-in.
- Mailpit is a lightweight Docker container for local testing (captures emails, displays HTML in browser).
- SendGrid SMTP relay fits the free tier on Azure and is a standard way to send emails without managing SMTP servers.

**Alternatives considered:**
- SendGrid SDK (`SendGrid` NuGet) — rejected due to vendor lock-in on the `IEmailSender` interface; requires port 587 anyway.
- Azure Communication Services Email — Azure-native, but more complex to set up locally than SMTP.
- `System.Net.Mail.SmtpClient` — legacy, does not support modern async properly, deprecated by Microsoft.

**Consequences:**
- Templates are placed in `Learnix.Infrastructure/Email/Templates/*.cshtml` and `.css`, copied to the output directory (`Content`, `CopyToOutputDirectory=PreserveNewest`).
- HTML templates use standard layout techniques, but are processed for maximum compatibility.
- `SmtpSettings` configured in `Learnix.Infrastructure/Settings/`.
- Future integration with MassTransit (Phase 6) will make `SmtpEmailSender` a consumer, decoupling the API process from SMTP latency.

---

## ADR-EMAIL-002: Email localization — IStringLocalizer + .resx + Language on User

**Decision:** Email templates are localized into English (default) and Ukrainian using `IStringLocalizer<EmailStrings>` and `.resx` resource files. The language preference is stored in the `Language` field of the `User` entity (default `"en"`), which is initially populated from the `Accept-Language` header during registration. `SmtpEmailSender` sets `CultureInfo.CurrentUICulture` before rendering; `IStringLocalizer` automatically picks up the correct translations.

**Why:**
- `IStringLocalizer` + `.resx` is the standard .NET localization mechanism; well supported by IDEs.
- Storing `Language` on the `User` entity is necessary because emails are generated asynchronously in background workers (Outbox) where the HTTP request context (`Accept-Language`) no longer exists.
- `Accept-Language` is used for smooth UX during onboarding (the first email will be in the user's preferred language).
- Marker class `EmailStrings` in root namespace `Learnix.Infrastructure` with `ResourcesPath = "Email/Resources"` maps to `.resx` files in the `Email/Resources/` folder.

**Alternatives considered:**
- Passing `Language` through domain events directly without storing in DB — rejected because changing the language later (e.g., via UI profile settings) would not affect subsequent system emails.
- UI profile setting for language — postponed; currently populated only via `Accept-Language`.
- Inline conditional blocks inside `.resx` — rejected as it's hard to maintain and violates localization principles.

**Consequences:**
- `User.Language` (varchar 5, default `en`) is added via `AddUserLanguage` migration.
- Outbox payloads now include `Language`; outbox handlers must retrieve it.
- Application-layer event handlers (`UserRegisteredDomainEventHandler`, `PasswordResetRequestedDomainEventHandler`) must query the user's `Language`.
- `SmtpEmailSender` is a singleton; `IStringLocalizerFactory` is injected, and the localizer is created in the constructor.
- `.resx` files (`Email/Resources/EmailStrings.resx` (EN) and `EmailStrings.uk.resx` (UK)) are embedded resources.
