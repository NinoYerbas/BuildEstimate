# Contributing to BuildEstimate

Thank you for your interest in contributing to **BuildEstimate**! This document explains how to get involved.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Branch Naming Conventions](#branch-naming-conventions)
3. [Running the Project Locally](#running-the-project-locally)
4. [Code Style Guidelines](#code-style-guidelines)
5. [How to Open a Pull Request](#how-to-open-a-pull-request)
6. [Code of Conduct](#code-of-conduct)

---

## Getting Started

1. **Fork** the repository by clicking the "Fork" button on GitHub.

2. **Clone** your fork locally:

   ```bash
   git clone https://github.com/YOUR_USERNAME/BuildEstimate.git
   cd BuildEstimate
   ```

3. **Add the upstream remote** so you can pull in future changes:

   ```bash
   git remote add upstream https://github.com/NinoYerbas/BuildEstimate.git
   ```

4. **Keep your fork up to date** before starting new work:

   ```bash
   git fetch upstream
   git checkout main
   git merge upstream/main
   ```

---

## Branch Naming Conventions

Use the following prefixes when creating branches:

| Prefix | When to use | Example |
|--------|-------------|---------|
| `feature/` | New functionality | `feature/export-to-pdf` |
| `fix/` | Bug fix | `fix/takeoff-calculation-error` |
| `docs/` | Documentation only | `docs/add-api-reference` |
| `refactor/` | Code cleanup, no behavior change | `refactor/extract-calculation-service` |
| `chore/` | Build scripts, dependencies | `chore/update-ef-core-8` |

**Example:**

```bash
git checkout -b feature/export-to-pdf
```

---

## Running the Project Locally

### Backend (.NET)

```bash
# 1. Make sure you have .NET 8 SDK installed
dotnet --version  # should print 8.x.x

# 2. Restore packages
dotnet restore BuildEstimate/BuildEstimate.sln

# 3. Set up the database (connection string in BuildEstimate/Api/appsettings.json)
cd BuildEstimate/Api
dotnet ef database update --startup-project . --project ../Infrastructure

# 4. Run the API
dotnet run
```

The API starts at `https://localhost:5xxx`. Swagger is available at `/swagger`.

### Frontend (TypeScript / React)

```bash
cd "BuildEstimate Frontend"
npm install
npm run dev
```

The frontend starts at `http://localhost:5173`.

---

## Code Style Guidelines

### C# (.NET)

- Follow [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).
- Use **PascalCase** for classes, methods, properties, and public members.
- Use **camelCase** for local variables and parameters.
- Prefix private fields with `_` (e.g., `_context`, `_logger`).
- Add XML doc comments (`/// <summary>`) to all public classes and methods.
- Add inline `//` comments to explain non-obvious logic.
- Never commit `appsettings.Development.json` or `appsettings.Local.json` — these contain secrets.

**Example:**

```csharp
/// <summary>
/// Retrieves all active assemblies, optionally filtered by category.
/// </summary>
/// <param name="category">Optional CSI category name to filter by.</param>
/// <returns>A list of <see cref="AssemblyDto"/> objects.</returns>
[HttpGet]
public async Task<IActionResult> GetAssemblies([FromQuery] string? category = null)
{
    // Start with all active assemblies; apply filters dynamically
    var query = _context.Assemblies
        .Where(a => a.IsActive)
        .AsQueryable();

    if (!string.IsNullOrEmpty(category))
        query = query.Where(a => a.Category == category); // filter by category name

    var assemblies = await query.ToListAsync();
    return Ok(assemblies);
}
```

### TypeScript (Frontend)

- Follow the existing ESLint configuration (run `npm run lint` before committing).
- Use **camelCase** for variables and functions.
- Use **PascalCase** for React components and interfaces.
- Prefer `const` over `let`; avoid `var`.
- Name React components with descriptive, domain-specific names (e.g., `TakeoffList`, `CostSummary`).
- Keep components small and focused — extract sub-components when a file exceeds ~150 lines.

---

## How to Open a Pull Request

1. Push your branch to your fork:

   ```bash
   git push origin feature/your-feature-name
   ```

2. Go to the original repository on GitHub and click **"Compare & pull request"**.

3. Fill in the PR template:
   - **Title**: Brief description (e.g., `feat: add PDF export for bid summary report`)
   - **Description**: What changed, why, and how to test it
   - **Checklist**: Mark off any relevant items (tests added, docs updated, etc.)

4. Request a review from a maintainer.

5. Address any review comments and push updates to the same branch.

6. Once approved and all checks pass, the maintainer will merge the PR.

---

## Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](https://www.contributor-covenant.org/version/2/1/code_of_conduct/). By participating, you agree to treat everyone with respect.

Please report unacceptable behavior to the project maintainer.
