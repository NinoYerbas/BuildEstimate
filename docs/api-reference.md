# API Reference — BuildEstimate

All endpoints are prefixed with `/api/v1`. All responses are wrapped in the standard envelope:

```json
{
  "success": true,
  "data": { … }
}
```

Authentication is JWT Bearer unless noted as `[AllowAnonymous]`.

---

## Projects — `/api/v1/projects`

### `GET /api/v1/projects`
List all projects.

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `status` | string (optional) | Filter by `ProjectStatus` enum value |
| `type` | string (optional) | Filter by `ProjectType` enum value |
| `search` | string (optional) | Filter by project name |

**Response:** Array of `ProjectDto`

---

### `POST /api/v1/projects`
Create a new project.

**Request Body:** `CreateProjectRequest`

```json
{
  "name": "Main Street Office Building",
  "address": "123 Main St",
  "city": "Los Angeles",
  "county": "Los Angeles",
  "state": "CA",
  "zipCode": "90001",
  "type": "Commercial",
  "isPrevailingWage": true,
  "clientName": "Acme Corp",
  "bidDueDate": "2026-04-15T00:00:00Z",
  "grossSquareFootage": 12500.0
}
```

**Response:** `ProjectDto` with HTTP 201

---

### `GET /api/v1/projects/{id}`
Get a single project by ID.

**Response:** `ProjectDto`

---

### `PUT /api/v1/projects/{id}`
Update an existing project.

**Request Body:** `UpdateProjectRequest` (same shape as `CreateProjectRequest` plus `status`)

**Response:** `ProjectDto`

---

### `DELETE /api/v1/projects/{id}`
Delete a project and all its estimates.

**Response:** HTTP 204 No Content

---

## Estimates — `/api/v1/estimates`

### `GET /api/v1/estimates`
List estimates, optionally filtered by project.

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `projectId` | Guid (optional) | Filter by project |

**Response:** Array of `EstimateDto`

---

### `POST /api/v1/estimates`
Create a new estimate for a project.

**Request Body:** `CreateEstimateRequest`

```json
{
  "projectId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "version": "v1.0",
  "description": "Initial budget estimate",
  "overheadPercent": 10.0,
  "profitPercent": 10.0,
  "contingencyPercent": 5.0
}
```

**Response:** `EstimateDto` with HTTP 201

---

### `GET /api/v1/estimates/{id}`
Get a single estimate with all line items.

**Response:** `EstimateDto`

---

### `PUT /api/v1/estimates/{id}/markups`
Update markup percentages (overhead, profit, bond, tax, contingency).

**Request Body:** `UpdateEstimateMarkupsRequest`

```json
{
  "overheadPercent": 12.0,
  "profitPercent": 8.0,
  "bondPercent": 1.5,
  "taxPercent": 0.0,
  "contingencyPercent": 7.5
}
```

**Response:** `EstimateDto`

---

### `POST /api/v1/estimates/{id}/calculate`
Recalculate all estimate totals from line items and apply markup percentages.

**Response:** `EstimateDto` with updated totals

---

### `GET /api/v1/estimates/{id}/cost-breakdown`
Get cost subtotals grouped by CSI division.

**Response:** `EstimateCostBreakdownDto`

---

### `POST /api/v1/estimates/{id}/line-items`
Add a new line item to an estimate.

**Request Body:** `CreateLineItemRequest`

```json
{
  "csiSectionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "description": "Cast-in-place concrete slab",
  "quantity": 450.0,
  "unitOfMeasure": "CY",
  "wasteFactor": 1.05,
  "materialUnitCost": 185.00,
  "laborHoursPerUnit": 1.2,
  "laborRate": 68.50
}
```

**Response:** `EstimateLineItemDto` with HTTP 201

---

### `PUT /api/v1/estimates/{estimateId}/line-items/{lineItemId}`
Update an existing line item.

**Request Body:** `UpdateLineItemRequest`

**Response:** `EstimateLineItemDto`

---

### `DELETE /api/v1/estimates/{estimateId}/line-items/{lineItemId}`
Delete a line item and recalculate estimate totals.

**Response:** HTTP 204 No Content

---

## CSI MasterFormat — `/api/v1/csi`

### `GET /api/v1/csi/divisions`
List all 34 active CSI divisions with section counts.

**Response:** Array of `CSIDivisionDto`

---

### `GET /api/v1/csi/divisions/{id}`
Get a single division with all its sections.

**Response:** `CSIDivisionDto` with a `sections` array

---

### `GET /api/v1/csi/sections/{id}`
Get a single CSI section with details.

**Response:** `CSISectionDto`

---

### `GET /api/v1/csi/search`
Search CSI codes and names.

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `q` | string | Search term (e.g., "concrete", "03 30") |

**Response:** Array of `CSISectionDto` matching the query

---

### `GET /api/v1/csi/tree`
Full hierarchical tree for the frontend code browser.

**Response:** Array of `CSITreeNodeDto` (each division with nested section children)

---

## Labor & Production Rates — `/api/v1/labor`

### `GET /api/v1/labor/trades`
List all active construction trades.

**Response:** Array of `TradeDto`

---

### `POST /api/v1/labor/trades`
Create a new trade.

**Request Body:** `CreateTradeRequest`

```json
{
  "name": "Carpenter",
  "tradeCode": "CAR",
  "unionAffiliation": "UBC Local 1977"
}
```

**Response:** `TradeDto` with HTTP 201

---

### `GET /api/v1/labor/rates`
List labor rates, optionally filtered by trade, county, and state.

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `tradeId` | Guid (optional) | Filter by trade |
| `county` | string (optional) | Filter by county |
| `state` | string (optional) | Filter by state (default: CA) |
| `prevailingOnly` | bool (optional) | Return only prevailing wage rates |

**Response:** Array of `LaborRateDto`

---

### `POST /api/v1/labor/rates`
Create a new labor rate.

**Request Body:** `CreateLaborRateRequest`

```json
{
  "tradeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "county": "Los Angeles",
  "state": "CA",
  "baseWage": 52.00,
  "healthWelfare": 8.90,
  "pension": 6.50,
  "vacationHoliday": 2.10,
  "training": 0.75,
  "rateType": "PrevailingWage",
  "effectiveDate": "2026-01-01T00:00:00Z"
}
```

**Response:** `LaborRateDto` with HTTP 201

---

### `GET /api/v1/labor/production-rates`
List production rates, optionally filtered by CSI section or trade.

**Response:** Array of `ProductionRateDto`

---

### `POST /api/v1/labor/production-rates`
Create a new production rate.

**Request Body:** `CreateProductionRateRequest`

```json
{
  "csiSectionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tradeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "description": "Install 5/8\" drywall — light commercial",
  "hoursPerUnit": 0.017,
  "unitOfMeasure": "SF",
  "crewSize": 2,
  "dailyOutput": 940,
  "source": "RSMeans"
}
```

**Response:** `ProductionRateDto` with HTTP 201

---

### `GET /api/v1/labor/lookup`
Look up the applicable labor rate and production rate for a trade + location combination.

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `csiSectionId` | Guid | CSI section being priced |
| `tradeId` | Guid | Construction trade |
| `county` | string | County name |
| `state` | string | State abbreviation |
| `isPrevailingWage` | bool | Whether to return the prevailing wage rate |

**Response:** `RateLookupResultDto`

```json
{
  "laborRate": { … },
  "productionRate": { … },
  "laborCostPerUnit": 1.17,
  "message": "Using prevailing wage rate for Los Angeles County"
}
```

---

## Quantity Takeoff — `/api/v1/takeoff`

### `GET /api/v1/takeoff`
List takeoff items for a project.

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `projectId` | Guid | Project to filter by |
| `drawingSheet` | string (optional) | Filter by drawing sheet number |
| `unlinkedOnly` | bool (optional) | Return only items not yet linked to an estimate |

**Response:** Array of `TakeoffItemDto`

---

### `POST /api/v1/takeoff`
Create a new takeoff measurement.

**Request Body:** `CreateTakeoffRequest`

```json
{
  "projectId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "csiSectionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "description": "Room 204 — drywall walls",
  "drawingSheet": "A-201",
  "unitOfMeasure": "SF",
  "length": 60.0,
  "height": 14.0,
  "count": 1
}
```

The system automatically calculates `quantity = length × height × count = 840 SF`.

**Response:** `TakeoffItemDto` with HTTP 201

---

### `POST /api/v1/takeoff/{id}/link-to-estimate`
Link a takeoff item to a specific estimate line item.

**Request Body:**

```json
{
  "estimateId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "lineItemId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Response:** Updated `TakeoffItemDto`

---

## Assemblies — `/api/v1/assemblies`

### `GET /api/v1/assemblies`
List assemblies, optionally filtered by category or search term.

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `category` | string (optional) | e.g., "Walls", "Floors", "Concrete" |
| `search` | string (optional) | Search by name or assembly code |

**Response:** Array of `AssemblyDto`

---

### `POST /api/v1/assemblies`
Create a new assembly template.

**Request Body:** `CreateAssemblyRequest`

```json
{
  "name": "Interior Gypsum Wall — Metal Stud",
  "assemblyCode": "WAL-INT-GYP",
  "category": "Walls",
  "unitOfMeasure": "SF",
  "components": [
    {
      "csiSectionId": "…",
      "description": "3-5/8\" metal stud framing",
      "quantityFactor": 1.0,
      "unitOfMeasure": "SF",
      "wasteFactor": 1.05,
      "materialUnitCost": 1.85,
      "laborHoursPerUnit": 0.022,
      "laborRate": 65.00
    }
  ]
}
```

**Response:** `AssemblyDto` with HTTP 201

---

### `POST /api/v1/assemblies/{id}/apply`
Apply an assembly to an estimate, creating one line item per component.

**Request Body:** `ApplyAssemblyRequest`

```json
{
  "estimateId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "quantity": 840.0,
  "location": "Room 204",
  "overrideLaborRates": false
}
```

**Response:** `ApplyAssemblyResultDto`

```json
{
  "assemblyName": "Interior Gypsum Wall — Metal Stud",
  "quantity": 840.0,
  "lineItemsCreated": 3,
  "totalMaterial": 4326.00,
  "totalLabor": 1209.60,
  "totalDirectCost": 5535.60,
  "updatedBidPrice": 67842.15
}
```

---

## Reports — `/api/v1/reports`

### `GET /api/v1/reports/bid-summary/{estimateId}`
Bid proposal summary grouped by CSI division. This is the document submitted to the project owner.

**Response:** Object with project info, cost by division, direct costs, markups, and `bidPrice`.

---

### `GET /api/v1/reports/detailed-cost/{estimateId}`
Full line-item cost report for internal estimator review.

**Response:** Object with division groups, each containing individual line items with all cost fields.

---

### `GET /api/v1/reports/labor-analysis/{estimateId}`
Labor hours breakdown by CSI division with scheduling estimates.

**Response:** Object with total hours, average rate, scheduling scenarios (crew of 5 vs crew of 10), and payroll preview.

---

### `GET /api/v1/reports/comparison`
Compare all estimate versions for a project side-by-side.

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `projectId` | Guid | Project to compare estimates for |

**Response:** Array of estimate summaries with version delta calculations.

---

### `GET /api/v1/reports/project-dashboard/{projectId}`
Executive project dashboard showing estimates, takeoffs, and cost breakdown at a glance.

**Response:** Object with project info, estimate stats, takeoff progress, and cost breakdown by type.

---

## AI Assistant — `/api/v1/ai`

All AI endpoints are `[AllowAnonymous]` and require the Anthropic API key to be configured in `appsettings.json`.

### `POST /api/v1/ai/validate/{estimateId}`
Validate estimate completeness. Identifies missing items, unusual quantities, or structural issues.

**Response:** `AIAnalysisResult` with issues list and recommendations.

---

### `POST /api/v1/ai/check-pricing/{estimateId}`
Compare estimate pricing against market benchmarks.

**Response:** `AIAnalysisResult` with pricing commentary per line item.

---

### `POST /api/v1/ai/suggest/{estimateId}`
Suggest assemblies or line items that may be missing based on project type.

**Response:** `AIAnalysisResult` with suggested additions.

---

### `POST /api/v1/ai/risk/{estimateId}`
Identify financial risks: subcontractor concentration, low contingency, missing allowances, etc.

**Response:** `AIAnalysisResult` with risk flags and severity levels.

---

### `POST /api/v1/ai/full-review/{estimateId}`
Run all four AI analyses in parallel and return a combined report.

**Response:** Object containing `validation`, `pricing`, `suggestions`, `risk`, and `timestamp`.
