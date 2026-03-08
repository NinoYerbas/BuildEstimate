/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// ASSEMBLIES CONTROLLER — Template Management + Explosion Engine
// ============================================================================
//
// An "Assembly" in construction estimating is a pre-built cost template
// that contains multiple line items.
//
// EXAMPLE — "Interior Drywall Wall System" assembly:
//   When a user applies this assembly with "500 SF", the system automatically creates:
//   - 09 21 00: Metal stud framing — 500 SF × 1.0 factor = 500 SF
//   - 09 29 00: 5/8" drywall one side — 500 SF × 1.0 factor = 500 SF
//   - 09 29 00: 5/8" drywall other side — 500 SF × 1.0 factor = 500 SF
//   - 09 91 00: Primer + paint 2 coats — 500 SF × 2.0 factor = 1,000 SF
//   - 09 21 00: Drywall tape & mud — 500 SF × 1.0 factor = 500 SF
//   That's 5 line items created from one command — this is the "explosion."
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
/// Manages assembly templates and applies them to estimates.
/// The key operation is ApplyAssembly — it "explodes" a template into
/// multiple estimate line items in a single API call.
/// </summary>
[Route("api/v1/assemblies")]
[AllowAnonymous]
public class AssembliesController : BaseApiController
{
    private readonly BuildEstimateDbContext _context;
    private readonly ILogger<AssembliesController> _logger;

    /// <summary>
    /// Constructs the controller with injected dependencies.
    /// </summary>
    public AssembliesController(
        BuildEstimateDbContext context,
        ILogger<AssembliesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET /api/v1/assemblies?category=Walls
    /// <summary>
    /// Lists all active assemblies, optionally filtered by category or search text.
    /// Returns summary info only (no components) — use GET /assemblies/{id} for full detail.
    /// </summary>
    /// <param name="category">Optional category filter, e.g., "Walls", "Ceilings", "MEP".</param>
    /// <param name="search">Optional name/code search string.</param>
    [HttpGet]
    public async Task<IActionResult> GetAssemblies(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null)
    {
        var query = _context.Assemblies
            .Where(a => a.IsActive) // ← only active assemblies; deleted ones have IsActive = false
            .AsQueryable();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(a => a.Category == category);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(a => a.Name.Contains(search)
                || (a.AssemblyCode != null && a.AssemblyCode.Contains(search)));

        var assemblies = await query
            .OrderBy(a => a.Category)
            .ThenBy(a => a.Name)
            .Select(a => new AssemblyDto
            {
                Id = a.Id,
                Name = a.Name,
                AssemblyCode = a.AssemblyCode,
                Description = a.Description,
                Category = a.Category,
                UnitOfMeasure = a.UnitOfMeasure,
                MaterialCostPerUnit = a.MaterialCostPerUnit,
                LaborCostPerUnit = a.LaborCostPerUnit,
                EquipmentCostPerUnit = a.EquipmentCostPerUnit,
                TotalCostPerUnit = a.TotalCostPerUnit,
                ComponentCount = a.ComponentCount,
                IsGlobal = a.IsGlobal,
                Source = a.Source
            })
            .ToListAsync();

        return Ok(assemblies);
    }

    // GET /api/v1/assemblies/{id} — with all components
    /// <summary>
    /// Gets a single assembly with its complete list of components.
    /// Use this before applying an assembly to see exactly what line items will be created.
    /// </summary>
    /// <param name="id">The assembly ID to retrieve.</param>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAssembly(Guid id)
    {
        var assembly = await _context.Assemblies
            .Where(a => a.Id == id)
            .Include(a => a.Components)
                .ThenInclude(c => c.CSISection) // ← need CSI code for display
            .Include(a => a.Components)
                .ThenInclude(c => c.Trade)      // ← need trade name for display
            .FirstOrDefaultAsync();

        if (assembly == null)
            return NotFound($"Assembly with ID {id} not found");

        return Ok(new AssemblyDto
        {
            Id = assembly.Id,
            Name = assembly.Name,
            AssemblyCode = assembly.AssemblyCode,
            Description = assembly.Description,
            Category = assembly.Category,
            UnitOfMeasure = assembly.UnitOfMeasure,
            MaterialCostPerUnit = assembly.MaterialCostPerUnit,
            LaborCostPerUnit = assembly.LaborCostPerUnit,
            EquipmentCostPerUnit = assembly.EquipmentCostPerUnit,
            TotalCostPerUnit = assembly.TotalCostPerUnit,
            ComponentCount = assembly.ComponentCount,
            IsGlobal = assembly.IsGlobal,
            Source = assembly.Source,
            Components = assembly.Components
                .OrderBy(c => c.SortOrder)
                .Select(c => new AssemblyComponentDto
                {
                    Id = c.Id,
                    CSISectionId = c.CSISectionId,
                    CSICode = c.CSISection?.Code ?? "",
                    CSISectionName = c.CSISection?.Name ?? "",
                    Description = c.Description,
                    QuantityFactor = c.QuantityFactor,
                    UnitOfMeasure = c.UnitOfMeasure,
                    WasteFactor = c.WasteFactor,
                    MaterialUnitCost = c.MaterialUnitCost,
                    LaborHoursPerUnit = c.LaborHoursPerUnit,
                    LaborRate = c.LaborRate,
                    EquipmentCost = c.EquipmentCost,
                    TradeId = c.TradeId,
                    TradeName = c.Trade?.Name,
                    SortOrder = c.SortOrder
                })
                .ToList()
        });
    }

    // POST /api/v1/assemblies — Create assembly with components in one call
    /// <summary>
    /// Creates a new assembly template with all its components in a single request.
    /// After saving, calculates and stores the per-unit costs so they're ready
    /// to display in the assembly list without loading components.
    /// </summary>
    /// <param name="request">The assembly name, category, UOM, and component list.</param>
    [HttpPost]
    public async Task<IActionResult> CreateAssembly([FromBody] CreateAssemblyRequest request)
    {
        if (request.Components.Count == 0)
            return BadRequest("Assembly must have at least one component");

        var assembly = new Assembly
        {
            Name = request.Name.Trim(),
            AssemblyCode = request.AssemblyCode?.Trim().ToUpper(),
            Description = request.Description?.Trim(),
            Category = request.Category.Trim(),
            UnitOfMeasure = request.UnitOfMeasure.Trim().ToUpper(),
            Source = request.Source?.Trim(),
            CreatedBy = GetCurrentUsername()
        };

        int sortOrder = 1; // ← track display order as we add components
        foreach (var comp in request.Components)
        {
            assembly.Components.Add(new AssemblyComponent
            {
                CSISectionId = comp.CSISectionId,
                Description = comp.Description.Trim(),
                QuantityFactor = comp.QuantityFactor,
                UnitOfMeasure = comp.UnitOfMeasure.Trim().ToUpper(),
                WasteFactor = comp.WasteFactor,
                MaterialUnitCost = comp.MaterialUnitCost,
                LaborHoursPerUnit = comp.LaborHoursPerUnit,
                LaborRate = comp.LaborRate,
                EquipmentCost = comp.EquipmentCost,
                TradeId = comp.TradeId,
                Notes = comp.Notes?.Trim(),
                SortOrder = sortOrder++
            });
        }

        // Calculate summary costs per unit
        RecalculateAssemblyCosts(assembly);

        _context.Assemblies.Add(assembly);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created assembly: {Name} ({Code}) with {Count} components, ${Cost}/{UOM}",
            assembly.Name, assembly.AssemblyCode, assembly.ComponentCount,
            assembly.TotalCostPerUnit, assembly.UnitOfMeasure);

        return Created(new AssemblyDto
        {
            Id = assembly.Id,
            Name = assembly.Name,
            AssemblyCode = assembly.AssemblyCode,
            Description = assembly.Description,
            Category = assembly.Category,
            UnitOfMeasure = assembly.UnitOfMeasure,
            MaterialCostPerUnit = assembly.MaterialCostPerUnit,
            LaborCostPerUnit = assembly.LaborCostPerUnit,
            EquipmentCostPerUnit = assembly.EquipmentCostPerUnit,
            TotalCostPerUnit = assembly.TotalCostPerUnit,
            ComponentCount = assembly.ComponentCount,
            IsGlobal = assembly.IsGlobal,
            Source = assembly.Source
        });
    }

    // =================================================================
    // ███████╗██╗  ██╗██████╗ ██╗      ██████╗ ██████╗ ███████╗
    // ██╔════╝╚██╗██╔╝██╔══██╗██║     ██╔═══██╗██╔══██╗██╔════╝
    // █████╗   ╚███╔╝ ██████╔╝██║     ██║   ██║██║  ██║█████╗
    // ██╔══╝   ██╔██╗ ██╔═══╝ ██║     ██║   ██║██║  ██║██╔══╝
    // ███████╗██╔╝ ██╗██║     ███████╗╚██████╔╝██████╔╝███████╗
    // ╚══════╝╚═╝  ╚═╝╚═╝     ╚══════╝ ╚═════╝ ╚═════╝ ╚══════╝
    // =================================================================

    // =====================================================================
    // POST /api/v1/assemblies/{id}/apply — THE EXPLOSION ENGINE
    // =====================================================================
    //
    // This is the MAGIC. User says:
    //   "Apply 500 SF of Interior Wall to my estimate"
    //
    // The system:
    //   1. Reads the assembly template (8 components)
    //   2. For EACH component:
    //      - Calculates quantity: 500 × QuantityFactor × WasteFactor
    //      - Calculates material, labor, equipment costs
    //      - Creates an EstimateLineItem
    //   3. Recalculates estimate totals (overhead, profit, bid price)
    //   4. Returns all 8 line items + updated bid price
    //
    // One API call → 8 line items → fully priced estimate section.
    // =====================================================================

    /// <summary>
    /// Applies an assembly template to an estimate — the "explosion" operation.
    /// For each component in the assembly, calculates costs scaled to the requested quantity
    /// and creates a new EstimateLineItem. Then recalculates the full estimate totals.
    /// Optionally looks up project-specific prevailing wage rates for each trade.
    /// </summary>
    /// <param name="id">The assembly template to apply.</param>
    /// <param name="request">The target estimate, quantity to apply, and optional overrides.</param>
    /// <returns>A summary of the operation with all created line items and the updated bid price.</returns>
    [HttpPost("{id}/apply")]
    public async Task<IActionResult> ApplyAssembly(Guid id, [FromBody] ApplyAssemblyRequest request)
    {
        var assembly = await _context.Assemblies
            .Include(a => a.Components)
                .ThenInclude(c => c.CSISection)
            .Include(a => a.Components)
                .ThenInclude(c => c.Trade)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assembly == null)
            return NotFound($"Assembly with ID {id} not found");

        if (assembly.Components.Count == 0)
            return BadRequest("Assembly has no components");

        var estimate = await _context.Estimates
            .Include(e => e.LineItems)
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == request.EstimateId);

        if (estimate == null)
            return BadRequest($"Estimate with ID {request.EstimateId} not found");

        if (estimate.IsSubmitted)
            return BadRequest("Cannot add items to a submitted estimate");

        // =====================================================================
        // THE EXPLOSION LOOP
        // =====================================================================

        var createdItems = new List<EstimateLineItemDto>();
        decimal totalMaterial = 0, totalLabor = 0, totalEquipment = 0;
        int baseSortOrder = estimate.LineItems.Count + 1; // ← new items go after existing ones

        foreach (var comp in assembly.Components.OrderBy(c => c.SortOrder))
        {
            // Step 1: Calculate component quantity
            // Assembly qty × factor × waste = actual quantity
            // 500 SF wall × 2.0 (both sides) × 1.10 (10% waste) = 1,100 SF drywall
            var rawQty = request.Quantity * comp.QuantityFactor;
            var adjustedQty = rawQty * comp.WasteFactor; // ← quantity after waste factor

            // Step 2: Look up labor rate if override requested
            var laborRate = comp.LaborRate;
            if (request.OverrideLaborRates && comp.TradeId.HasValue && estimate.Project != null)
            {
                var projectRate = await _context.LaborRates
                    .Where(r => r.TradeId == comp.TradeId.Value
                             && r.County.Contains(estimate.Project.County ?? "")
                             && r.State == (estimate.Project.State ?? "")
                             && r.IsActive)
                    .OrderByDescending(r => r.RateType == "Prevailing" &&
                        estimate.Project.IsPrevailingWage) // ← prefer prevailing if project requires it
                    .FirstOrDefaultAsync();

                if (projectRate != null)
                    laborRate = projectRate.TotalRate; // ← use the project's local rate
            }

            // Step 3: Calculate costs
            var materialTotal = Math.Round(adjustedQty * comp.MaterialUnitCost, 2);
            var laborHours = Math.Round(adjustedQty * comp.LaborHoursPerUnit, 2);
            var laborTotal = Math.Round(laborHours * laborRate, 2);
            var lineTotal = materialTotal + laborTotal + comp.EquipmentCost;

            // Step 4: Create the line item
            var lineItem = new EstimateLineItem
            {
                EstimateId = request.EstimateId,
                CSISectionId = comp.CSISectionId,
                Description = comp.Description,
                Quantity = Math.Round(rawQty, 4),
                UnitOfMeasure = comp.UnitOfMeasure,
                WasteFactor = comp.WasteFactor,
                AdjustedQuantity = Math.Round(adjustedQty, 4),
                MaterialUnitCost = comp.MaterialUnitCost,
                MaterialTotal = materialTotal,
                LaborHoursPerUnit = comp.LaborHoursPerUnit,
                LaborHours = laborHours,
                LaborRate = laborRate,
                LaborTotal = laborTotal,
                EquipmentTotal = comp.EquipmentCost,
                SubcontractorTotal = 0,
                LineTotal = Math.Round(lineTotal, 2),
                TakeoffSource = request.Location,
                Notes = $"From assembly: {assembly.Name}", // ← track origin for auditing
                SortOrder = baseSortOrder++
            };

            _context.EstimateLineItems.Add(lineItem);
            estimate.LineItems.Add(lineItem);

            totalMaterial += materialTotal;
            totalLabor += laborTotal;
            totalEquipment += comp.EquipmentCost;

            createdItems.Add(new EstimateLineItemDto
            {
                Id = lineItem.Id,
                EstimateId = lineItem.EstimateId,
                CSISectionId = lineItem.CSISectionId,
                CSICode = comp.CSISection?.Code ?? "",
                CSISectionName = comp.CSISection?.Name ?? "",
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
                SubcontractorTotal = 0,
                LineTotal = lineItem.LineTotal,
                TakeoffSource = lineItem.TakeoffSource,
                Notes = lineItem.Notes,
                SortOrder = lineItem.SortOrder
            });
        }

        // Step 5: Recalculate estimate totals with all new line items included
        RecalculateEstimateTotals(estimate);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Applied assembly '{Name}' × {Qty} {UOM} → {Count} line items, bid now {Bid:C}",
            assembly.Name, request.Quantity, assembly.UnitOfMeasure,
            createdItems.Count, estimate.TotalBidPrice);

        return Created(new ApplyAssemblyResultDto
        {
            AssemblyName = assembly.Name,
            Quantity = request.Quantity,
            UnitOfMeasure = assembly.UnitOfMeasure,
            LineItemsCreated = createdItems.Count,
            TotalMaterial = totalMaterial,
            TotalLabor = totalLabor,
            TotalEquipment = totalEquipment,
            TotalDirectCost = totalMaterial + totalLabor + totalEquipment,
            UpdatedBidPrice = estimate.TotalBidPrice, // ← what the bid is now after adding this assembly
            CreatedLineItems = createdItems
        });
    }

    // GET /api/v1/assemblies/categories — List available categories
    /// <summary>
    /// Returns a distinct list of assembly categories with counts.
    /// Used to populate the category filter dropdown in the UI.
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.Assemblies
            .Where(a => a.IsActive)
            .GroupBy(a => a.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderBy(c => c.Category)
            .ToListAsync();

        return Ok(categories);
    }

    // DELETE /api/v1/assemblies/{id}
    /// <summary>
    /// Soft-deletes an assembly by setting IsActive = false.
    /// The assembly record is kept for historical reference on existing estimates.
    /// Hard-delete is intentionally not supported.
    /// </summary>
    /// <param name="id">The assembly to deactivate.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAssembly(Guid id)
    {
        var assembly = await _context.Assemblies.FindAsync(id);
        if (assembly == null)
            return NotFound($"Assembly with ID {id} not found");

        assembly.IsActive = false; // Soft delete — keep the record, just hide it
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Assembly '{assembly.Name}' deactivated" });
    }

    // =====================================================================
    // HELPER — Calculate assembly summary costs
    // =====================================================================

    /// <summary>
    /// Recalculates the assembly's per-unit cost summary from its components.
    /// These summary fields (MaterialCostPerUnit, etc.) are denormalized copies
    /// stored on the assembly so the list view doesn't need to load all components.
    /// </summary>
    /// <param name="assembly">The assembly to recalculate. Modified in place.</param>
    private static void RecalculateAssemblyCosts(Assembly assembly)
    {
        decimal materialPerUnit = 0, laborPerUnit = 0, equipmentPerUnit = 0;

        foreach (var comp in assembly.Components)
        {
            var adjFactor = comp.QuantityFactor * comp.WasteFactor; // ← combined scaling factor
            materialPerUnit += adjFactor * comp.MaterialUnitCost;
            laborPerUnit += adjFactor * comp.LaborHoursPerUnit * comp.LaborRate;
            equipmentPerUnit += comp.EquipmentCost;
        }

        assembly.MaterialCostPerUnit = Math.Round(materialPerUnit, 2);
        assembly.LaborCostPerUnit = Math.Round(laborPerUnit, 2);
        assembly.EquipmentCostPerUnit = Math.Round(equipmentPerUnit, 2);
        assembly.TotalCostPerUnit = Math.Round(
            materialPerUnit + laborPerUnit + equipmentPerUnit, 2);
        assembly.ComponentCount = assembly.Components.Count;
    }

    // =====================================================================
    // ESTIMATE RECALCULATION
    // =====================================================================

    /// <summary>
    /// Recalculates all estimate totals after adding assembly line items.
    /// Sums line items, applies markups, and updates the bid price.
    /// This is the same calculation engine used by EstimatesController.
    /// </summary>
    /// <param name="estimate">The estimate to recalculate. Modified in place.</param>
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
        estimate.BondAmount = Math.Round(subtotal * (estimate.BondPercent / 100m), 2);
        estimate.TaxAmount = Math.Round(estimate.MaterialTotal * (estimate.TaxPercent / 100m), 2); // ← tax on materials only
        estimate.ContingencyAmount = Math.Round(estimate.DirectCost * (estimate.ContingencyPercent / 100m), 2);
        estimate.TotalBidPrice = subtotal + estimate.BondAmount + estimate.TaxAmount + estimate.ContingencyAmount;

        if (estimate.Project?.GrossSquareFootage > 0)
            estimate.CostPerSquareFoot = Math.Round(
                estimate.TotalBidPrice / estimate.Project.GrossSquareFootage.Value, 2);

        estimate.LastCalculatedAt = DateTime.UtcNow;
        if (estimate.Project != null)
            estimate.Project.BidAmount = estimate.TotalBidPrice; // ← sync project-level bid amount
    }
}
