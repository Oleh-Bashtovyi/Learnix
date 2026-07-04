# Frontend Documentation

Welcome to the frontend documentation for Learnix. This directory contains all the foundational knowledge required to develop, maintain, and understand the frontend part of the platform.

Whether you are a human developer or an AI assistant, this is your entry point.

## Table of Contents

- **[Project Structure](PROJECT_STRUCTURE.md)**: Details the folder organization, what goes where, and how to structure new features.
- **[Coding Style](CODING_STYLE.md)**: Outlines our conventions, component design patterns, styling rules, and how to write clean code.
- **[Architecture](ARCHITECTURE.md)**: Explains the high-level architecture of the frontend.
- **[Deployment](DEPLOYMENT.md)**: Instructions and environment setup for deploying the frontend application.

## Architectural Decision Records (ADR)

If you are looking for the rationale behind *why* we chose a specific technology or approach, refer to the `decisions/` directory.

- **[Decisions Register (ADR)](decisions/README.md)**: A complete index of all architectural decisions.
- **[New Decision Template](decisions/TEMPLATE.md)**: Use this template when proposing a new architectural or tooling decision.

## Development Commands

All code should be fully typed and formatted before committing. Our CI and Git pre-commit hooks enforce these standards automatically.

From the `learnix-client/` directory, you can run:

### Formatting
- `npm run format`: Format the entire project using Prettier.
- `npm run format:check`: Check if the project is formatted (runs in CI).
- `npm run format:staged`: Format only files staged in Git (runs in pre-commit).

### Linting
- `npm run lint`: Lint the entire project using ESLint.
- `npm run lint:staged`: Lint only staged files (runs in pre-commit).

### Type Checking
- `npm run type-check`: Run the TypeScript compiler to check for type errors across the whole project (runs in CI/pre-commit).

### Starting the App
- `npm run dev`: Start the Vite development server.
- `npm run build`: Build the app for production.
