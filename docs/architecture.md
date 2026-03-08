# Architecture Overview — BuildEstimate

## What is Clean Architecture?

**Clean Architecture** (introduced by Robert C. Martin) organizes a codebase into concentric layers, where the **inner layers define the rules** and the **outer layers implement them**. The key rule is:

> **Dependencies always point inward.** An outer layer can reference an inner layer, but never the reverse.

This makes the core business logic independent of frameworks, databases, and UI — meaning you can swap SQL Server for PostgreSQL, or replace the REST API with gRPC, without touching any business rules.

---

## Layer Diagram

```
┌──────────────────────────────────────────────────────────────────────────┐
│                          Frontend (TypeScript / React)                   │
│                                                                          │
│   "BuildEstimate Frontend/"                                              │
│   • blueprint-canvas.tsx      — Draw measurements on a blueprint        │
│   • takeoff-list.tsx          — List and edit quantity measurements      │
│   • cost-summary.tsx          — Show estimate totals                     │
│   • services/api.ts           — HTTP client that talks to the backend    │
│                                                                          │
│   Communicates via HTTP JSON to the API layer                            │
└──────────────────────────────┬───────────────────────────────────────────┘
                               │ HTTP (JSON)
                               ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                    API Layer  (BuildEstimate.Api)                        │
│                                                                          │
│   "BuildEstimate/Api/"                                                   │
│   • Program.cs                — Startup, DI registration, middleware     │
│   • BaseApiController.cs      — Shared response helpers (Ok, NotFound…) │
│   • ProjectsController.cs     — /api/v1/projects                        │
│   • EstimatesController.cs    — /api/v1/estimates                       │
│   • CSIMasterFormatController — /api/v1/csi                             │
│   • LaborController.cs        — /api/v1/labor                           │
│   • TakeoffController.cs      — /api/v1/takeoff                         │
│   • AssembliesController.cs   — /api/v1/assemblies                      │
│   • ReportsController.cs      — /api/v1/reports                         │
│   • AIAssistantController.cs  — /api/v1/ai                              │
│                                                                          │
│   Knows about: Application, Core, Infrastructure                        │
└──────────────────────────────┬───────────────────────────────────────────┘
                               │ references
                               ▼
┌──────────────────────────────────────────────────────────────────────────┐
│               Application Layer  (BuildEstimate.Application)            │
│                                                                          │
│   "BuildEstimate/Application/"                                           │
│   • DTOs/ProjectDtos.cs       — ProjectDto, CreateProjectRequest        │
│   • DTOs/EstimateDtos.cs      — EstimateDto, EstimateLineItemDto        │
│   • DTOs/LaborDtos.cs         — TradeDto, LaborRateDto, RateLookup      │
│   • DTOs/TakeoffDtos.cs       — TakeoffItemDto, CreateTakeoffRequest    │
│   • DTOs/AssemblyDtos.cs      — AssemblyDto, ApplyAssemblyRequest       │
│   • Services/AIEstimateService.cs — Claude API integration              │
│                                                                          │
│   Knows about: Core                                                      │
└──────────────────────────────┬───────────────────────────────────────────┘
                               │ references
                               ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                    Core Layer  (BuildEstimate.Core)                     │
│                                                                          │
│   "BuildEstimate/Core/"                                                  │
│   • Entities/Project.cs           — The Project domain entity           │
│   • Entities/CSIMasterFormat.cs   — CSIDivision, CSISection             │
│   • Entities/Estimate.cs          — Estimate, EstimateLineItem          │
│   • Entities/LaborAndProduction.cs — Trade, LaborRate, ProductionRate   │
│   • Entities/Assembly.cs          — Assembly, AssemblyComponent         │
│   • Enums/ProjectEnums.cs         — ProjectType, ProjectStatus, UOM     │
│                                                                          │
│   NO external dependencies — pure C# domain model                      │
│   This is the "what" of the system, not the "how"                       │
└──────────────────────────────┬───────────────────────────────────────────┘
                               │ implemented by
                               ▼
┌──────────────────────────────────────────────────────────────────────────┐
│              Infrastructure Layer  (BuildEstimate.Infrastructure)       │
│                                                                          │
│   "BuildEstimate/Infrastructure/"                                        │
│   • Data/BuildEstimateDbContext.cs  — EF Core context + seed data       │
│   • Migrations/                    — EF Core migration files            │
│                                                                          │
│   Knows about: Core                                                      │
│   Uses: Entity Framework Core 8, SQL Server                             │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Layer Descriptions

### Core Layer — `BuildEstimate.Core`

The **heart of the system**. Contains everything the business domain needs:

- **Entities** — C# classes that map to database tables (Project, Estimate, CSIDivision, etc.)
- **Enums** — Strongly-typed values for concepts like project type and status

The Core layer has **zero external NuGet dependencies**. It does not reference EF Core, ASP.NET, or anything else. This means you can unit test business logic without spinning up a database.

### Application Layer — `BuildEstimate.Application`

Contains **use cases** and **data transfer objects (DTOs)**:

- **DTOs** — Simplified representations of entities tailored for the API. An entity might have 30 columns; the DTO exposes only what the frontend needs.
- **Services** — `AIEstimateService` orchestrates calls to the Claude API. It takes an `EstimateForAI` object (assembled in the controller) and returns structured analysis.

### Infrastructure Layer — `BuildEstimate.Infrastructure`

Contains everything that deals with **external systems**:

- **`BuildEstimateDbContext`** — the EF Core `DbContext` that maps C# entities to SQL Server tables. It defines `DbSet<T>` properties and configures relationships in `OnModelCreating`.
- **Migrations** — EF Core-generated SQL scripts that create and evolve the database schema.
- **Seed data** — All 34 CSI MasterFormat divisions and their sections are seeded automatically on the first `Update-Database`.

### API Layer — `BuildEstimate.Api`

The **entry point** for all HTTP traffic:

- **Controllers** — Each controller handles one resource type (projects, estimates, etc.). They validate input, call the database via the `DbContext`, map entities to DTOs, and return HTTP responses.
- **`BaseApiController`** — A shared base class that wraps responses in a consistent JSON envelope: `{ "success": true, "data": { … } }`.
- **`Program.cs`** — Registers all services in the dependency injection container, configures middleware (authentication, CORS, Swagger), and starts the web server.

### Frontend — `BuildEstimate Frontend`

A **TypeScript React SPA** (Single Page Application) built with Vite:

- Communicates with the backend exclusively through the API service (`src/app/services/api.ts`)
- Renders interactive components for blueprint measurement, cost review, and project management
- Uses Tailwind CSS and a full shadcn/ui component library for consistent styling

---

## Data Flow Example: Creating an Estimate Line Item

```
User fills in the "Add Line Item" form in the UI
    ↓
Frontend calls POST /api/v1/estimates/{id}/line-items (api.ts)
    ↓
EstimatesController receives CreateLineItemRequest DTO
    ↓
Controller looks up the parent Estimate and CSISection in BuildEstimateDbContext
    ↓
Controller creates a new EstimateLineItem entity (from Core layer)
    ↓
Controller calls CalculateEstimateTotals() → loops all line items, sums material/labor/equipment
    ↓
Controller applies markups (overhead %, profit %) to calculate TotalBidPrice
    ↓
DbContext.SaveChangesAsync() writes changes to SQL Server
    ↓
Controller maps the updated Estimate to EstimateDto (from Application layer)
    ↓
Returns HTTP 201 with the new line item as JSON
    ↓
Frontend receives response and updates the cost summary display
```

---

## Why This Architecture?

| Benefit | Explanation |
|---------|-------------|
| **Testability** | Core entities and business logic can be unit tested without a database |
| **Flexibility** | Swap SQL Server for PostgreSQL by replacing only the Infrastructure layer |
| **Readability** | Each layer has a single, clear responsibility |
| **Security** | DTOs prevent over-posting attacks; only DTO fields can be set by external callers |
| **Scalability** | Application and Infrastructure layers can be split into separate microservices later |
