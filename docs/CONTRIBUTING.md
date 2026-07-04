# Contributing & Commit Convention

This repository follows [Conventional Commits](https://www.conventionalcommits.org/) for all commit messages.

## Format

```
<type>: <short summary>

[optional body]
```

## Types

| Type | When to use |
|---|---|
| `feat` | New feature (user-facing or internal capability) |
| `fix` | Bug fix |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `docs` | Documentation only (README, ADRs, code comments) |
| `chore` | Tooling, dependencies, build config, no production code change |
| `test` | Adding or fixing tests |
| `perf` | Performance improvement |
| `style` | Formatting, whitespace, no logic change |

## Examples

```
feat: add course enrollment command
fix: resolve 401 refresh loop in axios interceptor
refactor: extract pagination logic to shared hook
docs: add FADR-011 tooling decisions
chore: bump .NET SDK to 8.0.400
```

Scope in parentheses is optional for disambiguation:
```
feat(auth): add Google OAuth callback endpoint
fix(frontend): correct CourseCard responsive layout
```

## Pre-commit Checks

This project uses Husky to automatically run formatting, type-checking, and code duplication validation before every commit. 
If your commit fails due to code duplication (> 5%), you can manually check which files are affected by running:

```bash
npm run check:duplication:report
```

For more details on local setup and running manual checks, refer to the [Local Development Setup Guide](./DEV_SETUP.md#running-checks-manually).
