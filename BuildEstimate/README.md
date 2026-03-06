# 🏗 BuildEstimate — Construction Estimating Software

### By Julio Cesar Mendez Tobar
### Phase 1: Foundation — Projects + CSI MasterFormat

---

## Quick Start (on your Windows machine)

### Step 1: Copy the files
Place the `BuildEstimate/` folder alongside your JERP project:
```
C:\Users\ichbi\OneDrive\Documents\GitHub\
├── JERP-2.0/          ← your existing JERP project
└── BuildEstimate/     ← this new project
```

### Step 2: Open in Visual Studio
Double-click `BuildEstimate.sln`

### Step 3: Create the first migration
Open **Package Manager Console** (Tools → NuGet Package Manager → Package Manager Console)
Set **Default project** to `BuildEstimate.Infrastructure`
```
Add-Migration InitialCreate -StartupProject BuildEstimate.Api
```

### Step 4: Run the application
Press **F5** or click the green play button.
- Swagger opens at https://localhost:5xxx/
- Database is created automatically with all 34 CSI divisions and 70+ sections

### Step 5: Test in Swagger
- `GET /api/v1/csi/divisions` → See all 34 CSI divisions
- `GET /api/v1/csi/tree` → See the full hierarchy
- `GET /api/v1/csi/search?q=concrete` → Search for codes
- `POST /api/v1/projects` → Create your first construction project

---

## Phase 1 Files (12 files)

| File | Purpose |
|------|---------|
| `BuildEstimate.sln` | Solution file — open this in Visual Studio |
| `Core/Entities/Project.cs` | Construction project entity |
| `Core/Entities/CSIMasterFormat.cs` | CSI Division + Section entities |
| `Core/Entities/Estimate.cs` | Estimate + TakeoffItem skeletons (Phase 2-3) |
| `Core/Enums/ProjectEnums.cs` | ProjectType, ProjectStatus, UnitOfMeasure |
| `Application/DTOs/ProjectDtos.cs` | Data transfer objects |
| `Infrastructure/Data/BuildEstimateDbContext.cs` | Database context + CSI seed data |
| `Api/Controllers/BaseApiController.cs` | Shared controller base (from JERP) |
| `Api/Controllers/ProjectsController.cs` | Project CRUD endpoints |
| `Api/Controllers/CSIMasterFormatController.cs` | CSI code browsing |
| `Api/Program.cs` | Application startup |
| `Api/appsettings.json` | Configuration |

## What's Next

- **Phase 2:** Estimate engine (line items, cost calculation)
- **Phase 3:** Quantity takeoff
- **Phase 4:** Prevailing wages + production rates
- **Phase 5:** Assemblies (the killer feature)
- **Phase 6:** AI integration (Claude)
- **Phase 7:** Reports + deployment
