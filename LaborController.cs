/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// ESTIMATES CONTROLLER — The Heart of the Estimating System
// ============================================================================
//
// This controller handles:
//   1. CRUD for Estimates (create, read, update, delete)
//   2. CRUD for Line Items within an estimate
//   3. THE CALCULATION ENGINE — recalculates totals when lines change
//   4. Cost breakdown by CSI division
//
// THE FLOW:
//   User creates an Estimate → adds Line Items → system CALCULATES →
//   user adjusts markups → system RECALCULATES → user submits bid
//
// EVERY TIME a line item changes, the estimate totals are recalculated.
// This is like JERP: every time a JournalEntry posts, Account.Balance updates.
//
// ============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BuildEstimate.Application.DTOs;
using BuildEstimate.Core.Entities;
using BuildEstimate.Infrastructure.Data;

namespace BuildEstimate.Api.Controllers;

[Route("api/v1/estimates")]
[AllowAnonymous]  // TODO: Change to [Authorize] when auth is wired
public class EstimatesController : BaseApiController
{
    private readonly BuildEstimateDbContext _context;
    private readonly ILogger<EstimatesController> _logger;

    public EstimatesController(
        BuildEstimateDbContext context,
        ILogger<EstimatesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =====================================================================
    // GET /api/v1/estimates?projectId={id} — List estimates for a project
    // =====================================================================

    [HttpGet]
    public async Task<IActionResult> GetEstimates([FromQuery] Guid? projectId = null)
    {
        var query = _context.Estimates
            .Include(e => e.Project)
            .AsQueryable();

        if (projectId.HasValue)
            query = query.Where(e => e.ProjectId == projectId.Value);

        var estimates = await query
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new EstimateDto
            {
                Id = e.Id,
                ProjectId = e.ProjectId,
                ProjectName = e.Project != null ? e.Project.Name : "",
                Version = e.Version,
                Description = e.Description,
                MaterialTotal = e.MaterialTotal,
                LaborTotal = e.LaborTotal,
                EquipmentTotal = e.EquipmentTotal,
                SubcontractorTotal = e.SubcontractorTotal,
                DirectCost = e.DirectCost,
                OverheadPercent = e.OverheadPercent,
                OverheadAmount = e.OverheadAmount,
                ProfitPercent = e.ProfitPercent,
                ProfitAmount = e.ProfitAmount,
                BondPercent = e.BondPercent,
                BondAmount = e.BondAmount,
                TaxPercent = e.TaxPercent,
                TaxAmount = e.TaxAmount,
                ContingencyPercent = e.ContingencyPercent,
                ContingencyAmount = e.ContingencyAmount,
                TotalBidPrice = e.TotalBidPrice,
                CostPerSquareFoot = e.CostPerSquareFoot,
                IsSubmitted = e.IsSubmitted,
                SubmittedDate = e.SubmittedDate,
                LineItemCount = e.LineItems.Count,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                LastCalculatedAt = e.LastCalculatedAt
            })
            .ToListAsync();

        return Ok(estimates);
    }

    // =====================================================================
    // GET /api/v1/estimates/{id} — Get one estimate with all details
    // =====================================================================

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEstimate(Guid id)
    {
        var estimate = await _context.Estimates
            .Where(e => e.Id == id)
            .Include(e => e.Project)
            .Select(e => new EstimateDto
            {
                Id = e.Id,
                ProjectId = e.ProjectId,
                ProjectName = e.Project != null ? e.Project.Name : "",
                Version = e.Version,
                Description = e.Description,
                MaterialTotal = e.MaterialTotal,
                LaborTotal = e.LaborTotal,
                EquipmentTotal = e.EquipmentTotal,
                SubcontractorTotal = e.SubcontractorTotal,
                DirectCost = e.DirectCost,
                OverheadPercent = e.OverheadPercent,
                OverheadAmount = e.OverheadAmount,
                ProfitPercent = e.ProfitPercent,
                ProfitAmount = e.ProfitAmount,
                BondPercent = e.BondPercent,
                BondAmount = e.BondAmount,
                TaxPercent = e.TaxPercent,
                TaxAmount = e.TaxAmount,
                ContingencyPercent = e.ContingencyPercent,
                ContingencyAmount = e.ContingencyAmount,
                TotalBidPrice = e.TotalBidPrice,
                CostPerSquareFoot = e.CostPerSquareFoot,
                IsSubmitted = e.IsSubmitted,
                SubmittedDate = e.SubmittedDate,
                LineItemCount = e.LineItems.Count,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                LastCalculatedAt = e.LastCalculatedAt
            })
            .FirstOrDefaultAsync();

        if (estimate == null)
            return NotFound($"Estimate with ID {id} not found");

        return Ok(estimate);
    }

    // =====================================================================
    // POST /api/v1/estimates — Create a new estimate
    // =====================================================================

    [HttpPost]
    public async Task<IActionResult> CreateEstimate([FromBody] CreateEstimateRequest request)
    {
        // Verify the project exists
        var project = await _context.Projects.FindAsync(request.ProjectId);
        if (project == null)
            return BadRequest($"Project with ID {request.ProjectId} not found");

        var estimate = new Estimate
        {
            ProjectId = request.ProjectId,
            Version = request.Version.Trim(),
            Description = request.Description?.Trim(),
            OverheadPercent = request.OverheadPercent,
            ProfitPercent = request.ProfitPercent,
            BondPercent = request.BondPercent,
            TaxPercent = request.TaxPercent,
            ContingencyPercent = request.ContingencyPercent,
            CreatedBy = GetCurrentUsername()
        };

        _context.Estimates.Add(estimate);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created estimate {EstimateId} ({Version}) for project {ProjectName}",
            estimate.Id, estimate.Version, project.Name);

        return Created(new EstimateDto
        {
            Id = estimate.Id,
            ProjectId = estimate.ProjectId,
            ProjectName = project.Name,
            Version = estimate.Version,
            Description = estimate.Description,
            OverheadPercent = estimate.OverheadPercent,
            ProfitPercent = estimate.ProfitPercent,
            BondPercent = estimate.BondPercent,
            TaxPercent = estimate.TaxPercent,
            ContingencyPercent = estimate.ContingencyPercent,
            TotalBidPrice = 0,
            LineItemCount = 0,
            CreatedAt = estimate.CreatedAt,
            UpdatedAt = estimate.UpdatedAt
        });
    }

    // =====================================================================
    // PUT /api/v1/estimates/{id}/markups — Update markup percentages
    // =====================================================================
    // Changing markups triggers recalculation of the bid price.

    [HttpPut("{id}/markups")]
    public async Task<IActionResult> UpdateMarkups(Guid id, [FromBody] UpdateEstimateMarkupsRequest request)
    {
        var estimate = await _context.Estimates
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (estimate == null)
            return NotFound($"Estimate with ID {id} not found");

        if (estimate.IsSubmitted)
            return BadRequest("Cannot modify a submitted estimate");

        estimate.OverheadPercent = request.OverheadPercent;
        estimate.ProfitPercent = request.ProfitPercent;
        estimate.BondPercent = request.BondPercent;
        estimate.TaxPercent = request.TaxPercent;
        estimate.ContingencyPercent = request.ContingencyPercent;
        estimate.Description = request.Description?.Trim() ?? estimate.Description;

        // RECALCULATE with new markups
        RecalculateEstimateTotals(estimate);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated markups for estimate {EstimateId} — new bid: {BidPrice:C}",
            estimate.Id, estimate.TotalBidPrice);

        return Ok(BuildEstimateDto(estimate));
    }

    // =====================================================================
    // DELETE /api/v1/estimates/{id}
    // =====================================================================

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEstimate(Guid id)
    {
        var estimate = await _context.Estimates.FindAsync(id);
        if (estimate == null)
            return NotFound($"Estimate with ID {id} not found");

        if (estimate.IsSubmitted)
            return BadRequest("Cannot delete a submitted estimate");

        _context.Estimates.Remove(estimate);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Estimate {estimate.Version} deleted" });
    }

    // =================================================================
    // ██╗     ██╗███╗   ██╗███████╗    ██╗████████╗███████╗███╗   ███╗███████╗
    // ██║     ██║████╗  ██║██╔════╝    ██║╚══██╔══╝██╔════╝████╗ ████║██╔════╝
    // ██║     ██║██╔██╗ ██║█████╗      ██║   ██║   █████╗  ██╔████╔██║███████╗
    // ██║     ██║██║╚██╗██║██╔══╝      ██║   ██║   ██╔══╝  ██║╚██╔╝██║╚════██║
    // ███████╗██║██║ ╚████║███████╗    ██║   ██║   ███████╗██║ ╚═╝ ██║███████║
    // ╚══════╝╚═╝╚═╝  ╚═══╝╚══════╝    ╚═╝   ╚═╝   ╚══════╝╚═╝     ╚═╝╚══════╝
    // =================================================================

    // =====================================================================
    // GET /api/v1/estimates/{id}/lineitems — All line items for an estimate
    // =====================================================================

    [HttpGet("{estimateId}/lineitems")]
    public async Task<IActionResult> GetLineItems(Guid estimateId)
    {
        var exists = await _context.Estimates.AnyAsync(e => e.Id == estimateId);
        if (!exists)
            return NotFound($"Estimate with ID {estimateId} not found");

        var lineItems = await _context.EstimateLineItems
            .Where(li => li.EstimateId == estimateId)
            .Include(li => li.CSISection)
                .ThenInclude(s => s!.Division)
            .OrderBy(li => li.SortOrder)
            .ThenBy(li => li.CSISection!.Code)
            .Select(li => new EstimateLineItemDto
            {
                Id = li.Id,
                EstimateId = li.EstimateId,
                CSISectionId = li.CSISectionId,
                CSICode = li.CSISection != null ? li.CSISection.Code : "",
                CSISectionName = li.CSISection != null ? li.CSISection.Name : "",
                Description = li.Description,
                Quantity = li.Quantity,
                UnitOfMeasure = li.UnitOfMeasure,
                WasteFactor = li.WasteFactor,
                AdjustedQuantity = li.AdjustedQuantity,
                MaterialUnitCost = li.MaterialUnitCost,
                MaterialTotal = li.MaterialTotal,
                LaborHoursPerUnit = li.LaborHoursPerUnit,
                LaborHours = li.LaborHours,
                LaborRate = li.LaborRate,
                LaborTotal = li.LaborTotal,
                EquipmentTotal = li.EquipmentTotal,
                SubcontractorTotal = li.SubcontractorTotal,
                LineTotal = li.LineTotal,
                TakeoffSource = li.TakeoffSource,
                Notes = li.Notes,
                SortOrder = li.SortOrder
            })
            .ToListAsync();

        return Ok(lineItems);
    }

    // =====================================================================
    // POST /api/v1/estimates/{id}/lineitems — Add a line item
    // =====================================================================
    //
    // THIS IS THE KEY ENDPOINT. When a user adds a line item:
    //   1. We calculate the line item's costs (adjusted qty, material, labor)
    //   2. We save the line item
    //   3. We recalculate the ENTIRE ESTIMATE (sum all lines, apply markups)
    //   4. We return both the new line item AND the updated estimate totals
    //
    // This ensures the estimate is ALWAYS in sync with its line items.
    // =====================================================================

    [HttpPost("{estimateId}/lineitems")]
    public async Task<IActionResult> AddLineItem(Guid estimateId, [FromBody] CreateLineItemRequest request)
    {
        var estimate = await _context.Estimates
            .Include(e => e.LineItems)
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == estimateId);

        if (estimate == null)
            return NotFound($"Estimate with ID {estimateId} not found");

        if (estimate.IsSubmitted)
            return BadRequest("Cannot add items to a submitted estimate");

        // Verify CSI section exists
        var section = await _context.CSISections
            .Include(s => s.Division)
            .FirstOrDefaultAsync(s => s.Id == request.CSISectionId);

        if (section == null)
            return BadRequest($"CSI Section with ID {request.CSISectionId} not found");

        // =====================================================================
        // CALCULATE THE LINE ITEM
        // =====================================================================
        // This is THE MATH — the fundamental calculation of construction estimating.
        //
        // Step 1: Adjusted Quantity = Quantity × Waste Factor
        //   9,400 SF × 1.10 = 10,340 SF
        //
        // Step 2: Material Total = Adjusted Qty × Material Unit Cost
        //   10,340 × $0.52 = $5,376.80
        //
        // Step 3: Labor Hours = Adjusted Qty × Hours Per Unit
        //   10,340 × 0.017 = 175.78 hours
        //
        // Step 4: Labor Total = Labor Hours × Labor Rate
        //   175.78 × $65.00 = $11,425.70
        //
        // Step 5: Line Total = Material + Labor + Equipment + Sub
        //   $5,376.80 + $11,425.70 + $0 + $0 = $16,802.50
        // =====================================================================

        var adjustedQty = request.Quantity * request.WasteFactor;
        var materialTotal = adjustedQty * request.MaterialUnitCost;
        var laborHours = adjustedQty * request.LaborHoursPerUnit;
        var laborTotal = laborHours * request.LaborRate;

        var lineTotal = materialTotal + laborTotal 
                      + request.EquipmentTotal + request.SubcontractorTotal;

        var lineItem = new EstimateLineItem
        {
            EstimateId = estimateId,
            CSISectionId = request.CSISectionId,
            Description = request.Description.Trim(),
            Quantity = request.Quantity,
            UnitOfMeasure = request.UnitOfMeasure.Trim().ToUpper(),
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
            TakeoffSource = request.TakeoffSource?.Trim(),
            Notes = request.Notes?.Trim(),
            SortOrder = estimate.LineItems.Count + 1
        };

        _context.EstimateLineItems.Add(lineItem);

        // RECALCULATE ESTIMATE TOTALS (includes this new line)
        estimate.LineItems.Add(lineItem);
        RecalculateEstimateTotals(estimate);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Added line item to estimate {EstimateId}: {Description} = {LineTotal:C} | New bid: {BidPrice:C}",
            estimateId, lineItem.Description, lineItem.LineTotal, estimate.TotalBidPrice);

        // Return the line item AND the updated estimate totals
        return Created(new
        {
            LineItem = BuildLineItemDto(lineItem, section),
            EstimateTotals = BuildEstimateDto(estimate)
        });
    }

    // =====================================================================
    // PUT /api/v1/estimates/{estimateId}/lineitems/{lineItemId}
    // =====================================================================

    [HttpPut("{estimateId}/lineitems/{lineItemId}")]
    public async Task<IActionResult> UpdateLineItem(
        Guid estimateId, Guid lineItemId, [FromBody] UpdateLineItemRequest request)
    {
        var estimate = await _context.Estimates
            .Include(e => e.LineItems)
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == estimateId);

        if (estimate == null)
            return NotFound($"Estimate with ID {estimateId} not found");

        if (estimate.IsSubmitted)
            return BadRequest("Cannot modify a submitted estimate");

        var lineItem = estimate.LineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (lineItem == null)
            return NotFound($"Line item with ID {lineItemId} not found in this estimate");

        // Recalculate this line item
        var adjustedQty = request.Quantity * request.WasteFactor;
        var materialTotal = adjustedQty * request.MaterialUnitCost;
        var laborHours = adjustedQty * request.LaborHoursPerUnit;
        var laborTotal = laborHours * request.LaborRate;
        var lineTotal = materialTotal + laborTotal 
                      + request.EquipmentTotal + request.SubcontractorTotal;

        lineItem.CSISectionId = request.CSISectionId;
        lineItem.Description = request.Description.Trim();
        lineItem.Quantity = request.Quantity;
        lineItem.UnitOfMeasure = request.UnitOfMeasure.Trim().ToUpper();
        lineItem.WasteFactor = request.WasteFactor;
        lineItem.AdjustedQuantity = Math.Round(adjustedQty, 4);
        lineItem.MaterialUnitCost = request.MaterialUnitCost;
        lineItem.MaterialTotal = Math.Round(materialTotal, 2);
        lineItem.LaborHoursPerUnit = request.LaborHoursPerUnit;
        lineItem.LaborHours = Math.Round(laborHours, 2);
        lineItem.LaborRate = request.LaborRate;
        lineItem.LaborTotal = Math.Round(laborTotal, 2);
        lineItem.EquipmentTotal = request.EquipmentTotal;
        lineItem.SubcontractorTotal = request.SubcontractorTotal;
        lineItem.LineTotal = Math.Round(lineTotal, 2);
        lineItem.TakeoffSource = request.TakeoffSource?.Trim();
        lineItem.Notes = request.Notes?.Trim();

        // RECALCULATE ESTIMATE
        RecalculateEstimateTotals(estimate);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            LineItem = new EstimateLineItemDto
            {
                Id = lineItem.Id,
                EstimateId = lineItem.EstimateId,
                CSISectionId = lineItem.CSISectionId,
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
            EstimateTotals = BuildEstimateDto(estimate)
        });
    }

    // =====================================================================
    // DELETE /api/v1/estimates/{estimateId}/lineitems/{lineItemId}
    // =====================================================================

    [HttpDelete("{estimateId}/lineitems/{lineItemId}")]
    public async Task<IActionResult> DeleteLineItem(Guid estimateId, Guid lineItemId)
    {
        var estimate = await _context.Estimates
            .Include(e => e.LineItems)
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == estimateId);

        if (estimate == null)
            return NotFound($"Estimate with ID {estimateId} not found");

        if (estimate.IsSubmitted)
            return BadRequest("Cannot modify a submitted estimate");

        var lineItem = estimate.LineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (lineItem == null)
            return NotFound($"Line item not found");

        estimate.LineItems.Remove(lineItem);
        _context.EstimateLineItems.Remove(lineItem);

        // RECALCULATE after removal
        RecalculateEstimateTotals(estimate);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"Line item deleted",
            EstimateTotals = BuildEstimateDto(estimate)
        });
    }

    // =================================================================
    //  ██████╗ █████╗ ██╗      ██████╗    ███████╗███╗   ██╗ ██████╗ ██╗███╗   ██╗███████╗
    // ██╔════╝██╔══██╗██║     ██╔════╝    ██╔════╝████╗  ██║██╔════╝ ██║████╗  ██║██╔════╝
    // ██║     ███████║██║     ██║         █████╗  ██╔██╗ ██║██║  ███╗██║██╔██╗ ██║█████╗  
    // ██║     ██╔══██║██║     ██║         ██╔══╝  ██║╚██╗██║██║   ██║██║██║╚██╗██║██╔══╝  
    // ╚██████╗██║  ██║███████╗╚██████╗    ███████╗██║ ╚████║╚██████╔╝██║██║ ╚████║███████╗
    //  ╚═════╝╚═╝  ╚═╝╚══════╝ ╚═════╝    ╚══════╝╚═╝  ╚═══╝ ╚═════╝ ╚═╝╚═╝  ╚═══╝╚══════╝
    // =================================================================

    // =====================================================================
    // THE CALCULATION ENGINE
    // =====================================================================
    //
    // This is the equivalent of JERP's trial balance logic.
    // In JERP: Debits must equal Credits across all accounts.
    // Here: DirectCost must equal SUM(LineItems) and TotalBidPrice must
    // follow the markup formula exactly.
    //
    // The engine runs every time ANY line item changes.
    // It ensures the estimate is ALWAYS mathematically correct.
    //
    // THE FORMULA:
    //   DirectCost = SUM(LineItems.LineTotal)
    //   OverheadAmount = DirectCost × (OverheadPercent / 100)
    //   ProfitAmount = (DirectCost + OverheadAmount) × (ProfitPercent / 100)
    //   Subtotal = DirectCost + OverheadAmount + ProfitAmount
    //   BondAmount = Subtotal × (BondPercent / 100)
    //   TaxAmount = MaterialTotal × (TaxPercent / 100)
    //   ContingencyAmount = DirectCost × (ContingencyPercent / 100)
    //   TotalBidPrice = Subtotal + BondAmount + TaxAmount + ContingencyAmount
    //
    // =====================================================================

    private void RecalculateEstimateTotals(Estimate estimate)
    {
        // Step 1: Sum all line items by cost type
        estimate.MaterialTotal = estimate.LineItems.Sum(li => li.MaterialTotal);
        estimate.LaborTotal = estimate.LineItems.Sum(li => li.LaborTotal);
        estimate.EquipmentTotal = estimate.LineItems.Sum(li => li.EquipmentTotal);
        estimate.SubcontractorTotal = estimate.LineItems.Sum(li => li.SubcontractorTotal);

        // Step 2: Direct Cost = all four components
        estimate.DirectCost = estimate.MaterialTotal + estimate.LaborTotal
                            + estimate.EquipmentTotal + estimate.SubcontractorTotal;

        // Step 3: Overhead (on direct cost)
        estimate.OverheadAmount = Math.Round(
            estimate.DirectCost * (estimate.OverheadPercent / 100m), 2);

        // Step 4: Profit (on cost + overhead)
        var costPlusOverhead = estimate.DirectCost + estimate.OverheadAmount;
        estimate.ProfitAmount = Math.Round(
            costPlusOverhead * (estimate.ProfitPercent / 100m), 2);

        // Step 5: Subtotal before bond/tax/contingency
        var subtotal = estimate.DirectCost + estimate.OverheadAmount + estimate.ProfitAmount;

        // Step 6: Bond (on subtotal — the bonding company bases their fee on your price)
        estimate.BondAmount = Math.Round(
            subtotal * (estimate.BondPercent / 100m), 2);

        // Step 7: Tax (on MATERIALS ONLY — labor is not taxed)
        estimate.TaxAmount = Math.Round(
            estimate.MaterialTotal * (estimate.TaxPercent / 100m), 2);

        // Step 8: Contingency (on direct cost — a safety buffer)
        estimate.ContingencyAmount = Math.Round(
            estimate.DirectCost * (estimate.ContingencyPercent / 100m), 2);

        // Step 9: TOTAL BID PRICE
        estimate.TotalBidPrice = subtotal + estimate.BondAmount 
                               + estimate.TaxAmount + estimate.ContingencyAmount;

        // Step 10: Cost per square foot (if project has square footage)
        if (estimate.Project?.GrossSquareFootage > 0)
        {
            estimate.CostPerSquareFoot = Math.Round(
                estimate.TotalBidPrice / estimate.Project.GrossSquareFootage.Value, 2);
        }

        estimate.LastCalculatedAt = DateTime.UtcNow;

        // Also update the project's bid amount
        if (estimate.Project != null)
        {
            estimate.Project.BidAmount = estimate.TotalBidPrice;
        }
    }

    // =====================================================================
    // GET /api/v1/estimates/{id}/breakdown — Cost breakdown by CSI division
    // =====================================================================
    //
    // Groups all line items by their CSI division and shows:
    //   Division 03 - Concrete: $245,000 (18% of total)
    //   Division 09 - Finishes: $189,000 (14% of total)
    //   Division 26 - Electrical: $312,000 (23% of total) ← highest!
    //
    // This is like a trial balance grouped by account type in JERP.
    // =====================================================================

    [HttpGet("{id}/breakdown")]
    public async Task<IActionResult> GetCostBreakdown(Guid id)
    {
        var estimate = await _context.Estimates
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (estimate == null)
            return NotFound($"Estimate with ID {id} not found");

        var lineItems = await _context.EstimateLineItems
            .Where(li => li.EstimateId == id)
            .Include(li => li.CSISection)
                .ThenInclude(s => s!.Division)
            .ToListAsync();

        // Group by CSI Division
        var divisionGroups = lineItems
            .Where(li => li.CSISection?.Division != null)
            .GroupBy(li => new
            {
                li.CSISection!.Division!.Code,
                li.CSISection!.Division!.Name
            })
            .Select(g => new DivisionCostSummary
            {
                DivisionCode = g.Key.Code,
                DivisionName = g.Key.Name,
                MaterialTotal = g.Sum(li => li.MaterialTotal),
                LaborTotal = g.Sum(li => li.LaborTotal),
                EquipmentTotal = g.Sum(li => li.EquipmentTotal),
                SubcontractorTotal = g.Sum(li => li.SubcontractorTotal),
                Total = g.Sum(li => li.LineTotal),
                PercentOfTotal = estimate.DirectCost > 0
                    ? Math.Round(g.Sum(li => li.LineTotal) / estimate.DirectCost * 100, 1)
                    : 0,
                LineItemCount = g.Count()
            })
            .OrderBy(d => d.DivisionCode)
            .ToList();

        return Ok(new EstimateCostBreakdownDto
        {
            EstimateId = estimate.Id,
            ProjectName = estimate.Project?.Name ?? "",
            Version = estimate.Version,
            DivisionBreakdown = divisionGroups,
            MaterialTotal = estimate.MaterialTotal,
            LaborTotal = estimate.LaborTotal,
            EquipmentTotal = estimate.EquipmentTotal,
            SubcontractorTotal = estimate.SubcontractorTotal,
            DirectCost = estimate.DirectCost,
            TotalBidPrice = estimate.TotalBidPrice
        });
    }

    // =====================================================================
    // POST /api/v1/estimates/{id}/recalculate — Force full recalculation
    // =====================================================================

    [HttpPost("{id}/recalculate")]
    public async Task<IActionResult> ForceRecalculate(Guid id)
    {
        var estimate = await _context.Estimates
            .Include(e => e.LineItems)
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (estimate == null)
            return NotFound($"Estimate with ID {id} not found");

        // Recalculate every line item first
        foreach (var li in estimate.LineItems)
        {
            li.AdjustedQuantity = Math.Round(li.Quantity * li.WasteFactor, 4);
            li.MaterialTotal = Math.Round(li.AdjustedQuantity * li.MaterialUnitCost, 2);
            li.LaborHours = Math.Round(li.AdjustedQuantity * li.LaborHoursPerUnit, 2);
            li.LaborTotal = Math.Round(li.LaborHours * li.LaborRate, 2);
            li.LineTotal = Math.Round(
                li.MaterialTotal + li.LaborTotal + li.EquipmentTotal + li.SubcontractorTotal, 2);
        }

        // Then recalculate estimate totals
        RecalculateEstimateTotals(estimate);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Force recalculated estimate {EstimateId} — Bid: {BidPrice:C}",
            id, estimate.TotalBidPrice);

        return Ok(BuildEstimateDto(estimate));
    }

    // =====================================================================
    // HELPER METHODS — Build DTOs from entities
    // =====================================================================

    private EstimateDto BuildEstimateDto(Estimate estimate)
    {
        return new EstimateDto
        {
            Id = estimate.Id,
            ProjectId = estimate.ProjectId,
            ProjectName = estimate.Project?.Name ?? "",
            Version = estimate.Version,
            Description = estimate.Description,
            MaterialTotal = estimate.MaterialTotal,
            LaborTotal = estimate.LaborTotal,
            EquipmentTotal = estimate.EquipmentTotal,
            SubcontractorTotal = estimate.SubcontractorTotal,
            DirectCost = estimate.DirectCost,
            OverheadPercent = estimate.OverheadPercent,
            OverheadAmount = estimate.OverheadAmount,
            ProfitPercent = estimate.ProfitPercent,
            ProfitAmount = estimate.ProfitAmount,
            BondPercent = estimate.BondPercent,
            BondAmount = estimate.BondAmount,
            TaxPercent = estimate.TaxPercent,
            TaxAmount = estimate.TaxAmount,
            ContingencyPercent = estimate.ContingencyPercent,
            ContingencyAmount = estimate.ContingencyAmount,
            TotalBidPrice = estimate.TotalBidPrice,
            CostPerSquareFoot = estimate.CostPerSquareFoot,
            IsSubmitted = estimate.IsSubmitted,
            SubmittedDate = estimate.SubmittedDate,
            LineItemCount = estimate.LineItems.Count,
            CreatedAt = estimate.CreatedAt,
            UpdatedAt = estimate.UpdatedAt,
            LastCalculatedAt = estimate.LastCalculatedAt
        };
    }

    private static EstimateLineItemDto BuildLineItemDto(EstimateLineItem li, CSISection section)
    {
        return new EstimateLineItemDto
        {
            Id = li.Id,
            EstimateId = li.EstimateId,
            CSISectionId = li.CSISectionId,
            CSICode = section.Code,
            CSISectionName = section.Name,
            Description = li.Description,
            Quantity = li.Quantity,
            UnitOfMeasure = li.UnitOfMeasure,
            WasteFactor = li.WasteFactor,
            AdjustedQuantity = li.AdjustedQuantity,
            MaterialUnitCost = li.MaterialUnitCost,
            MaterialTotal = li.MaterialTotal,
            LaborHoursPerUnit = li.LaborHoursPerUnit,
            LaborHours = li.LaborHours,
            LaborRate = li.LaborRate,
            LaborTotal = li.LaborTotal,
            EquipmentTotal = li.EquipmentTotal,
            SubcontractorTotal = li.SubcontractorTotal,
            LineTotal = li.LineTotal,
            TakeoffSource = li.TakeoffSource,
            Notes = li.Notes,
            SortOrder = li.SortOrder
        };
    }
}
