/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// AI ASSISTANT CONTROLLER — Claude-Powered Estimate Intelligence
// ============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BuildEstimate.Application.Services;
using BuildEstimate.Infrastructure.Data;

namespace BuildEstimate.Api.Controllers;

[Route("api/v1/ai")]
[AllowAnonymous]
public class AIAssistantController : BaseApiController
{
    private readonly BuildEstimateDbContext _context;
    private readonly AIEstimateService _aiService;
    private readonly ILogger<AIAssistantController> _logger;

    public AIAssistantController(
        BuildEstimateDbContext context,
        AIEstimateService aiService,
        ILogger<AIAssistantController> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    // =====================================================================
    // POST /api/v1/ai/validate/{estimateId}
    // "Does my estimate have missing items or wrong quantities?"
    // =====================================================================

    [HttpPost("validate/{estimateId}")]
    public async Task<IActionResult> ValidateEstimate(Guid estimateId)
    {
        var data = await LoadEstimateForAI(estimateId);
        if (data == null)
            return NotFound($"Estimate with ID {estimateId} not found");

        _logger.LogInformation("AI validating estimate {Id} ({Items} items)",
            estimateId, data.LineItems.Count);

        var result = await _aiService.ValidateEstimate(data);
        return Ok(result);
    }

    // =====================================================================
    // POST /api/v1/ai/check-pricing/{estimateId}
    // "Are my prices competitive with the market?"
    // =====================================================================

    [HttpPost("check-pricing/{estimateId}")]
    public async Task<IActionResult> CheckPricing(Guid estimateId)
    {
        var data = await LoadEstimateForAI(estimateId);
        if (data == null)
            return NotFound($"Estimate with ID {estimateId} not found");

        var result = await _aiService.CheckPricing(data);
        return Ok(result);
    }

    // =====================================================================
    // POST /api/v1/ai/suggest/{estimateId}
    // "What assemblies or items am I missing?"
    // =====================================================================

    [HttpPost("suggest/{estimateId}")]
    public async Task<IActionResult> SuggestAssemblies(Guid estimateId)
    {
        var data = await LoadEstimateForAI(estimateId);
        if (data == null)
            return NotFound($"Estimate with ID {estimateId} not found");

        var result = await _aiService.SuggestAssemblies(data);
        return Ok(result);
    }

    // =====================================================================
    // POST /api/v1/ai/risk/{estimateId}
    // "What are the financial risks in this estimate?"
    // =====================================================================

    [HttpPost("risk/{estimateId}")]
    public async Task<IActionResult> AnalyzeRisk(Guid estimateId)
    {
        var data = await LoadEstimateForAI(estimateId);
        if (data == null)
            return NotFound($"Estimate with ID {estimateId} not found");

        var result = await _aiService.AnalyzeRisk(data);
        return Ok(result);
    }

    // =====================================================================
    // POST /api/v1/ai/full-review/{estimateId}
    // "Run ALL analysis in one call"
    // =====================================================================

    [HttpPost("full-review/{estimateId}")]
    public async Task<IActionResult> FullReview(Guid estimateId)
    {
        var data = await LoadEstimateForAI(estimateId);
        if (data == null)
            return NotFound($"Estimate with ID {estimateId} not found");

        _logger.LogInformation("AI full review of estimate {Id}", estimateId);

        // Run all analyses in parallel
        var validateTask = _aiService.ValidateEstimate(data);
        var pricingTask = _aiService.CheckPricing(data);
        var suggestTask = _aiService.SuggestAssemblies(data);
        var riskTask = _aiService.AnalyzeRisk(data);

        await Task.WhenAll(validateTask, pricingTask, suggestTask, riskTask);

        return Ok(new
        {
            Validation = await validateTask,
            Pricing = await pricingTask,
            Suggestions = await suggestTask,
            Risk = await riskTask,
            Timestamp = DateTime.UtcNow
        });
    }

    // =====================================================================
    // HELPER — Load estimate data for AI analysis
    // =====================================================================

    private async Task<EstimateForAI?> LoadEstimateForAI(Guid estimateId)
    {
        var estimate = await _context.Estimates
            .Include(e => e.Project)
            .Include(e => e.LineItems)
                .ThenInclude(li => li.CSISection)
            .FirstOrDefaultAsync(e => e.Id == estimateId);

        if (estimate == null) return null;

        return new EstimateForAI
        {
            ProjectName = estimate.Project?.Name ?? "Unknown",
            ProjectType = estimate.Project?.Type.ToString() ?? "Commercial",
            SquareFootage = estimate.Project?.GrossSquareFootage ?? 0,
            County = estimate.Project?.County ?? "",
            State = estimate.Project?.State ?? "",
            IsPrevailingWage = estimate.Project?.IsPrevailingWage ?? false,
            MaterialTotal = estimate.MaterialTotal,
            LaborTotal = estimate.LaborTotal,
            EquipmentTotal = estimate.EquipmentTotal,
            SubcontractorTotal = estimate.SubcontractorTotal,
            DirectCost = estimate.DirectCost,
            TotalBidPrice = estimate.TotalBidPrice,
            CostPerSF = estimate.CostPerSquareFoot ?? 0m,
            OverheadPercent = estimate.OverheadPercent,
            ProfitPercent = estimate.ProfitPercent,
            ContingencyPercent = estimate.ContingencyPercent,
            LineItems = estimate.LineItems.Select(li => new LineItemForAI
            {
                CSICode = li.CSISection?.Code ?? "Unknown",
                Description = li.Description,
                Quantity = li.Quantity,
                UOM = li.UnitOfMeasure,
                MaterialTotal = li.MaterialTotal,
                LaborTotal = li.LaborTotal,
                LineTotal = li.LineTotal
            }).ToList()
        };
    }
}
