/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// ESTIMATE DTOs — The API shapes for estimate data
// ============================================================================

namespace BuildEstimate.Application.DTOs;

// =====================================================================
// ESTIMATE DTO — What the API returns
// =====================================================================

public class EstimateDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Cost breakdown
    public decimal MaterialTotal { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal EquipmentTotal { get; set; }
    public decimal SubcontractorTotal { get; set; }
    public decimal DirectCost { get; set; }

    // Markups
    public decimal OverheadPercent { get; set; }
    public decimal OverheadAmount { get; set; }
    public decimal ProfitPercent { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal BondPercent { get; set; }
    public decimal BondAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ContingencyPercent { get; set; }
    public decimal ContingencyAmount { get; set; }

    // Final number
    public decimal TotalBidPrice { get; set; }
    public decimal? CostPerSquareFoot { get; set; }

    // Meta
    public bool IsSubmitted { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public int LineItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastCalculatedAt { get; set; }
}

// =====================================================================
// CREATE / UPDATE REQUESTS
// =====================================================================

public class CreateEstimateRequest
{
    public Guid ProjectId { get; set; }
    public string Version { get; set; } = "v1.0";
    public string? Description { get; set; }
    public decimal OverheadPercent { get; set; } = 10.00m;
    public decimal ProfitPercent { get; set; } = 10.00m;
    public decimal BondPercent { get; set; } = 0;
    public decimal TaxPercent { get; set; } = 0;
    public decimal ContingencyPercent { get; set; } = 5.00m;
}

public class UpdateEstimateMarkupsRequest
{
    // Only the markups can be updated directly.
    // Cost totals are CALCULATED from line items — never set manually.
    public decimal OverheadPercent { get; set; }
    public decimal ProfitPercent { get; set; }
    public decimal BondPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal ContingencyPercent { get; set; }
    public string? Description { get; set; }
}

// =====================================================================
// LINE ITEM DTO — Individual cost line
// =====================================================================

public class EstimateLineItemDto
{
    public Guid Id { get; set; }
    public Guid EstimateId { get; set; }
    public Guid CSISectionId { get; set; }

    // From the CSI join
    public string CSICode { get; set; } = string.Empty;
    public string CSISectionName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal WasteFactor { get; set; }
    public decimal AdjustedQuantity { get; set; }

    // Cost breakdown
    public decimal MaterialUnitCost { get; set; }
    public decimal MaterialTotal { get; set; }
    public decimal LaborHoursPerUnit { get; set; }
    public decimal LaborHours { get; set; }
    public decimal LaborRate { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal EquipmentTotal { get; set; }
    public decimal SubcontractorTotal { get; set; }
    public decimal LineTotal { get; set; }

    public string? TakeoffSource { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}

// =====================================================================
// CREATE / UPDATE LINE ITEM
// =====================================================================

public class CreateLineItemRequest
{
    public Guid CSISectionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = "SF";
    public decimal WasteFactor { get; set; } = 1.00m;

    // The user enters the unit costs — the system calculates the totals
    public decimal MaterialUnitCost { get; set; } = 0;
    public decimal LaborHoursPerUnit { get; set; } = 0;
    public decimal LaborRate { get; set; } = 0;
    public decimal EquipmentTotal { get; set; } = 0;
    public decimal SubcontractorTotal { get; set; } = 0;

    public string? TakeoffSource { get; set; }
    public string? Notes { get; set; }
}

public class UpdateLineItemRequest
{
    public Guid CSISectionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = "SF";
    public decimal WasteFactor { get; set; } = 1.00m;

    public decimal MaterialUnitCost { get; set; } = 0;
    public decimal LaborHoursPerUnit { get; set; } = 0;
    public decimal LaborRate { get; set; } = 0;
    public decimal EquipmentTotal { get; set; } = 0;
    public decimal SubcontractorTotal { get; set; } = 0;

    public string? TakeoffSource { get; set; }
    public string? Notes { get; set; }
}

// =====================================================================
// ESTIMATE SUMMARY — For the cost breakdown view
// =====================================================================

public class EstimateCostBreakdownDto
{
    public Guid EstimateId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;

    // The breakdown by CSI division
    public List<DivisionCostSummary> DivisionBreakdown { get; set; } = new();

    // Grand totals
    public decimal MaterialTotal { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal EquipmentTotal { get; set; }
    public decimal SubcontractorTotal { get; set; }
    public decimal DirectCost { get; set; }
    public decimal TotalBidPrice { get; set; }
}

public class DivisionCostSummary
{
    public string DivisionCode { get; set; } = string.Empty;
    public string DivisionName { get; set; } = string.Empty;
    public decimal MaterialTotal { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal EquipmentTotal { get; set; }
    public decimal SubcontractorTotal { get; set; }
    public decimal Total { get; set; }
    public decimal PercentOfTotal { get; set; }
    public int LineItemCount { get; set; }
}
