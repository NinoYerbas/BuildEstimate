/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

namespace BuildEstimate.Application.DTOs;

// =====================================================================
// TAKEOFF DTOs
// =====================================================================

public class TakeoffItemDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid? CSISectionId { get; set; }
    public string? CSICode { get; set; }
    public string? CSISectionName { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;

    // Dimensions
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? Depth { get; set; }
    public decimal? Count { get; set; }

    // Drawing reference
    public string? DrawingSheet { get; set; }
    public string? Location { get; set; }
    public string? GridReference { get; set; }

    // Deductions
    public decimal DeductionQuantity { get; set; }
    public string? DeductionNotes { get; set; }
    public decimal NetQuantity { get; set; }

    public bool IsLinkedToEstimate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTakeoffItemRequest
{
    public Guid ProjectId { get; set; }
    public Guid? CSISectionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = "SF";

    // User can enter quantity directly OR provide dimensions
    public decimal? Quantity { get; set; }

    // Dimensions — system calculates quantity from these
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? Depth { get; set; }
    public decimal? Count { get; set; }

    public string? DrawingSheet { get; set; }
    public string? Location { get; set; }
    public string? GridReference { get; set; }

    public decimal DeductionQuantity { get; set; } = 0;
    public string? DeductionNotes { get; set; }
    public string? Notes { get; set; }
}

public class UpdateTakeoffItemRequest
{
    public Guid? CSISectionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = "SF";

    public decimal? Quantity { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? Depth { get; set; }
    public decimal? Count { get; set; }

    public string? DrawingSheet { get; set; }
    public string? Location { get; set; }
    public string? GridReference { get; set; }

    public decimal DeductionQuantity { get; set; } = 0;
    public string? DeductionNotes { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Summary of all takeoff items for a project, grouped by drawing sheet.
/// Shows what's been measured and what's still unlinked to estimates.
/// </summary>
public class TakeoffSummaryDto
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int LinkedItems { get; set; }
    public int UnlinkedItems { get; set; }
    public List<DrawingSheetSummary> ByDrawingSheet { get; set; } = new();
    public List<CSISummary> ByCSISection { get; set; } = new();
}

public class DrawingSheetSummary
{
    public string DrawingSheet { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int LinkedCount { get; set; }
}

public class CSISummary
{
    public string CSICode { get; set; } = string.Empty;
    public string CSISectionName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
}
