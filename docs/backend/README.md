# Backend Documentation

Welcome to the backend documentation for Learnix. This directory contains all the foundational knowledge required to develop, maintain, and understand the C# /.NET backend part of the platform.

Whether you are a human developer or an AI assistant, this is your entry point.

## Table of Contents

- **[Project Structure](PROJECT_STRUCTURE.md)**: Details the folder organization, layered architecture boundaries, and what goes where.
- **[Architecture](ARCHITECTURE.md)**: Explains the high-level architecture of the backend (Clean Architecture, CQRS, MediatR, EF Core).

## Architectural Decision Records (ADR)

If you are looking for the rationale behind *why* we chose a specific technology or approach (e.g., why we use PostgreSQL + MongoDB, or how we handle AI chat), refer to the `decisions/` directory.

- **[Decisions Register (ADR)](decisions/README.md)**: A complete index of all architectural decisions.
- **[New Decision Template](decisions/TEMPLATE.md)**: Use this template when proposing a new architectural or tooling decision.

## Development Commands

Run these commands from the `Learnix.Backend/` directory.

### Building & Testing
- `dotnet build`: Build the entire backend solution.
- `dotnet test`: Run all unit and integration tests.

### Running the Application
The backend is split into the main API and a standalone migration runner.

1. **Run Database Migrations & Seeding:**
   ```bash
   dotnet run --project Learnix.DbMigrator --launch-profile Development -- --create-blob
   ```
   *(Run this first to ensure your local PostgreSQL database schema is up-to-date and seeded with initial data).*

2. **Start the API Server:**
   ```bash
   dotnet run --project Learnix.API
   ```
   *(Starts the main ASP.NET Core API server).*
