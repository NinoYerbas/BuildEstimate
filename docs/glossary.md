# Glossary — Construction Estimating Terms

This glossary defines the key construction estimating terms used throughout the BuildEstimate codebase and documentation. Terms are listed alphabetically.

---

## A

### Assembly
A **cost template** that bundles multiple CSI line items into a single reusable unit. For example, an "Interior Metal Stud Wall" assembly might contain framing, drywall on both sides, and blocking — all under one name. When you **apply** an assembly to an estimate with a given quantity (e.g., 840 SF), the system creates all the individual line items automatically. This is the fastest way to build an estimate.

*See:* `AssembliesController`, `Assembly.cs`, `AssemblyComponent`

---

## B

### Bid Price
The **total amount a contractor proposes to charge** to complete a project. The bid price is calculated as:

```
Direct Cost
  + Overhead
  + Profit
  + Bond
  + Tax
  + Contingency
= Total Bid Price
```

If the bid is too high, the contractor loses the project to a competitor. If it's too low, the contractor may complete the work at a loss.

*See:* `Estimate.TotalBidPrice`, `ReportsController.GetBidSummary`

### Blueprint (Drawing)
The **architectural and engineering drawings** that describe what is to be built. Estimators measure quantities directly from blueprints during the quantity takeoff process. Modern blueprints are typically delivered as PDFs.

*See:* `TakeoffItem.DrawingSheet`

### Bond
A **surety bond** (typically a performance or payment bond) required on many projects, especially public works. The bond guarantees the contractor will complete the work and pay subcontractors. Bond premiums are typically 1–2% of the bid price and are included as a markup in the estimate.

*See:* `Estimate.BondPercent`, `Estimate.BondAmount`

---

## C

### Change Order
A written modification to the original contract scope, schedule, or price agreed upon by both the owner and contractor after the contract is signed. BuildEstimate focuses on the **pre-contract estimating phase**; change orders occur during construction.

### Contingency
A **reserve percentage** added to the estimate to cover unforeseen conditions, design changes, or items that were missed in the takeoff. A typical contingency is 5–10% for commercial construction. It is distinct from profit — contingency is expected to be spent; profit is the contractor's return.

*See:* `Estimate.ContingencyPercent`, `Estimate.ContingencyAmount`

### CSI MasterFormat
The **Construction Specifications Institute MasterFormat** — the industry-standard numbering system used to categorize all construction work into 34 divisions. Every line item in a BuildEstimate estimate is tagged with a CSI code.

*See:* [`docs/csi-masterformat.md`](csi-masterformat.md), `CSIDivision`, `CSISection`

### Crew Size
The **number of workers in a crew** performing a specific task. Crew size directly affects the production rate. A crew of 1 framer might install 200 SF/day; a crew of 3 might install 550 SF/day (not 600, due to coordination overhead).

*See:* `ProductionRate.CrewSize`

---

## D

### Daily Output
The **amount of work a crew can complete in one 8-hour work day**, expressed in the unit of measure for that work type (e.g., 940 SF/day for drywall installation). Daily output = 8 ÷ HoursPerUnit.

*See:* `ProductionRate.DailyOutput`

### Direct Cost
The sum of **material + labor + equipment + subcontractor** costs before any markups (overhead, profit, bond, etc.). Direct costs are what the work actually costs to perform.

*See:* `Estimate.DirectCost`

### Division (CSI)
One of the **34 top-level categories** in the CSI MasterFormat system. For example, Division 03 = Concrete, Division 09 = Finishes. Each division contains multiple sections.

*See:* `CSIDivision`

---

## E

### Estimate
A **detailed calculation of the total cost** to complete a construction project. An estimate contains line items organized by CSI code, with material, labor, equipment, and subcontractor costs for each item, plus markup percentages that produce the final bid price.

BuildEstimate supports multiple versions of an estimate per project (e.g., v1.0 = initial budget, v2.0 = after value engineering).

*See:* `Estimate`, `EstimatesController`

### Equipment Cost
Costs for **renting or owning construction equipment** used on a project (cranes, excavators, concrete pumps, scaffolding, etc.). Equipment costs are tracked separately from labor in the estimate.

*See:* `EstimateLineItem.EquipmentTotal`

---

## F

### Fringe Benefits
Supplemental compensation beyond the base wage, required under prevailing wage laws. Includes health & welfare, pension, vacation/holiday pay, training funds, and other contributions. Fringe benefits are added to the base wage to arrive at the total prevailing wage rate.

*See:* `LaborRate.HealthWelfare`, `LaborRate.Pension`, `LaborRate.VacationHoliday`, `LaborRate.Training`

---

## G

### General Conditions
Overhead costs that are project-specific but not tied to a specific work scope — things like a job-site superintendent, temporary utilities, portable toilets, a site trailer, and safety equipment. These are typically included in Division 01 (General Requirements) or as a percentage markup.

---

## L

### Labor Cost
The cost of **wages and fringe benefits paid to workers** performing construction activities. Calculated as: `LaborHoursPerUnit × AdjustedQuantity × LaborRate`.

*See:* `EstimateLineItem.LaborTotal`

### Labor Rate
The **total hourly compensation** for a construction worker, including base wage plus all fringe benefits. On prevailing wage projects, this rate is set by the government and is significantly higher than the market rate.

*See:* `LaborRate`, `LaborController`

### Line Item
A **single row** in an estimate representing one specific scope of work. Each line item has a CSI code, description, quantity, unit of measure, and cost breakdown (material, labor, equipment). An estimate is the sum of all its line items.

*See:* `EstimateLineItem`, `EstimateLineItemDto`

---

## M

### Material Cost
The cost of **physical materials** required for construction — lumber, concrete, steel, drywall, paint, pipe, conduit, fixtures, etc.

*See:* `EstimateLineItem.MaterialTotal`

### Markup
A **percentage added to the direct cost** to cover the contractor's indirect costs and profit. BuildEstimate tracks five markup types: overhead, profit, bond, tax, and contingency.

*See:* `Estimate.OverheadPercent`, `Estimate.ProfitPercent`

---

## O

### Overhead
A contractor's **indirect business costs** that cannot be attributed to a specific project — office rent, insurance, vehicles, administrative salaries, accounting software, etc. These are recovered by adding an overhead percentage (typically 8–15%) to every estimate's direct cost.

*See:* `Estimate.OverheadPercent`, `Estimate.OverheadAmount`

---

## P

### Prevailing Wage
A **government-mandated minimum wage** for each trade classification on public works projects (projects funded by federal, state, or local government money). Prevailing wages are set county-by-county and updated periodically. They are always higher than market wages and include detailed fringe benefit requirements.

In California, the Department of Industrial Relations (DIR) publishes prevailing wage rates by county and trade.

*See:* `LaborRate.RateType`, `Project.IsPrevailingWage`

### Production Rate
The **amount of time (in labor hours) required to complete one unit of work**. For example, a production rate of 0.017 hours per SF for drywall means a single worker takes 0.017 hours (about 1 minute) to install one square foot. Multiply by quantity to get total labor hours.

`LaborHours = HoursPerUnit × Quantity`

*See:* `ProductionRate`, `EstimateLineItem.LaborHoursPerUnit`

### Profit
The contractor's **financial return** on a project, expressed as a percentage of direct cost (or sometimes total cost). Also called "fee" or "margin." Typical commercial construction profit margins are 5–15%.

*See:* `Estimate.ProfitPercent`, `Estimate.ProfitAmount`

---

## Q

### Quantity
The **measured amount** of a specific work item in the appropriate unit of measure — square feet of flooring, cubic yards of concrete, linear feet of pipe, each (count) of doors. Accuracy in quantity measurement is the most critical skill in estimating.

*See:* `EstimateLineItem.Quantity`, `TakeoffItem`

### Quantity Takeoff (QTO)
The **systematic process of measuring all construction quantities** from the architectural and engineering drawings. An estimator goes through every sheet of the blueprints and records how much of each type of work is needed. These measurements feed directly into the estimate as quantities.

*See:* `TakeoffController`, `TakeoffItem`

---

## R

### RSMeans
A widely-used **cost database** published by Gordian (formerly RS Means) that provides material unit costs and production rates for thousands of construction work types, organized by CSI code. BuildEstimate's production rate data is compatible with RSMeans format.

*See:* `ProductionRate.Source`

---

## S

### Section (CSI)
A **specific work type within a CSI division**. For example, within Division 03 (Concrete), Section 03 30 00 is Cast-in-Place Concrete. There are hundreds of sections across all 34 divisions.

*See:* `CSISection`

### Subcontractor
A **specialty contractor** hired by the general contractor to perform specific scopes of work (electrical, plumbing, HVAC, roofing, etc.). Subcontractor costs are tracked separately in the estimate because the general contractor typically does not know the breakdown of the sub's labor/material split.

*See:* `EstimateLineItem.SubcontractorTotal`

---

## T

### Trade
A **construction craft classification** — Carpenter, Electrician, Iron Worker, Plumber, Laborer, Operating Engineer, etc. Each trade has its own prevailing wage rate and production capabilities.

*See:* `Trade`, `LaborController`

### Trade Code
A short alphanumeric identifier for a trade (e.g., "CAR" for Carpenter, "ELEC" for Electrician). Used for quick lookup.

*See:* `Trade.TradeCode`

---

## U

### Unit of Measure (UOM)
The **standard measurement unit** for a work type. Common units in construction:

| Abbreviation | Full Name | Typical Use |
|---|---|---|
| SF | Square Foot | Floors, walls, roofing, painting |
| CY | Cubic Yard | Concrete, excavation, fill |
| LF | Linear Foot | Pipe, conduit, trim, fencing |
| EA | Each | Doors, fixtures, equipment |
| TON | Ton | Steel, gravel |
| LS | Lump Sum | Fixed-price subcontracts, allowances |

*See:* `UnitOfMeasure` enum, `EstimateLineItem.UnitOfMeasure`

---

## V

### Value Engineering (VE)
The process of **reducing project cost** without sacrificing function or quality — for example, specifying a less expensive roofing material that meets the same performance standard. In BuildEstimate, a second estimate version (v2.0) is often a VE estimate.

*See:* `Estimate.Version`, `ReportsController.GetEstimateComparison`

---

## W

### Waste Factor
A **multiplier applied to the quantity** to account for material waste during installation. For example, tile is typically purchased at a 10% waste factor (factor = 1.10) because cuts and breakage waste 10% of the material. The waste factor converts raw quantity to the adjusted (purchased) quantity.

`AdjustedQuantity = Quantity × WasteFactor`

*See:* `EstimateLineItem.WasteFactor`, `EstimateLineItem.AdjustedQuantity`
