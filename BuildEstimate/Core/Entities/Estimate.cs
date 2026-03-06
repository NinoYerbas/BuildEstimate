/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// ESTIMATE.CS — Phase 2: THE FULL COST ESTIMATE
// ============================================================================
//
// THE FUNDAMENTAL EQUATION OF CONSTRUCTION ESTIMATING:
//
//   Material + Labor + Equipment + Subcontractor = DIRECT COST
//   Direct Cost × (1 + Overhead%) = COST WITH OVERHEAD
//   Cost With Overhead × (1 + Profit%) = SELLING PRICE
//   Selling Price + Bond + Tax + Contingency = BID PRICE
//
// This is like JERP's Debit = Credit — the equation everything is built on.
//
// JERP EQUIVALENT:
//   Estimate ≈ SalesOrder (a document with line items and a total)
//   EstimateLineItem ≈ JournalEntryLine (individual lines that add up)
//
// ============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildEstimate.Core.Entities;

/// <summary>
/// A construction cost estimate — the main document the system produces.
/// One Project can have multiple Estimates (original, revised, value-engineered).
/// </summary>
[Table("Estimates")]
public class Estimate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // =====================================================================
    // PARENT PROJECT
    // =====================================================================
    [Required]
    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    // =====================================================================
    // ESTIMATE IDENTITY
    // =====================================================================

    [Required]
    [MaxLength(50)]
    public string Version { get; set; } = "v1.0";
    // "v1.0", "v2.0 - Value Engineered", "v3.0 - Addendum 1"

    [MaxLength(500)]
    public string? Description { get; set; }

    // =====================================================================
    // COST TOTALS — Calculated from Line Items
    // =====================================================================
    // These are COMPUTED fields — recalculated when line items change.
    //
    // WHY STORE COMPUTED VALUES?
    //   Dashboard shows totals for 50 projects at once.
    //   Computing SUM() across 50 projects × 200 line items each = slow.
    //   Storing the pre-calculated total = instant.
    //   We recalculate only when a line item changes.
    //
    //   JERP does this too: Account.Balance is stored, not recomputed
    //   from every journal entry on each read.
    // =====================================================================

    [Column(TypeName = "decimal(18,2)")]
    public decimal MaterialTotal { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal LaborTotal { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal EquipmentTotal { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubcontractorTotal { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DirectCost { get; set; } = 0;
    // Material + Labor + Equipment + Subcontractor
    // This is the ACTUAL COST to build — before any markup.

    // =====================================================================
    // MARKUP PERCENTAGES
    // =====================================================================
    // Think of it like retail:
    //   A shirt costs $10 to make → store sells for $15 → $5 is markup
    //   A building costs $1M to build → bid $1.21M → $210K covers overhead + profit
    // =====================================================================

    [Column(TypeName = "decimal(5,2)")]
    public decimal OverheadPercent { get; set; } = 10.00m;
    // Company overhead: office rent, admin staff, insurance, trucks
    // Typical: 8-15%

    [Column(TypeName = "decimal(18,2)")]
    public decimal OverheadAmount { get; set; } = 0;

    [Column(TypeName = "decimal(5,2)")]
    public decimal ProfitPercent { get; set; } = 10.00m;
    // Contractor's profit margin. Typical: 5-15%

    [Column(TypeName = "decimal(18,2)")]
    public decimal ProfitAmount { get; set; } = 0;

    [Column(TypeName = "decimal(5,2)")]
    public decimal BondPercent { get; set; } = 0;
    // Performance bond — insurance that you'll finish the job
    // Required on government projects. Typical: 1-3%

    [Column(TypeName = "decimal(18,2)")]
    public decimal BondAmount { get; set; } = 0;

    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxPercent { get; set; } = 0;
    // Sales tax on MATERIALS ONLY (labor is NOT taxed in most states)
    // California: 7.25-10.75% depending on city

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; } = 0;

    [Column(TypeName = "decimal(5,2)")]
    public decimal ContingencyPercent { get; set; } = 5.00m;
    // Safety cushion for unknowns. Typical: 3-10%

    [Column(TypeName = "decimal(18,2)")]
    public decimal ContingencyAmount { get; set; } = 0;

    // =====================================================================
    // THE FINAL NUMBER
    // =====================================================================

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalBidPrice { get; set; } = 0;
    // DirectCost + Overhead + Profit + Bond + Tax + Contingency
    // THIS IS THE NUMBER ON THE BID PROPOSAL.

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CostPerSquareFoot { get; set; }
    // TotalBidPrice ÷ Project.GrossSquareFootage
    // Sanity check: "Medical office at $185/SF seems low..."

    // =====================================================================
    // STATUS & LINE ITEMS
    // =====================================================================

    public bool IsSubmitted { get; set; } = false;
    public DateTime? SubmittedDate { get; set; }

    public List<EstimateLineItem> LineItems { get; set; } = new();

    // =====================================================================
    // AUDIT
    // =====================================================================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastCalculatedAt { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; }
}

// ============================================================================
// ESTIMATE LINE ITEM — One Row In The Estimate
// ============================================================================
//
// THIS IS WHERE THE MONEY MATH HAPPENS.
//
// Each line = one piece of work:
//   "Install 9,400 SF of 5/8" drywall on metal studs"
//   Material: 10,340 SF × $0.52/SF = $5,376.80
//   Labor:    175.78 hrs × $65/hr  = $11,425.70
//   Equipment: $0
//   Sub: $0
//   LINE TOTAL: $16,802.50
//
// The four cost components:
//   MATERIAL — what you buy (pipe, drywall, concrete)
//   LABOR — what you pay workers (hours × wage rate)
//   EQUIPMENT — what you rent (crane, excavator, scaffolding)
//   SUBCONTRACTOR — what an outside company charges (their lump sum)
//
// JERP EQUIVALENT:
//   JournalEntryLine: AccountId, Debit, Credit
//   EstimateLineItem: CSISectionId, Material, Labor, Equipment, Sub
// ============================================================================

[Table("EstimateLineItems")]
public class EstimateLineItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // =====================================================================
    // PARENT ESTIMATE
    // =====================================================================
    [Required]
    public Guid EstimateId { get; set; }

    [ForeignKey(nameof(EstimateId))]
    public Estimate? Estimate { get; set; }

    // =====================================================================
    // CSI CODE — What type of work is this?
    // =====================================================================
    [Required]
    public Guid CSISectionId { get; set; }

    [ForeignKey(nameof(CSISectionId))]
    public CSISection? CSISection { get; set; }

    // =====================================================================
    // DESCRIPTION & QUANTITY
    // =====================================================================

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    // "5/8\" Type X Gypsum Board on metal studs, Level 4 finish"

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; } = 0;
    // 9,400 (square feet)

    [Required]
    [MaxLength(10)]
    public string UnitOfMeasure { get; set; } = "SF";

    [Column(TypeName = "decimal(5,2)")]
    public decimal WasteFactor { get; set; } = 1.00m;
    // 1.00 = no waste, 1.10 = 10% waste
    // Drywall: 10%, Concrete: 5%, Doors: 0%

    [Column(TypeName = "decimal(18,4)")]
    public decimal AdjustedQuantity { get; set; } = 0;
    // Quantity × WasteFactor = what you actually buy

    // =====================================================================
    // MATERIAL COST
    // =====================================================================

    [Column(TypeName = "decimal(18,4)")]
    public decimal MaterialUnitCost { get; set; } = 0;
    // $0.52 per SF for drywall board

    [Column(TypeName = "decimal(18,2)")]
    public decimal MaterialTotal { get; set; } = 0;
    // AdjustedQuantity × MaterialUnitCost

    // =====================================================================
    // LABOR COST
    // =====================================================================

    [Column(TypeName = "decimal(18,4)")]
    public decimal LaborHoursPerUnit { get; set; } = 0;
    // 0.017 hours per SF (from RSMeans production rates)

    [Column(TypeName = "decimal(18,2)")]
    public decimal LaborHours { get; set; } = 0;
    // AdjustedQuantity × LaborHoursPerUnit

    [Column(TypeName = "decimal(18,2)")]
    public decimal LaborRate { get; set; } = 0;
    // $65.00/hr (from prevailing wages or market)

    [Column(TypeName = "decimal(18,2)")]
    public decimal LaborTotal { get; set; } = 0;
    // LaborHours × LaborRate

    // =====================================================================
    // EQUIPMENT & SUBCONTRACTOR
    // =====================================================================

    [Column(TypeName = "decimal(18,2)")]
    public decimal EquipmentTotal { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubcontractorTotal { get; set; } = 0;
    // When SubcontractorTotal > 0, Material/Labor/Equipment are usually 0

    // =====================================================================
    // LINE TOTAL
    // =====================================================================

    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; set; } = 0;
    // MaterialTotal + LaborTotal + EquipmentTotal + SubcontractorTotal
    // Sum of ALL LineTotals = Estimate.DirectCost

    // =====================================================================
    // REFERENCE & NOTES
    // =====================================================================

    [MaxLength(100)]
    public string? TakeoffSource { get; set; }
    // "Sheet A-101, Room 204"

    public Guid? TakeoffItemId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int SortOrder { get; set; } = 0;

    // AUDIT
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ============================================================================
// TAKEOFFITEM.CS — Phase 3: Quantity Measurement From Blueprints
// ============================================================================
//
// WHAT A "TAKEOFF" IS:
//   Before you can price anything, you must MEASURE it.
//   An architect gives you blueprints (drawings). You measure:
//     - 9,400 SF of walls that need drywall
//     - 350 CY of concrete for the foundation
//     - 127 EA doors throughout the building
//
//   This measurement process is called "quantity takeoff" — you're
//   "taking off" quantities from the drawings.
//
// THE FLOW:
//   Blueprints → Takeoff (measure) → Estimate Line Item (price)
//   
//   TakeoffItem: "Room 204 walls = 840 SF"  (just the measurement)
//   LineItem:    "840 SF drywall at $1.79/SF = $1,503.60"  (measurement + pricing)
//
// WHY SEPARATE FROM LINE ITEMS?
//   1. One takeoff measurement can feed MULTIPLE line items:
//      840 SF of wall → drywall line item + painting line item + insulation line item
//   2. Takeoffs are organized by DRAWING SHEET, line items by CSI CODE
//   3. You can redo pricing without re-measuring
//   4. Different people may do takeoff vs pricing
//
// JERP EQUIVALENT:
//   TakeoffItem ≈ StockMovement (tracking quantities)
//   The takeoff feeds into estimates like inventory receipts feed into costing.
//
// ============================================================================

[Table("TakeoffItems")]
public class TakeoffItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // =====================================================================
    // PARENT PROJECT — Takeoffs belong to a project, shared across estimates
    // =====================================================================
    [Required]
    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    // =====================================================================
    // CSI CLASSIFICATION — What type of work is this measurement for?
    // =====================================================================
    public Guid? CSISectionId { get; set; }

    [ForeignKey(nameof(CSISectionId))]
    public CSISection? CSISection { get; set; }

    // =====================================================================
    // MEASUREMENT DATA
    // =====================================================================

    [Required]
    [MaxLength(300)]
    public string Description { get; set; } = string.Empty;
    // "Interior partition walls, 2nd Floor, Rooms 200-212"

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; } = 0;
    // The measured amount: 840 SF, 350 CY, 127 EA

    [Required]
    [MaxLength(10)]
    public string UnitOfMeasure { get; set; } = "SF";

    // =====================================================================
    // DIMENSION DETAILS — How was the quantity calculated?
    // =====================================================================
    // For area measurements (SF): Length × Height = Quantity
    // For volume measurements (CY): Length × Width × Depth ÷ 27 = Quantity
    // For linear measurements (LF): Just Length = Quantity
    // For count (EA): Just count them
    //
    // Storing the dimensions lets you AUDIT the math:
    //   "840 SF? Let me check... 60 LF wall × 14 FT height = 840 SF ✓"
    // =====================================================================

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Length { get; set; }
    // In feet. Example: 60.0 (60 linear feet of wall)

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Width { get; set; }
    // In feet. For area/volume calculations.

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Height { get; set; }
    // In feet. Example: 14.0 (14-foot ceiling height)

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Depth { get; set; }
    // In feet. For excavation and foundation work.

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Count { get; set; }
    // For repetitions: "12 rooms × 70 SF each = 840 SF"

    // =====================================================================
    // DRAWING REFERENCE — Where on the blueprints is this?
    // =====================================================================

    [MaxLength(50)]
    public string? DrawingSheet { get; set; }
    // "A-201" — Architectural sheet 201 (2nd floor plan)
    // Architects use a naming convention:
    //   A = Architectural, S = Structural, M = Mechanical,
    //   E = Electrical, P = Plumbing

    [MaxLength(200)]
    public string? Location { get; set; }
    // "2nd Floor, Rooms 200-212"
    // Where in the building this measurement applies.

    [MaxLength(50)]
    public string? GridReference { get; set; }
    // "Between grids A-D, 1-3"
    // Buildings have a grid system (like a map) for locating things.

    // =====================================================================
    // DEDUCTIONS — Subtract openings from wall areas
    // =====================================================================
    // When you measure 840 SF of wall, you need to SUBTRACT openings:
    //   840 SF total wall - 3 doors (21 SF each) - 2 windows (15 SF each)
    //   = 840 - 63 - 30 = 747 SF of actual drywall needed
    //
    // This is critical for accuracy — skip deductions and you overbuy.
    // =====================================================================

    [Column(TypeName = "decimal(18,4)")]
    public decimal DeductionQuantity { get; set; } = 0;
    // Total area of openings to subtract

    [MaxLength(300)]
    public string? DeductionNotes { get; set; }
    // "3 doors @ 3x7=21 SF each, 2 windows @ 3x5=15 SF each"

    [Column(TypeName = "decimal(18,4)")]
    public decimal NetQuantity { get; set; } = 0;
    // Quantity - DeductionQuantity = what you actually need
    // 840 - 93 = 747 SF

    // =====================================================================
    // STATUS — Has this been linked to an estimate line item?
    // =====================================================================

    public bool IsLinkedToEstimate { get; set; } = false;
    // TRUE = an estimate line item references this takeoff
    // Helps identify orphaned takeoffs (measured but not priced)

    [MaxLength(500)]
    public string? Notes { get; set; }

    // AUDIT
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }
}
