# Changelog

All notable changes to **BuildEstimate** are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] — 2026-03-08

### Added

#### Project Management
- `Project` entity with full address, client info, project type, bid due date, and square footage
- `ProjectType` enum: Commercial, Residential, Industrial, Institutional, Government, Mixed-Use
- `ProjectStatus` enum: Estimating, BidSubmitted, Awarded, InProgress, Complete, Lost, OnHold
- `ProjectsController` with full CRUD at `GET/POST/PUT/DELETE /api/v1/projects`
- Project listing with estimate count and status display fields

#### CSI MasterFormat
- `CSIDivision` and `CSISection` entities following the Construction Specifications Institute standard
- All 34 CSI divisions seeded automatically on first database migration
- 70+ CSI sections seeded as initial reference data
- `CSIMasterFormatController` with browse, search, and tree endpoints at `/api/v1/csi`
- `GET /api/v1/csi/divisions` — list all divisions with section counts
- `GET /api/v1/csi/tree` — full hierarchical tree for frontend navigation
- `GET /api/v1/csi/search?q=` — full-text search across codes and names

#### Estimate Engine
- `Estimate` entity with version tracking, markup percentages (overhead, profit, bond, tax, contingency), and auto-calculated totals
- `EstimateLineItem` entity with quantity, waste factor, material/labor/equipment costs, and CSI code link
- `EstimatesController` at `/api/v1/estimates` with full line-item CRUD
- `POST /api/v1/estimates/{id}/calculate` — recalculates all estimate totals from line items
- `GET /api/v1/estimates/{id}/cost-breakdown` — cost breakdown by CSI division
- Multiple estimate versions per project with submit/lock support
- Cost-per-square-foot calculation when project square footage is available

#### Quantity Takeoff
- `TakeoffItem` entity with support for area (SF), volume (CY), linear (LF), and count (EA) measurements
- Automatic quantity calculation from dimensions (Length × Height for SF, Length × Width × Depth ÷ 27 for CY)
- Drawing sheet and tag tracking for blueprint reference
- `TakeoffController` at `/api/v1/takeoff` with full CRUD
- `POST /api/v1/takeoff/{id}/link-to-estimate` — links a takeoff item to an estimate line item
- Unlinked-item filter for tracking unmeasured quantities

#### Labor & Production Rates
- `Trade` entity for construction trade classification (Carpenter, Electrician, Plumber, etc.)
- `LaborRate` entity with base wage, fringe benefits (health & welfare, pension, vacation, training), prevailing and market rate support by county
- `ProductionRate` entity with hours-per-unit, crew size, daily output, and RSMeans-compatible data
- `LaborController` at `/api/v1/labor` for full CRUD on trades, rates, and production rates
- `GET /api/v1/labor/lookup` — rate lookup by trade, county, and prevailing wage flag
- Overtime and double-time rate fields

#### Assembly Templates
- `Assembly` entity as a named cost template with category, unit of measure, and component list
- `AssemblyComponent` entity linking CSI sections with quantity factors, waste factors, and unit costs
- "Explosion" engine: `POST /api/v1/assemblies/{id}/apply` creates all component line items in the target estimate
- Optional labor rate override when applying an assembly
- Global assembly library shareable across projects

#### AI Assistant (Claude Integration)
- `AIEstimateService` using Anthropic Claude API
- `POST /api/v1/ai/validate/{id}` — validates estimate completeness; flags missing items or unusual quantities
- `POST /api/v1/ai/check-pricing/{id}` — compares line-item pricing against market benchmarks
- `POST /api/v1/ai/suggest/{id}` — suggests assemblies or line items that appear to be missing
- `POST /api/v1/ai/risk/{id}` — identifies financial risks (concentration, low contingency, etc.)
- `POST /api/v1/ai/full-review/{id}` — runs all four analyses in parallel and returns a combined report

#### Reports
- `GET /api/v1/reports/bid-summary/{id}` — bid proposal summary grouped by CSI division
- `GET /api/v1/reports/detailed-cost/{id}` — full line-item detail report for internal review
- `GET /api/v1/reports/labor-analysis/{id}` — labor hours by trade with scheduling estimates
- `GET /api/v1/reports/comparison?projectId=` — side-by-side estimate version comparison with delta calculations
- `GET /api/v1/reports/project-dashboard/{id}` — executive dashboard with project, estimate, and takeoff summary

#### Infrastructure
- `BuildEstimateDbContext` with EF Core 8 and SQL Server
- Clean Architecture: Core → Application → Infrastructure → API layers
- `BaseApiController` with consistent JSON response envelope (`{ success, data }`)
- JWT Bearer authentication support
- Swagger / OpenAPI documentation with annotations
- Serilog structured logging to console and file
- SQL Server health check endpoint
- `Program.cs` with full DI registration for all services

#### Frontend
- TypeScript / React SPA (Vite build)
- Blueprint canvas component for drawing overlay
- Takeoff list, cost summary, measurement panel, and project header components
- Full shadcn/ui component library (buttons, cards, forms, tables, charts, etc.)
- API client service (`src/app/services/api.ts`)
- Login form component

#### Documentation
- Comprehensive `README.md` with architecture overview, setup guide, and API reference
- `CONTRIBUTING.md` with branch conventions and code style guidelines
- `CHANGELOG.md` (this file)
- `docs/architecture.md` — Clean Architecture explanation and layer diagram
- `docs/api-reference.md` — complete endpoint reference
- `docs/csi-masterformat.md` — educational CSI MasterFormat guide
- `docs/glossary.md` — construction estimating terms glossary
- XML documentation comments on all root-level C# source files
- `.gitignore` covering .NET, Node.js, OS, and IDE artifacts
- `LICENSE` (MIT)
