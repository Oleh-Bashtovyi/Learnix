# ADR: Extracting Database Migrations and Seeding to a Standalone Project

## Context
When scaling out the backend API (e.g., using Azure Container Apps), multiple instances of the application can start simultaneously. Previously, database seeding logic was executed on application startup using `IHostedService` (`CourseSeederHostedService`, `StudentSeederHostedService`, etc.). 

If multiple instances run these seeders concurrently, it causes race conditions, duplicate data insertions, and Unique Constraint violations in the database. Furthermore, executing database schema migrations (`dotnet ef database update`) as part of the CI/CD pipeline against the `Learnix.API` project requires the API to carry Entity Framework tooling dependencies, which bloats the runtime and violates the Principle of Least Privilege (the API shouldn't technically need DDL execution rights at runtime).

## Decision
We extracted all database migration execution logic and data seeding into a standalone console application: `Learnix.DbMigrator`.

1. **Migrations:** The CI/CD pipeline now executes `dotnet run --project Learnix.DbMigrator` instead of `dotnet ef database update`.
2. **Seeding:** Seeders were moved out of `Learnix.Infrastructure` and refactored into standard classes implementing `IDataSeeder`. They are executed sequentially by the Migrator.
3. **Data Separation:** Seeders are categorized into `System` (always run: Admin, Roles, Categories) and `Demo` (run only when the `--seed-demo` flag is passed: fake Courses and Students).

## Consequences

**Positive:**
- **Cloud-Native Scalability:** The API is now entirely stateless on startup. It can scale out safely without database-initialization race conditions.
- **Improved Security (Principle of Least Privilege):** DDL permissions (Create/Drop tables) can be restricted to the Migrator identity, while the API uses standard DML permissions.
- **Faster API Startup:** Removing `IHostedService` seeders significantly reduces the startup time and memory footprint of `Learnix.API`.
- **Environment Control:** We can deploy the system to production without fake demo content by simply omitting the `--seed-demo` flag.

**Negative:**
- We introduced an additional project `Learnix.DbMigrator` to maintain in the solution.
- Minor duplication of Dependency Injection configuration for setting up the `ApplicationDbContext`.
