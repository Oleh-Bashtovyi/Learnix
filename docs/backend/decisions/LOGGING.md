# Architecture Decision Record: Logging & Tracing

## Context

As the Learnix application grows, diagnosing issues using standard, unstructured console logs becomes increasingly difficult. Standard ASP.NET Core logging emits multiple text lines per HTTP request, making it hard to track the lifecycle of a specific request, especially in an Azure Container Apps (microservices-ready) environment.

To ensure production-readiness, we need a robust logging mechanism that supports structured logging (JSON), request correlation (tracing), and user identification.

## ADR-BACK-LOG-001: Structured Logging with Serilog

**Decision:** We will use **Serilog** as our primary logging provider, completely replacing the default ASP.NET Core logging provider.

**Why:**
- Serilog is the industry standard for structured logging in .NET.
- It allows logs to be written as structured JSON data (key-value pairs) rather than plain text strings.
- We utilize `app.UseSerilogRequestLogging()` to condense the noisy ASP.NET Core HTTP request logs into a single, clean log entry containing the method, path, status code, and duration.
- It seamlessly integrates with external log aggregators via "Sinks".

**Consequences:**
- All developers must use structured logging syntax, e.g., `_logger.LogInformation("Processing course {CourseId}", course.Id)` instead of string interpolation `$"Processing course {course.Id}"`.

---

## ADR-BACK-LOG-002: Traceability via LogEnrichmentMiddleware

**Decision:** We implemented `LogEnrichmentMiddleware` to intercept every incoming HTTP request and enrich the Serilog `LogContext` with a `CorrelationId` and `UserId`.

**Why:**
- **CorrelationId:** If a client passes an `X-Correlation-ID` header, we use it. If not, we generate a new `Guid`. This ID is injected into every single log emitted during that request (SQL queries, warnings, errors). If an error occurs, the frontend receives this ID in the response headers, allowing developers to search the exact error trace in the log aggregator.
- **UserId:** We extract the `sub` claim from the JWT token and append it to the log context. This allows us to filter logs by a specific user across the entire system.

**Consequences:**
- Serilog requires `.Enrich.FromLogContext()` in its configuration.
- The middleware must be registered very early in the pipeline (before `UseSerilogRequestLogging`), so that the final request log also contains the enriched data.

---

## ADR-BACK-LOG-003: Local Log Aggregation with Seq

**Decision:** We are introducing **datalust/seq** into our `docker-compose.yml` for local development log aggregation.

**Why:**
- Reading structured JSON logs in a raw console window is difficult.
- Seq provides a beautiful, web-based UI (at `http://localhost:5341`) tailored specifically for Serilog. It supports SQL-like querying over structured log properties (e.g., `UserId = '123' and @Level = 'Error'`).
- It teaches developers how to interact with real log aggregators (like Splunk, Application Insights, or Kibana) in a zero-configuration local environment.

**Consequences:**
- The `Serilog.Sinks.Seq` package is required in the API project.
- Local environment uses `appsettings.Development.json` to route logs to `http://localhost:5341`.
- In production (Azure), we can easily switch to Application Insights or Log Analytics simply by changing the configuration or deploying an Azure-specific sink, without touching the application code.
