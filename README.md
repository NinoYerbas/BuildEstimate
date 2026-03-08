# 🏗️ BuildEstimate

> **Construction Cost Estimating Software** — a full-stack application that automates the process of building a construction bid from start to finish, covering quantity takeoff, CSI MasterFormat coding, prevailing wages, assembly templates, AI-powered analysis, and final report generation.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178C6)](https://www.typescriptlang.org)

**Author:** Julio Cesar Mendez Tobar

---

## Table of Contents

1. [Features](#features)
2. [Tech Stack](#tech-stack)
3. [Architecture Overview](#architecture-overview)
4. [Repository Structure](#repository-structure)
5. [Getting Started](#getting-started)
   - [Prerequisites](#prerequisites)
   - [Backend Setup](#backend-setup)
   - [Frontend Setup](#frontend-setup)
6. [API Endpoints](#api-endpoints)
7. [Key Concepts](#key-concepts)
8. [Development Roadmap](#development-roadmap)
9. [Contributing](#contributing)
10. [License](#license)

---

## Features

| Feature | Description |
|---------|-------------|
| **Projects** | Create and manage construction projects with address, type, client info, bid due date, and square footage |
| **CSI MasterFormat** | Browse and search all 34 divisions and 70+ sections of the Construction Specifications Institute code system |
| **Estimates** | Build detailed cost estimates with line items organized by CSI code; supports multiple versions per project |
| **Quantity Takeoff** | Record measurements from blueprints; supports area (SF), volume (CY), and linear (LF) calculations |
| **Assemblies** | Pre-built cost templates that "explode" into multiple line items (e.g., one "Interior Wall" assembly = framing + drywall + paint) |
| **Labor & Production Rates** | Track prevailing wages by county and trade; store production rates for labor-hour calculations |
| **AI Assistant** | Claude-powered estimate validation, pricing review, assembly suggestions, and risk analysis |
| **Reports** | Generate bid proposal summaries, detailed cost reports, labor analyses, and executive dashboards |
| **Version Comparison** | Compare multiple estimate versions side-by-side with delta calculations |

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| **Runtime** | .NET 8 (ASP.NET Core Web API) |
| **Language** | C# 12 |
| **ORM** | Entity Framework Core 8 |
| **Database** | SQL Server |
| **API Docs** | Swagger / OpenAPI (Swashbuckle) |
| **Logging** | Serilog |
| **AI** | Anthropic Claude API |
| **Frontend** | TypeScript, React, Vite |
| **Styling** | Tailwind CSS |
| **Auth** | JWT Bearer tokens |

---

## Architecture Overview

BuildEstimate follows **Clean Architecture** — a layered approach that keeps business logic independent of frameworks, databases, and UI.

```
┌──────────────────────────────────────────────────────────────────┐
│                         Frontend (TypeScript / React)            │
│          src/app/  →  components, services, styles               │
└──────────────────────────┬───────────────────────────────────────┘
                           │ HTTP (JSON)
┌──────────────────────────▼───────────────────────────────────────┐
│                     API Layer  (BuildEstimate.Api)                │
│   Controllers, routing, HTTP request/response, JWT auth          │
│   Program.cs configures services and middleware                  │
└──────────────────────────┬───────────────────────────────────────┘
                           │ depends on ↓
┌──────────────────────────▼───────────────────────────────────────┐
│               Application Layer  (BuildEstimate.Application)     │
│   DTOs, Services (AIEstimateService), business use cases         │
└──────────────────────────┬───────────────────────────────────────┘
                           │ depends on ↓
┌──────────────────────────▼───────────────────────────────────────┐
│                   Core Layer  (BuildEstimate.Core)               │
│   Entities (Project, Estimate, CSI…), Enums                      │
│   No external dependencies — pure C# domain model               │
└──────────────────────────┬───────────────────────────────────────┘
                           │ implemented by ↓
┌──────────────────────────▼───────────────────────────────────────┐
│             Infrastructure Layer  (BuildEstimate.Infrastructure)  │
│   BuildEstimateDbContext, EF Core migrations, SQL Server         │
└──────────────────────────────────────────────────────────────────┘
```

**The rule:** inner layers never reference outer layers. Core knows nothing about EF Core or HTTP. This makes the business logic easy to test and reuse.

See [`docs/architecture.md`](docs/architecture.md) for a deeper explanation.

---

## Repository Structure

```
BuildEstimate/                     ← Repository root
├── .gitignore                     ← Excludes build artifacts, secrets, node_modules
├── LICENSE                        ← MIT License
├── README.md                      ← This file
├── CONTRIBUTING.md                ← How to contribute
├── CHANGELOG.md                   ← Version history
│
├── BuildEstimate.sln              ← Root solution file (open in Visual Studio)
├── BuildEstimate.Core.csproj      ← Core library project
├── BuildEstimate.Infrastructure.csproj ← Infrastructure library project
├── appsettings.json               ← App configuration (connection string, JWT)
│
├── *.cs                           ← Root-level C# source files (draft/staging)
│
├── BuildEstimate/                 ← Main application source code
│   ├── Core/                      ← Entities, Enums (domain model)
│   ├── Application/               ← DTOs, Services (use cases)
│   ├── Infrastructure/            ← EF Core DbContext, Migrations
│   ├── Api/                       ← Controllers, Program.cs, appsettings.json
│   └── BuildEstimate.sln          ← Inner solution file
│
└── BuildEstimate Frontend/        ← TypeScript / React frontend
    ├── src/
    │   ├── app/                   ← React components and app logic
    │   │   ├── components/        ← UI components (blueprint-canvas, takeoff-list, …)
    │   │   └── services/          ← API client (api.ts)
    │   └── styles/                ← Tailwind + theme CSS
    ├── package.json               ← npm dependencies
    └── vite.config.ts             ← Vite build configuration
```

---

## Getting Started

### Prerequisites

- [Visual Studio 2022](https://visualstudio.microsoft.com/) (or VS Code with C# Dev Kit)
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express is free)
- [Node.js 18+](https://nodejs.org/) (for the frontend)

### Backend Setup

```bash
# 1. Clone the repository
git clone https://github.com/NinoYerbas/BuildEstimate.git
cd BuildEstimate

# 2. Open the solution in Visual Studio
#    Double-click BuildEstimate/BuildEstimate.sln
#    OR open it from the command line:
start BuildEstimate/BuildEstimate.sln
```

**3. Set the connection string** in `BuildEstimate/Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=BuildEstimate;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

```bash
# 4. Create and apply the database migration
#    In Visual Studio: Tools → NuGet Package Manager → Package Manager Console
#    Set Default Project = BuildEstimate.Infrastructure, then run:
Add-Migration InitialCreate -StartupProject BuildEstimate.Api
Update-Database -StartupProject BuildEstimate.Api

# 5. Run the application (press F5 in Visual Studio, or):
dotnet run --project BuildEstimate/Api/BuildEstimate.Api.csproj
```

Swagger UI opens at `https://localhost:5xxx/swagger` and the database is seeded with all 34 CSI divisions automatically.

### Frontend Setup

```bash
# 1. Navigate to the frontend directory
cd "BuildEstimate Frontend"

# 2. Install npm dependencies
npm install

# 3. Start the development server
npm run dev
```

The app opens at `http://localhost:5173` by default.

---

## API Endpoints

| Controller | Base Route | Purpose |
|------------|------------|---------|
| `ProjectsController` | `GET/POST /api/v1/projects` | Create and list construction projects |
| `ProjectsController` | `GET/PUT/DELETE /api/v1/projects/{id}` | Read, update, delete a project |
| `EstimatesController` | `GET/POST /api/v1/estimates` | Create and list estimates |
| `EstimatesController` | `POST /api/v1/estimates/{id}/calculate` | Recalculate all estimate totals |
| `EstimatesController` | `POST /api/v1/estimates/{id}/line-items` | Add a cost line item |
| `CSIMasterFormatController` | `GET /api/v1/csi/divisions` | List all 34 CSI divisions |
| `CSIMasterFormatController` | `GET /api/v1/csi/tree` | Full hierarchical code tree |
| `CSIMasterFormatController` | `GET /api/v1/csi/search?q=` | Search CSI codes |
| `LaborController` | `GET/POST /api/v1/labor/trades` | Manage construction trades |
| `LaborController` | `GET/POST /api/v1/labor/rates` | Manage labor rates by county |
| `LaborController` | `GET /api/v1/labor/lookup` | Look up rate for a trade + location |
| `TakeoffController` | `GET/POST /api/v1/takeoff` | Record quantity measurements |
| `TakeoffController` | `POST /api/v1/takeoff/{id}/link` | Link a takeoff to an estimate line item |
| `AssembliesController` | `GET/POST /api/v1/assemblies` | Browse and create assembly templates |
| `AssembliesController` | `POST /api/v1/assemblies/{id}/apply` | Apply assembly to an estimate |
| `ReportsController` | `GET /api/v1/reports/bid-summary/{id}` | Bid proposal summary |
| `ReportsController` | `GET /api/v1/reports/detailed-cost/{id}` | Detailed line-item cost report |
| `ReportsController` | `GET /api/v1/reports/labor-analysis/{id}` | Labor hours and payroll analysis |
| `ReportsController` | `GET /api/v1/reports/comparison?projectId=` | Compare estimate versions |
| `ReportsController` | `GET /api/v1/reports/project-dashboard/{id}` | Executive project dashboard |
| `AIAssistantController` | `POST /api/v1/ai/validate/{id}` | AI: validate estimate completeness |
| `AIAssistantController` | `POST /api/v1/ai/check-pricing/{id}` | AI: check pricing vs. market |
| `AIAssistantController` | `POST /api/v1/ai/suggest/{id}` | AI: suggest missing assemblies |
| `AIAssistantController` | `POST /api/v1/ai/risk/{id}` | AI: analyze financial risk |
| `AIAssistantController` | `POST /api/v1/ai/full-review/{id}` | AI: run all analyses in parallel |

For full request/response schemas see [`docs/api-reference.md`](docs/api-reference.md).

---

## Key Concepts

### CSI MasterFormat
The **Construction Specifications Institute (CSI) MasterFormat** is the standard numbering system used by the construction industry to organize work into divisions. All 34 divisions are pre-loaded as seed data.

Example: `03 30 00 — Cast-in-Place Concrete`  
- `03` = Division (Concrete)  
- `30` = Section (Cast-in-Place)  
- `00` = Sub-section

See [`docs/csi-masterformat.md`](docs/csi-masterformat.md) for all 34 divisions.

### Quantity Takeoff
A **quantity takeoff** (QTO) is the process of measuring every item from the construction drawings — how many square feet of drywall, how many linear feet of pipe, how many cubic yards of concrete. These quantities feed directly into the estimate's line items.

### Assemblies
An **assembly** is a cost template that bundles multiple CSI line items together. For example, an "Interior Drywall Wall" assembly might contain:
- `09 21 16` — Gypsum board (material cost)
- `09 22 16` — Metal stud framing (material + labor)
- `09 91 00` — Painting (labor + material)

Applying the assembly to an estimate automatically creates all three line items from a single quantity.

### Prevailing Wages
In California and many other states, public works projects require contractors to pay **prevailing wages** — government-mandated minimum rates per trade per county. These are significantly higher than market wages and include fringe benefits (health & welfare, pension, vacation). BuildEstimate tracks both market and prevailing wage rates and applies the correct one based on the project's `IsPrevailingWage` flag.

---

## Development Roadmap

| Phase | Status | Description |
|-------|--------|-------------|
| **Phase 1** | ✅ Complete | Foundation — Projects + CSI MasterFormat |
| **Phase 2** | ✅ Complete | Estimate Engine — line items, cost calculation |
| **Phase 3** | ✅ Complete | Quantity Takeoff — measuring from blueprints |
| **Phase 4** | ✅ Complete | Labor & Production Rates — prevailing wages |
| **Phase 5** | ✅ Complete | Assemblies — template explosion engine |
| **Phase 6** | ✅ Complete | AI Integration — Claude-powered analysis |
| **Phase 7** | ✅ Complete | Reports + Deployment |

---

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on how to fork, branch, open pull requests, and follow code style conventions.

---

## License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

Copyright © 2026 Julio Cesar Mendez Tobar
