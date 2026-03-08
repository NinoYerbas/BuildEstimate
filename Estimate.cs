/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// ESTIMATE DTOs — The API shapes for estimate data
// ============================================================================
//
// This file defines all the Data Transfer Objects (DTOs) for the estimating
// workflow — the core of the construction estimating system.
//
// AN ESTIMATE CONTAINS:
//   - Header: project reference, version, markup percentages
//   - Line Items: each CSI-coded cost item (material + labor + equipment)
//   - Totals: calculated from line items, then marked up for overhead/profit/bond/tax
//
// THE CALCULATION FLOW (how a bid price is assembled):
//   1. User adds line items (each has material, labor, equipment costs)
//   2. System sums them → DirectCost
//   3. Applies markups:
//        Overhead = DirectCost × OverheadPercent
//        Profit   = (DirectCost + Overhead) × ProfitPercent
//        Bond     = (DirectCost + Overhead + Profit) × BondPercent
//        Tax      = MaterialTotal × TaxPercent
//        Contingency = DirectCost × ContingencyPercent
//   4. TotalBidPrice = DirectCost + all markups
//   5. CostPerSF = TotalBidPrice ÷ project square footage
//
// ============================================================================

namespace BuildEstimate.Application.DTOs;

// =====================================================================
// ESTIMATE DTO — What the API returns
// =====================================================================

/// <summary>
/// The full estimate record as returned by the API.
/// Contains all header data, calculated cost totals, markup amounts, and the final bid price.
/// Read-only — cost totals are never set directly; they are calculated from line items.
/// </summary>
public class EstimateDto
{
    /// <summary>Unique identifier for this estimate.</summary>
    public Guid Id { get; set; }

    /// <summary>The project this estimate belongs to.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Project name, joined from the Project table for display purposes.</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Version label for this estimate, e.g., "v1.0", "v2.0 VE".
    /// A project may have multiple estimate versions as the scope evolves.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Optional notes about this estimate version, e.g., "Value-engineered scope reduction".</summary>
    public string? Description { get; set; }

    // Cost breakdown — these are the sums of all line items, sorted by cost type
    /// <summary>Total material cost across all line items (sum of MaterialTotal per line).</summary>
    public decimal MaterialTotal { get; set; }

    /// <summary>Total labor cost across all line items (sum of LaborTotal per line).</summary>
    public decimal LaborTotal { get; set; }

    /// <summary>Total equipment cost across all line items.</summary>
    public decimal EquipmentTotal { get; set; }

    /// <summary>Total subcontractor cost across all line items.</summary>
    public decimal SubcontractorTotal { get; set; }

    /// <summary>
    /// Direct cost = MaterialTotal + LaborTotal + EquipmentTotal + SubcontractorTotal.
    /// This is the "bare bones" cost before any markup is applied.
    /// </summary>
    public decimal DirectCost { get; set; }

    // Markups — percentages set by the user, amounts calculated by the engine
    /// <summary>Overhead percentage (e.g., 10.00 = 10%). Applied to DirectCost.</summary>
    public decimal OverheadPercent { get; set; }

    /// <summary>Dollar amount of overhead: DirectCost × (OverheadPercent / 100).</summary>
    public decimal OverheadAmount { get; set; }

    /// <summary>Profit percentage (e.g., 10.00 = 10%). Applied to DirectCost + Overhead.</summary>
    public decimal ProfitPercent { get; set; }

    /// <summary>Dollar amount of profit: (DirectCost + Overhead) × (ProfitPercent / 100).</summary>
    public decimal ProfitAmount { get; set; }

    /// <summary>Bid bond percentage. Applied to the subtotal (DirectCost + Overhead + Profit).</summary>
    public decimal BondPercent { get; set; }

    /// <summary>Dollar amount of bond fee.</summary>
    public decimal BondAmount { get; set; }

    /// <summary>Sales tax percentage. Applied to MaterialTotal only (labor is not taxed).</summary>
    public decimal TaxPercent { get; set; }

    /// <summary>Dollar amount of sales tax on materials.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Contingency percentage — a safety buffer for unexpected costs. Applied to DirectCost.</summary>
    public decimal ContingencyPercent { get; set; }

    /// <summary>Dollar amount of contingency reserve.</summary>
    public decimal ContingencyAmount { get; set; }

    // Final number
    /// <summary>
    /// The final number you submit to the project owner.
    /// TotalBidPrice = DirectCost + Overhead + Profit + Bond + Tax + Contingency
    /// </summary>
    public decimal TotalBidPrice { get; set; }

    /// <summary>
    /// TotalBidPrice divided by the project's gross square footage.
    /// Null if the project has no square footage. Used for benchmarking against similar projects.
    /// </summary>
    public decimal? CostPerSquareFoot { get; set; }

    // Meta
    /// <summary>True if this estimate has been officially submitted to the project owner. Submitted estimates are locked.</summary>
    public bool IsSubmitted { get; set; }

    /// <summary>The date and time this estimate was submitted. Null if not yet submitted.</summary>
    public DateTime? SubmittedDate { get; set; }

    /// <summary>How many line items this estimate currently has.</summary>
    public int LineItemCount { get; set; }

    /// <summary>When this estimate record was first created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When this estimate record was last modified.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>When the calculation engine last ran to update the totals. Null if never calculated (no line items).</summary>
    public DateTime? LastCalculatedAt { get; set; }
}

// =====================================================================
// CREATE / UPDATE REQUESTS
// =====================================================================

/// <summary>
/// Data the API receives when creating a new estimate.
/// The user sets the markup percentages upfront; they can be changed later via UpdateMarkups.
/// ProjectId is required — an estimate must belong to a project.
/// Cost totals start at zero and build up as line items are added.
/// </summary>
public class CreateEstimateRequest
{
    /// <summary>The project this estimate belongs to (required).</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Version label for this estimate. Defaults to "v1.0". Use "v2.0", "v3.0 VE" for subsequent revisions.</summary>
    public string Version { get; set; } = "v1.0";

    /// <summary>Optional description of what's different about this version.</summary>
    public string? Description { get; set; }

    /// <summary>General overhead percentage — covers office costs, supervision, insurance. Defaults to 10%.</summary>
    public decimal OverheadPercent { get; set; } = 10.00m;

    /// <summary>Profit margin percentage. Defaults to 10%.</summary>
    public decimal ProfitPercent { get; set; } = 10.00m;

    /// <summary>Bid bond percentage. Defaults to 0 (many projects don't require a bond).</summary>
    public decimal BondPercent { get; set; } = 0;

    /// <summary>Sales tax on materials percentage. Defaults to 0 (tax is jurisdiction-specific).</summary>
    public decimal TaxPercent { get; set; } = 0;

    /// <summary>Contingency percentage — buffer for unknown costs. Defaults to 5%.</summary>
    public decimal ContingencyPercent { get; set; } = 5.00m;
}

/// <summary>
/// Data the API receives when updating markup percentages on an existing estimate.
/// Changing any markup triggers a full recalculation of the bid price.
/// Note: Cost totals are NOT included — they are always calculated from line items, never set directly.
/// </summary>
public class UpdateEstimateMarkupsRequest
{
    // Only the markups can be updated directly.
    // Cost totals are CALCULATED from line items — never set manually.
    /// <summary>New overhead percentage to apply.</summary>
    public decimal OverheadPercent { get; set; }

    /// <summary>New profit percentage to apply.</summary>
    public decimal ProfitPercent { get; set; }

    /// <summary>New bond percentage to apply.</summary>
    public decimal BondPercent { get; set; }

    /// <summary>New sales tax percentage to apply (on materials only).</summary>
    public decimal TaxPercent { get; set; }

    /// <summary>New contingency percentage to apply.</summary>
    public decimal ContingencyPercent { get; set; }

    /// <summary>Optional update to the estimate's description/notes.</summary>
    public string? Description { get; set; }
}

// =====================================================================
// LINE ITEM DTO — Individual cost line
// =====================================================================

/// <summary>
/// A single priced line item within an estimate.
/// Each line item corresponds to one CSI code + description + quantity + unit costs.
///
/// THE MATH for a line item:
///   AdjustedQuantity = Quantity × WasteFactor
///   MaterialTotal    = AdjustedQuantity × MaterialUnitCost
///   LaborHours       = AdjustedQuantity × LaborHoursPerUnit
///   LaborTotal       = LaborHours × LaborRate
///   LineTotal        = MaterialTotal + LaborTotal + EquipmentTotal + SubcontractorTotal
/// </summary>
public class EstimateLineItemDto
{
    /// <summary>Unique identifier for this line item.</summary>
    public Guid Id { get; set; }

    /// <summary>The estimate this line item belongs to.</summary>
    public Guid EstimateId { get; set; }

    /// <summary>The CSI section this line item is filed under (e.g., "09 29 00 — Gypsum Board").</summary>
    public Guid CSISectionId { get; set; }

    // From the CSI join
    /// <summary>The CSI code, e.g., "09 29 00". Joined from the CSISection table for display.</summary>
    public string CSICode { get; set; } = string.Empty;

    /// <summary>The CSI section name, e.g., "Gypsum Board (Drywall)". Joined for display.</summary>
    public string CSISectionName { get; set; } = string.Empty;

    /// <summary>A specific description of this work item, e.g., "5/8\" Type-X drywall on metal studs — 2nd floor".</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The raw measured quantity before waste factor adjustment (e.g., 9,400 SF from the takeoff).</summary>
    public decimal Quantity { get; set; }

    /// <summary>Unit of measure for the quantity (e.g., "SF", "LF", "CY", "EA").</summary>
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>
    /// Multiplier that accounts for material waste and cutting losses.
    /// 1.10 = add 10% for waste. AdjustedQuantity = Quantity × WasteFactor.
    /// </summary>
    public decimal WasteFactor { get; set; }

    /// <summary>Quantity after applying waste factor — this is what you order and what costs are based on.</summary>
    public decimal AdjustedQuantity { get; set; }

    // Cost breakdown
    /// <summary>Material cost per unit (e.g., $0.52 per SF for drywall panels).</summary>
    public decimal MaterialUnitCost { get; set; }

    /// <summary>Total material cost: AdjustedQuantity × MaterialUnitCost.</summary>
    public decimal MaterialTotal { get; set; }

    /// <summary>Labor hours required per unit of work (from production rate data).</summary>
    public decimal LaborHoursPerUnit { get; set; }

    /// <summary>Total labor hours for this line: AdjustedQuantity × LaborHoursPerUnit.</summary>
    public decimal LaborHours { get; set; }

    /// <summary>The all-in hourly labor cost (from labor rate data for this trade/location).</summary>
    public decimal LaborRate { get; set; }

    /// <summary>Total labor cost: LaborHours × LaborRate.</summary>
    public decimal LaborTotal { get; set; }

    /// <summary>Total equipment rental or ownership cost for this line item.</summary>
    public decimal EquipmentTotal { get; set; }

    /// <summary>Total subcontractor cost if this work is being subcontracted rather than self-performed.</summary>
    public decimal SubcontractorTotal { get; set; }

    /// <summary>Total cost for this line: Material + Labor + Equipment + Subcontractor.</summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Reference to where this quantity came from (e.g., "Sheet A-201, Room 204").
    /// Links this estimate line back to the original takeoff measurement.
    /// </summary>
    public string? TakeoffSource { get; set; }

    /// <summary>Additional notes about this line item, clarifications, or assumptions made.</summary>
    public string? Notes { get; set; }

    /// <summary>Display order within the estimate — line items are shown sorted by this value.</summary>
    public int SortOrder { get; set; }
}

// =====================================================================
// CREATE / UPDATE LINE ITEM
// =====================================================================

/// <summary>
/// Data the API receives when adding a new line item to an estimate.
/// The user provides quantities and unit costs; the system calculates all totals.
/// </summary>
public class CreateLineItemRequest
{
    /// <summary>The CSI section this line item belongs to (required). Determines the division for cost breakdown.</summary>
    public Guid CSISectionId { get; set; }

    /// <summary>Description of this specific work item.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The measured quantity (from takeoff or estimate). The system multiplies this by WasteFactor.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Unit of measure for the quantity. Defaults to "SF" (square feet).</summary>
    public string UnitOfMeasure { get; set; } = "SF";

    /// <summary>
    /// Waste factor multiplier. 1.00 = no waste. 1.10 = add 10% for waste and cutting.
    /// Defaults to 1.00 (no waste). Adjust based on material type and conditions.
    /// </summary>
    public decimal WasteFactor { get; set; } = 1.00m;

    // The user enters the unit costs — the system calculates the totals
    /// <summary>Material cost per unit BEFORE applying waste factor. The system multiplies by adjusted quantity.</summary>
    public decimal MaterialUnitCost { get; set; } = 0;

    /// <summary>Labor hours required per unit of work (from production rate). Used with LaborRate to calculate LaborTotal.</summary>
    public decimal LaborHoursPerUnit { get; set; } = 0;

    /// <summary>All-in hourly labor rate (from labor rate data). Multiplied by LaborHours to get LaborTotal.</summary>
    public decimal LaborRate { get; set; } = 0;

    /// <summary>Total equipment cost for this line item as a lump sum (not per-unit).</summary>
    public decimal EquipmentTotal { get; set; } = 0;

    /// <summary>Total subcontractor cost for this line item as a lump sum.</summary>
    public decimal SubcontractorTotal { get; set; } = 0;

    /// <summary>Optional reference to the takeoff measurement source (drawing sheet and location).</summary>
    public string? TakeoffSource { get; set; }

    /// <summary>Optional notes or clarifications about this line item.</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Data the API receives when updating an existing line item.
/// Identical fields to CreateLineItemRequest — all fields are replaceable.
/// After saving, the system recalculates all estimate totals.
/// </summary>
public class UpdateLineItemRequest
{
    /// <summary>The CSI section to assign or reassign this line item to.</summary>
    public Guid CSISectionId { get; set; }

    /// <summary>Updated description of the work item.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Updated quantity.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Updated unit of measure.</summary>
    public string UnitOfMeasure { get; set; } = "SF";

    /// <summary>Updated waste factor. 1.00 = no waste.</summary>
    public decimal WasteFactor { get; set; } = 1.00m;

    /// <summary>Updated material unit cost.</summary>
    public decimal MaterialUnitCost { get; set; } = 0;

    /// <summary>Updated labor hours per unit.</summary>
    public decimal LaborHoursPerUnit { get; set; } = 0;

    /// <summary>Updated all-in hourly labor rate.</summary>
    public decimal LaborRate { get; set; } = 0;

    /// <summary>Updated total equipment cost (lump sum).</summary>
    public decimal EquipmentTotal { get; set; } = 0;

    /// <summary>Updated total subcontractor cost (lump sum).</summary>
    public decimal SubcontractorTotal { get; set; } = 0;

    /// <summary>Updated takeoff source reference.</summary>
    public string? TakeoffSource { get; set; }

    /// <summary>Updated notes.</summary>
    public string? Notes { get; set; }
}

// =====================================================================
// ESTIMATE SUMMARY — For the cost breakdown view
// =====================================================================

/// <summary>
/// A rolled-up cost breakdown of an estimate, organized by CSI division.
/// Used for the "Cost Breakdown" view that shows:
///   Division 03 - Concrete: $245,000 (18% of direct cost)
///   Division 09 - Finishes: $189,000 (14% of direct cost)
///   Division 26 - Electrical: $312,000 (23% of direct cost) ← highest
/// This is the construction equivalent of a trial balance grouped by account type.
/// </summary>
public class EstimateCostBreakdownDto
{
    /// <summary>The estimate being broken down.</summary>
    public Guid EstimateId { get; set; }

    /// <summary>Project name for display in report headers.</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>Estimate version for display, e.g., "v2.0 VE".</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// The breakdown by CSI division — one row per division that has line items.
    /// Sorted by division code (01, 02, 03, ...).
    /// </summary>
    public List<DivisionCostSummary> DivisionBreakdown { get; set; } = new();

    // Grand totals — same as EstimateDto but here for report completeness
    /// <summary>Sum of all material costs across all divisions.</summary>
    public decimal MaterialTotal { get; set; }

    /// <summary>Sum of all labor costs across all divisions.</summary>
    public decimal LaborTotal { get; set; }

    /// <summary>Sum of all equipment costs across all divisions.</summary>
    public decimal EquipmentTotal { get; set; }

    /// <summary>Sum of all subcontractor costs across all divisions.</summary>
    public decimal SubcontractorTotal { get; set; }

    /// <summary>Sum of all four cost types = direct cost before markups.</summary>
    public decimal DirectCost { get; set; }

    /// <summary>The final bid price after all markups are applied.</summary>
    public decimal TotalBidPrice { get; set; }
}

/// <summary>
/// One row in the cost breakdown — the aggregated costs for a single CSI division.
/// Includes a PercentOfTotal so the UI can show a pie chart or progress bar.
/// </summary>
public class DivisionCostSummary
{
    /// <summary>The CSI division code, e.g., "09".</summary>
    public string DivisionCode { get; set; } = string.Empty;

    /// <summary>The CSI division name, e.g., "Finishes".</summary>
    public string DivisionName { get; set; } = string.Empty;

    /// <summary>Total material cost for all line items in this division.</summary>
    public decimal MaterialTotal { get; set; }

    /// <summary>Total labor cost for all line items in this division.</summary>
    public decimal LaborTotal { get; set; }

    /// <summary>Total equipment cost for all line items in this division.</summary>
    public decimal EquipmentTotal { get; set; }

    /// <summary>Total subcontractor cost for all line items in this division.</summary>
    public decimal SubcontractorTotal { get; set; }

    /// <summary>Combined total for this division: Material + Labor + Equipment + Subcontractor.</summary>
    public decimal Total { get; set; }

    /// <summary>
    /// This division's total as a percentage of the estimate's DirectCost.
    /// Example: 14.2 means this division is 14.2% of the total direct cost.
    /// </summary>
    public decimal PercentOfTotal { get; set; }

    /// <summary>Number of individual line items in this division.</summary>
    public int LineItemCount { get; set; }
}
