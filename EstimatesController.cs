/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// TAKEOFF CONTROLLER — Measuring Blueprints
// ============================================================================
//
// THE REAL-WORLD PROCESS:
//   1. Architect delivers blueprints (PDF or printed drawings)
//   2. Estimator opens Sheet A-201 (2nd floor plan)
//   3. Measures: "Room 204 walls: 60 LF × 14 FT = 840 SF"
//   4. Subtracts openings: "3 doors + 2 windows = -93 SF"
//   5. Net quantity: 747 SF of drywall needed
//   6. This feeds into the estimate: "747 SF × $1.79/SF = $1,337.13"
//
// THE SMART CALCULATION:
//   User can EITHER enter a quantity directly (840 SF)
//   OR enter dimensions (Length=60, Height=14) and the system calculates.
//
//   For area (SF):    Length × Height (or Length × Width)
//   For volume (CY):  Length × Width × Depth ÷ 27 (27 cubic feet per yard)
//   For linear (LF):  Just Length
//   For count (EA):   Just Count
//
//   If Count is provided, it multiplies: Count × calculated area
//   Example: 12 rooms × 70 SF each = 840 SF
//
// JERP EQUIVALENT: StockMovementsController (tracking quantities of items)
//
// ============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BuildEstimate.Application.DTOs;
using BuildEstimate.Core.Entities;
using BuildEstimate.Infrastructure.Data;

namespace BuildEstimate.Api.Controllers;

/// <summary>
/// Manages construction takeoff items — field measurements taken from blueprints.
/// Takeoff items record raw measurements (e.g., 840 SF of wall) plus deductions
/// (openings for doors and windows) to produce a net quantity ready for pricing.
///
/// The key method, <see cref="LinkToEstimate"/>, converts a measured takeoff item
/// into a fully priced EstimateLineItem and recalculates the estimate's bid price.
/// </summary>
[Route("api/v1/takeoff")]
[AllowAnonymous]
public class TakeoffController : BaseApiController
{
    private readonly BuildEstimateDbContext _context;
    private readonly ILogger<TakeoffController> _logger;

    /// <summary>
    /// Constructs the controller with injected database context and logger.
    /// </summary>
    public TakeoffController(
        BuildEstimateDbContext context,
        ILogger<TakeoffController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =====================================================================
    // GET /api/v1/takeoff?projectId={id} — All takeoff items for a project
    // =====================================================================

    /// <summary>
    /// Returns all takeoff items for a project, with optional filters for drawing sheet
    /// and unlinked-only items. Results are sorted by drawing sheet then location.
    /// </summary>
    /// <param name="projectId">The project whose takeoff items to list.</param>
    /// <param name="drawingSheet">Optional filter — only return items from this drawing sheet.</param>
    /// <param name="unlinkedOnly">If true, only return items not yet linked to an estimate.</param>
    [HttpGet]
    public async Task<IActionResult> GetTakeoffItems(
        [FromQuery] Guid projectId,
        [FromQuery] string? drawingSheet = null,
        [FromQuery] bool? unlinkedOnly = false)
    {
        var query = _context.TakeoffItems
            .Where(t => t.ProjectId == projectId)
            .Include(t => t.CSISection)
            .AsQueryable();

        if (!string.IsNullOrEmpty(drawingSheet))
            query = query.Where(t => t.DrawingSheet == drawingSheet);

        if (unlinkedOnly == true)
            query = query.Where(t => !t.IsLinkedToEstimate);

        var items = await query
            .OrderBy(t => t.DrawingSheet)
            .ThenBy(t => t.Location)
            .Select(t => new TakeoffItemDto
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                ProjectName = t.Project != null ? t.Project.Name : "",
                CSISectionId = t.CSISectionId,
                CSICode = t.CSISection != null ? t.CSISection.Code : null,
                CSISectionName = t.CSISection != null ? t.CSISection.Name : null,
                Description = t.Description,
                Quantity = t.Quantity,
                UnitOfMeasure = t.UnitOfMeasure,
                Length = t.Length,
                Width = t.Width,
                Height = t.Height,
                Depth = t.Depth,
                Count = t.Count,
                DrawingSheet = t.DrawingSheet,
                Location = t.Location,
                GridReference = t.GridReference,
                DeductionQuantity = t.DeductionQuantity,
                DeductionNotes = t.DeductionNotes,
                NetQuantity = t.NetQuantity,
                IsLinkedToEstimate = t.IsLinkedToEstimate,
                Notes = t.Notes,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    // =====================================================================
    // GET /api/v1/takeoff/{id} — Single takeoff item
    // =====================================================================

    /// <summary>
    /// Returns a single takeoff item by its ID, including CSI section and project details.
    /// Returns HTTP 404 if no item with the given ID exists.
    /// </summary>
    /// <param name="id">The takeoff item ID to retrieve.</param>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTakeoffItem(Guid id)
    {
        var item = await _context.TakeoffItems
            .Where(t => t.Id == id)
            .Include(t => t.CSISection)
            .Include(t => t.Project)
            .Select(t => new TakeoffItemDto
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                ProjectName = t.Project != null ? t.Project.Name : "",
                CSISectionId = t.CSISectionId,
                CSICode = t.CSISection != null ? t.CSISection.Code : null,
                CSISectionName = t.CSISection != null ? t.CSISection.Name : null,
                Description = t.Description,
                Quantity = t.Quantity,
                UnitOfMeasure = t.UnitOfMeasure,
                Length = t.Length,
                Width = t.Width,
                Height = t.Height,
                Depth = t.Depth,
                Count = t.Count,
                DrawingSheet = t.DrawingSheet,
                Location = t.Location,
                GridReference = t.GridReference,
                DeductionQuantity = t.DeductionQuantity,
                DeductionNotes = t.DeductionNotes,
                NetQuantity = t.NetQuantity,
                IsLinkedToEstimate = t.IsLinkedToEstimate,
                Notes = t.Notes,
                CreatedAt = t.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound($"Takeoff item with ID {id} not found");

        return Ok(item);
    }

    // =====================================================================
    // POST /api/v1/takeoff — Create a takeoff item
    // =====================================================================
    //
    // THE SMART CALCULATION ENGINE:
    //   If user provides dimensions → calculate Quantity automatically
    //   If user provides Quantity directly → use it as-is
    //   Then subtract deductions → NetQuantity
    //
    //   UOM determines the formula:
    //   SF:  Length × Height (or Length × Width) × Count
    //   CY:  Length × Width × Depth ÷ 27 × Count
    //   LF:  Length × Count
    //   EA:  Count (or just Quantity)
    //
    // =====================================================================

    /// <summary>
    /// Creates a new takeoff item for a project.
    /// If dimensions (Length, Width, Height, Depth) are provided instead of a direct quantity,
    /// the system automatically calculates the quantity based on the UnitOfMeasure.
    /// Subtracts DeductionQuantity from the gross to produce NetQuantity.
    /// </summary>
    /// <param name="request">All fields needed to create the takeoff item including dimensions or direct quantity.</param>
    [HttpPost]
    public async Task<IActionResult> CreateTakeoffItem([FromBody] CreateTakeoffItemRequest request)
    {
        // Verify project exists
        var project = await _context.Projects.FindAsync(request.ProjectId);
        if (project == null)
            return BadRequest($"Project with ID {request.ProjectId} not found");

        // Verify CSI section if provided
        if (request.CSISectionId.HasValue)
        {
            var sectionExists = await _context.CSISections
                .AnyAsync(s => s.Id == request.CSISectionId.Value);
            if (!sectionExists)
                return BadRequest($"CSI Section with ID {request.CSISectionId} not found");
        }

        // Calculate quantity from dimensions or use direct input
        decimal calculatedQty = CalculateQuantity(
            request.UnitOfMeasure,
            request.Quantity,
            request.Length,
            request.Width,
            request.Height,
            request.Depth,
            request.Count);

        decimal netQty = calculatedQty - request.DeductionQuantity;
        if (netQty < 0) netQty = 0;

        var item = new TakeoffItem
        {
            ProjectId = request.ProjectId,
            CSISectionId = request.CSISectionId,
            Description = request.Description.Trim(),
            Quantity = Math.Round(calculatedQty, 4),
            UnitOfMeasure = request.UnitOfMeasure.Trim().ToUpper(),
            Length = request.Length,
            Width = request.Width,
            Height = request.Height,
            Depth = request.Depth,
            Count = request.Count,
            DrawingSheet = request.DrawingSheet?.Trim(),
            Location = request.Location?.Trim(),
            GridReference = request.GridReference?.Trim(),
            DeductionQuantity = request.DeductionQuantity,
            DeductionNotes = request.DeductionNotes?.Trim(),
            NetQuantity = Math.Round(netQty, 4),
            Notes = request.Notes?.Trim(),
            CreatedBy = GetCurrentUsername()
        };

        _context.TakeoffItems.Add(item);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created takeoff: {Description} = {NetQty} {UOM} on sheet {Sheet}",
            item.Description, item.NetQuantity, item.UnitOfMeasure, item.DrawingSheet);

        return Created(new TakeoffItemDto
        {
            Id = item.Id,
            ProjectId = item.ProjectId,
            ProjectName = project.Name,
            CSISectionId = item.CSISectionId,
            Description = item.Description,
            Quantity = item.Quantity,
            UnitOfMeasure = item.UnitOfMeasure,
            Length = item.Length,
            Width = item.Width,
            Height = item.Height,
            Depth = item.Depth,
            Count = item.Count,
            DrawingSheet = item.DrawingSheet,
            Location = item.Location,
            GridReference = item.GridReference,
            DeductionQuantity = item.DeductionQuantity,
            DeductionNotes = item.DeductionNotes,
            NetQuantity = item.NetQuantity,
            IsLinkedToEstimate = false,
            Notes = item.Notes,
            CreatedAt = item.CreatedAt
        });
    }

    // =====================================================================
    // PUT /api/v1/takeoff/{id} — Update a takeoff item
    // =====================================================================

    /// <summary>
    /// Updates all fields of an existing takeoff item and recalculates its quantity.
    /// Behaves identically to CreateTakeoffItem but updates an existing record.
    /// </summary>
    /// <param name="id">The takeoff item ID to update.</param>
    /// <param name="request">Updated field values.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTakeoffItem(Guid id, [FromBody] UpdateTakeoffItemRequest request)
    {
        var item = await _context.TakeoffItems.FindAsync(id);
        if (item == null)
            return NotFound($"Takeoff item with ID {id} not found");

        decimal calculatedQty = CalculateQuantity(
            request.UnitOfMeasure,
            request.Quantity,
            request.Length,
            request.Width,
            request.Height,
            request.Depth,
            request.Count);

        decimal netQty = calculatedQty - request.DeductionQuantity;
        if (netQty < 0) netQty = 0;

        item.CSISectionId = request.CSISectionId;
        item.Description = request.Description.Trim();
        item.Quantity = Math.Round(calculatedQty, 4);
        item.UnitOfMeasure = request.UnitOfMeasure.Trim().ToUpper();
        item.Length = request.Length;
        item.Width = request.Width;
        item.Height = request.Height;
        item.Depth = request.Depth;
        item.Count = request.Count;
        item.DrawingSheet = request.DrawingSheet?.Trim();
        item.Location = request.Location?.Trim();
        item.GridReference = request.GridReference?.Trim();
        item.DeductionQuantity = request.DeductionQuantity;
        item.DeductionNotes = request.DeductionNotes?.Trim();
        item.NetQuantity = Math.Round(netQty, 4);
        item.Notes = request.Notes?.Trim();

        await _context.SaveChangesAsync();

        return Ok(new TakeoffItemDto
        {
            Id = item.Id,
            ProjectId = item.ProjectId,
            CSISectionId = item.CSISectionId,
            Description = item.Description,
            Quantity = item.Quantity,
            UnitOfMeasure = item.UnitOfMeasure,
            Length = item.Length,
            Width = item.Width,
            Height = item.Height,
            Depth = item.Depth,
            Count = item.Count,
            DrawingSheet = item.DrawingSheet,
            Location = item.Location,
            GridReference = item.GridReference,
            DeductionQuantity = item.DeductionQuantity,
            DeductionNotes = item.DeductionNotes,
            NetQuantity = item.NetQuantity,
            IsLinkedToEstimate = item.IsLinkedToEstimate,
            Notes = item.Notes,
            CreatedAt = item.CreatedAt
        });
    }

    // =====================================================================
    // DELETE /api/v1/takeoff/{id}
    // =====================================================================

    /// <summary>
    /// Deletes a takeoff item. Refuses to delete items that are linked to an estimate
    /// because deleting them would leave orphaned line items.
    /// Unlink the item from the estimate first if deletion is needed.
    /// </summary>
    /// <param name="id">The takeoff item ID to delete.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTakeoffItem(Guid id)
    {
        var item = await _context.TakeoffItems.FindAsync(id);
        if (item == null)
            return NotFound($"Takeoff item with ID {id} not found");

        if (item.IsLinkedToEstimate)
            return BadRequest("Cannot delete a takeoff item linked to an estimate. Unlink it first.");

        _context.TakeoffItems.Remove(item);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Takeoff item '{item.Description}' deleted" });
    }

    // =====================================================================
    // POST /api/v1/takeoff/{id}/link-to-estimate — Create line item from takeoff
    // =====================================================================
    //
    // THE KEY FEATURE: Take a measurement and turn it into a priced line item.
    //
    // User measured 747 SF net of drywall on Sheet A-201.
    // Now they click "Link to Estimate" and provide pricing:
    //   Material: $0.52/SF, Labor: 0.017 hrs/SF × $65/hr
    // The system creates the EstimateLineItem with:
    //   Quantity = 747 (from takeoff NetQuantity)
    //   + all pricing = LineTotal
    // And marks the takeoff as linked.
    // =====================================================================

    /// <summary>
    /// Links a takeoff item to an estimate by creating a priced EstimateLineItem from it.
    /// Uses the takeoff's NetQuantity (after deductions) as the line item quantity.
    /// After creating the line item, recalculates the estimate's total bid price.
    /// Marks the takeoff item as linked so it doesn't get added twice.
    /// </summary>
    /// <param name="id">The takeoff item to link.</param>
    /// <param name="request">The target estimate, pricing data, and optional overrides.</param>
    [HttpPost("{id}/link-to-estimate")]
    public async Task<IActionResult> LinkToEstimate(
        Guid id,
        [FromBody] LinkTakeoffToEstimateRequest request)
    {
        var takeoff = await _context.TakeoffItems
            .Include(t => t.CSISection)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (takeoff == null)
            return NotFound($"Takeoff item with ID {id} not found");

        var estimate = await _context.Estimates
            .Include(e => e.LineItems)
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == request.EstimateId);

        if (estimate == null)
            return BadRequest($"Estimate with ID {request.EstimateId} not found");

        if (estimate.IsSubmitted)
            return BadRequest("Cannot add items to a submitted estimate");

        // Use takeoff's CSI section, or override
        var csiSectionId = request.CSISectionId ?? takeoff.CSISectionId;
        if (csiSectionId == null)
            return BadRequest("CSI Section is required. Set it on the takeoff or in this request.");

        // Use the NET quantity from the takeoff (after deductions)
        var quantity = takeoff.NetQuantity > 0 ? takeoff.NetQuantity : takeoff.Quantity;

        // Calculate line item costs
        var adjustedQty = quantity * request.WasteFactor;
        var materialTotal = adjustedQty * request.MaterialUnitCost;
        var laborHours = adjustedQty * request.LaborHoursPerUnit;
        var laborTotal = laborHours * request.LaborRate;
        var lineTotal = materialTotal + laborTotal
                      + request.EquipmentTotal + request.SubcontractorTotal;

        var lineItem = new EstimateLineItem
        {
            EstimateId = request.EstimateId,
            CSISectionId = csiSectionId.Value,
            Description = request.Description ?? takeoff.Description,
            Quantity = quantity,
            UnitOfMeasure = takeoff.UnitOfMeasure,
            WasteFactor = request.WasteFactor,
            AdjustedQuantity = Math.Round(adjustedQty, 4),
            MaterialUnitCost = request.MaterialUnitCost,
            MaterialTotal = Math.Round(materialTotal, 2),
            LaborHoursPerUnit = request.LaborHoursPerUnit,
            LaborHours = Math.Round(laborHours, 2),
            LaborRate = request.LaborRate,
            LaborTotal = Math.Round(laborTotal, 2),
            EquipmentTotal = request.EquipmentTotal,
            SubcontractorTotal = request.SubcontractorTotal,
            LineTotal = Math.Round(lineTotal, 2),
            TakeoffItemId = takeoff.Id,
            TakeoffSource = $"Sheet {takeoff.DrawingSheet}, {takeoff.Location}",
            Notes = request.Notes,
            SortOrder = estimate.LineItems.Count + 1
        };

        _context.EstimateLineItems.Add(lineItem);
        estimate.LineItems.Add(lineItem);

        // Mark takeoff as linked
        takeoff.IsLinkedToEstimate = true;

        // Recalculate estimate totals
        RecalculateEstimateTotals(estimate);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Linked takeoff {TakeoffId} to estimate {EstimateId}: {Qty} {UOM} = {LineTotal:C}",
            takeoff.Id, estimate.Id, quantity, takeoff.UnitOfMeasure, lineItem.LineTotal);

        return Created(new
        {
            LineItem = new EstimateLineItemDto
            {
                Id = lineItem.Id,
                EstimateId = lineItem.EstimateId,
                CSISectionId = lineItem.CSISectionId,
                CSICode = takeoff.CSISection?.Code ?? "",
                CSISectionName = takeoff.CSISection?.Name ?? "",
                Description = lineItem.Description,
                Quantity = lineItem.Quantity,
                UnitOfMeasure = lineItem.UnitOfMeasure,
                WasteFactor = lineItem.WasteFactor,
                AdjustedQuantity = lineItem.AdjustedQuantity,
                MaterialUnitCost = lineItem.MaterialUnitCost,
                MaterialTotal = lineItem.MaterialTotal,
                LaborHoursPerUnit = lineItem.LaborHoursPerUnit,
                LaborHours = lineItem.LaborHours,
                LaborRate = lineItem.LaborRate,
                LaborTotal = lineItem.LaborTotal,
                EquipmentTotal = lineItem.EquipmentTotal,
                SubcontractorTotal = lineItem.SubcontractorTotal,
                LineTotal = lineItem.LineTotal,
                TakeoffSource = lineItem.TakeoffSource,
                Notes = lineItem.Notes,
                SortOrder = lineItem.SortOrder
            },
            message = $"Takeoff linked: {quantity} {takeoff.UnitOfMeasure} → ${lineItem.LineTotal:N2}"
        });
    }

    // =====================================================================
    // GET /api/v1/takeoff/summary?projectId={id} — Project takeoff summary
    // =====================================================================

    /// <summary>
    /// Returns a summary of all takeoff items for a project, grouped by drawing sheet
    /// and by CSI section. Shows totals for linked vs. unlinked items so the estimator
    /// can see how much of the takeoff has been priced.
    /// </summary>
    /// <param name="projectId">The project to summarize.</param>
    [HttpGet("summary")]
    public async Task<IActionResult> GetTakeoffSummary([FromQuery] Guid projectId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null)
            return NotFound($"Project with ID {projectId} not found");

        var items = await _context.TakeoffItems
            .Where(t => t.ProjectId == projectId)
            .Include(t => t.CSISection)
            .ToListAsync();

        var bySheet = items
            .GroupBy(t => t.DrawingSheet ?? "Unassigned")
            .Select(g => new DrawingSheetSummary
            {
                DrawingSheet = g.Key,
                ItemCount = g.Count(),
                LinkedCount = g.Count(t => t.IsLinkedToEstimate)
            })
            .OrderBy(s => s.DrawingSheet)
            .ToList();

        var byCSI = items
            .Where(t => t.CSISection != null)
            .GroupBy(t => new { t.CSISection!.Code, t.CSISection!.Name, t.UnitOfMeasure })
            .Select(g => new CSISummary
            {
                CSICode = g.Key.Code,
                CSISectionName = g.Key.Name,
                ItemCount = g.Count(),
                TotalQuantity = g.Sum(t => t.NetQuantity),
                UnitOfMeasure = g.Key.UnitOfMeasure
            })
            .OrderBy(s => s.CSICode)
            .ToList();

        return Ok(new TakeoffSummaryDto
        {
            ProjectId = projectId,
            ProjectName = project.Name,
            TotalItems = items.Count,
            LinkedItems = items.Count(t => t.IsLinkedToEstimate),
            UnlinkedItems = items.Count(t => !t.IsLinkedToEstimate),
            ByDrawingSheet = bySheet,
            ByCSISection = byCSI
        });
    }

    // =====================================================================
    // QUANTITY CALCULATION ENGINE
    // =====================================================================
    //
    // Calculates quantity from dimensions based on unit of measure:
    //   SF → area (Length × Height or Length × Width)
    //   CY → volume (Length × Width × Depth ÷ 27)
    //   LF → linear (Length)
    //   EA → count (Count)
    //   LS → 1 (lump sum is always 1)
    //
    // If Count is provided, multiplies the calculated unit by Count.
    //   12 rooms × 70 SF each = 840 SF
    // =====================================================================

    /// <summary>
    /// Calculates a takeoff quantity from dimensions based on the unit of measure.
    /// If a direct quantity is provided, it is returned as-is without any calculation.
    /// If Count is provided along with dimensions, the result is multiplied by Count.
    /// </summary>
    /// <param name="uom">Unit of measure code: "SF", "CY", "CF", "LF", "EA", "LS", or "SQ".</param>
    /// <param name="directQuantity">A pre-calculated quantity entered by the user. If provided, all dimension params are ignored.</param>
    /// <param name="length">Measured length in feet.</param>
    /// <param name="width">Measured width in feet.</param>
    /// <param name="height">Measured height in feet (used for SF wall calculations).</param>
    /// <param name="depth">Measured depth in feet (used for CY concrete calculations).</param>
    /// <param name="count">Number of identical items (e.g., 12 identical rooms). Multiplies the calculated unit quantity.</param>
    /// <returns>The calculated quantity, rounded to 4 decimal places.</returns>
    private static decimal CalculateQuantity(
        string uom,
        decimal? directQuantity,
        decimal? length,
        decimal? width,
        decimal? height,
        decimal? depth,
        decimal? count)
    {
        // If user provided a direct quantity, use it
        if (directQuantity.HasValue && directQuantity.Value > 0)
            return directQuantity.Value;

        // Otherwise calculate from dimensions
        decimal qty = 0;
        decimal multiplier = count ?? 1;

        switch (uom.Trim().ToUpper())
        {
            case "SF": // Square Feet = Length × Height (walls) or Length × Width (floors)
                if (length > 0 && height > 0)
                    qty = length.Value * height.Value;
                else if (length > 0 && width > 0)
                    qty = length.Value * width.Value;
                break;

            case "CY": // Cubic Yards = L × W × D ÷ 27
                if (length > 0 && width > 0 && depth > 0)
                    qty = (length.Value * width.Value * depth.Value) / 27m;
                break;

            case "CF": // Cubic Feet = L × W × D (or L × W × H)
                if (length > 0 && width > 0)
                    qty = length.Value * width.Value * (depth ?? height ?? 1);
                break;

            case "LF": // Linear Feet = just Length
                if (length > 0)
                    qty = length.Value;
                break;

            case "EA": // Each = Count
                qty = multiplier;
                multiplier = 1; // Don't multiply again
                break;

            case "LS": // Lump Sum = always 1
                return 1;

            case "SQ": // Roofing Squares = SF ÷ 100
                if (length > 0 && width > 0)
                    qty = (length.Value * width.Value) / 100m;
                break;

            default: // Unknown UOM — need direct quantity
                return directQuantity ?? 0;
        }

        return qty * multiplier;
    }

    // =====================================================================
    // ESTIMATE RECALCULATION (same as in EstimatesController)
    // =====================================================================

    /// <summary>
    /// Recalculates all estimate totals after a line item is added via LinkToEstimate.
    /// Same calculation engine as EstimatesController.RecalculateEstimateTotals.
    /// </summary>
    private void RecalculateEstimateTotals(Estimate estimate)
    {
        estimate.MaterialTotal = estimate.LineItems.Sum(li => li.MaterialTotal);
        estimate.LaborTotal = estimate.LineItems.Sum(li => li.LaborTotal);
        estimate.EquipmentTotal = estimate.LineItems.Sum(li => li.EquipmentTotal);
        estimate.SubcontractorTotal = estimate.LineItems.Sum(li => li.SubcontractorTotal);

        estimate.DirectCost = estimate.MaterialTotal + estimate.LaborTotal
                            + estimate.EquipmentTotal + estimate.SubcontractorTotal;

        estimate.OverheadAmount = Math.Round(
            estimate.DirectCost * (estimate.OverheadPercent / 100m), 2);

        var costPlusOverhead = estimate.DirectCost + estimate.OverheadAmount;
        estimate.ProfitAmount = Math.Round(
            costPlusOverhead * (estimate.ProfitPercent / 100m), 2);

        var subtotal = estimate.DirectCost + estimate.OverheadAmount + estimate.ProfitAmount;

        estimate.BondAmount = Math.Round(
            subtotal * (estimate.BondPercent / 100m), 2);
        estimate.TaxAmount = Math.Round(
            estimate.MaterialTotal * (estimate.TaxPercent / 100m), 2);
        estimate.ContingencyAmount = Math.Round(
            estimate.DirectCost * (estimate.ContingencyPercent / 100m), 2);

        estimate.TotalBidPrice = subtotal + estimate.BondAmount
                               + estimate.TaxAmount + estimate.ContingencyAmount;

        if (estimate.Project?.GrossSquareFootage > 0)
        {
            estimate.CostPerSquareFoot = Math.Round(
                estimate.TotalBidPrice / estimate.Project.GrossSquareFootage.Value, 2);
        }

        estimate.LastCalculatedAt = DateTime.UtcNow;

        if (estimate.Project != null)
            estimate.Project.BidAmount = estimate.TotalBidPrice;
    }
}

// =====================================================================
// REQUEST DTO for linking takeoff to estimate
// =====================================================================

/// <summary>
/// Data the API receives when linking a takeoff measurement to an estimate.
/// Includes pricing data (material cost, labor hours/rate) needed to create
/// the EstimateLineItem from the takeoff's measured quantity.
/// </summary>
public class LinkTakeoffToEstimateRequest
{
    /// <summary>The estimate to add the new line item to.</summary>
    public Guid EstimateId { get; set; }

    /// <summary>Override the takeoff's CSI section if the work belongs under a different code.</summary>
    public Guid? CSISectionId { get; set; }  // Override takeoff's CSI if needed

    /// <summary>Override the takeoff's description with a more specific line item description.</summary>
    public string? Description { get; set; }  // Override takeoff description

    /// <summary>Waste factor multiplier. 1.00 = no waste, 1.10 = add 10%. Defaults to 1.00.</summary>
    public decimal WasteFactor { get; set; } = 1.00m;

    /// <summary>Material cost per unit (e.g., cost per SF of drywall panels).</summary>
    public decimal MaterialUnitCost { get; set; } = 0;

    /// <summary>Labor hours per unit (from production rate data).</summary>
    public decimal LaborHoursPerUnit { get; set; } = 0;

    /// <summary>All-in hourly labor rate (from labor rate data for this trade/location).</summary>
    public decimal LaborRate { get; set; } = 0;

    /// <summary>Lump-sum equipment cost for this line item.</summary>
    public decimal EquipmentTotal { get; set; } = 0;

    /// <summary>Lump-sum subcontractor cost for this line item.</summary>
    public decimal SubcontractorTotal { get; set; } = 0;

    /// <summary>Optional notes or clarifications about the pricing assumptions used.</summary>
    public string? Notes { get; set; }
}
