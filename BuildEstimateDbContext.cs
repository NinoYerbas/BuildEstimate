/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// AI ASSISTANT CONTROLLER — Claude-Powered Estimate Intelligence
// ============================================================================
//
// This controller exposes five API endpoints that use Claude (Anthropic's AI)
// to analyze a construction estimate and provide expert feedback.
//
// WHY AI FOR ESTIMATING?
//   Construction estimating requires deep domain knowledge:
//   - "Is 0.35/SF for drywall below market?" (yes — market is ~0.52/SF)
//   - "Does a medical office need fire suppression?" (yes, by code)
//   - "Is 40% of cost in one sub too much?" (yes — concentration risk)
//   A senior estimator knows this. Claude can approximate it.
//
// HOW IT WORKS:
//   1. Load the estimate from the database
//   2. Convert it to a plain-text summary (EstimateForAI)
//   3. Send the summary + a specialized prompt to Claude
//   4. Claude responds with structured JSON analysis
//   5. Parse and return the analysis to the caller
//
// ENDPOINTS:
//   POST /api/v1/ai/validate/{estimateId}     → Missing items, wrong quantities
//   POST /api/v1/ai/check-pricing/{estimateId} → Prices vs. market rates
//   POST /api/v1/ai/suggest/{estimateId}       → What assemblies are missing?
//   POST /api/v1/ai/risk/{estimateId}          → Financial and execution risks
//   POST /api/v1/ai/full-review/{estimateId}   → All four analyses at once
//
// ============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BuildEstimate.Application.Services;
using BuildEstimate.Infrastructure.Data;

namespace BuildEstimate.Api.Controllers;

/// <summary>
/// Provides AI-powered estimate analysis using Claude (Anthropic).
/// All endpoints load a saved estimate, convert it to AI-readable format,
/// and return structured findings with severity levels and recommendations.
///
/// Marked [AllowAnonymous] for development convenience — in production,
/// change to [Authorize] to require authentication.
/// </summary>
[Route("api/v1/ai")]
[AllowAnonymous]
public class AIAssistantController : BaseApiController
{
    private readonly BuildEstimateDbContext _context;
    private readonly AIEstimateService _aiService;
    private readonly ILogger<AIAssistantController> _logger;

    /// <summary>
    /// Constructs the controller with database context and AI service injected by the DI container.
    /// </summary>
    /// <param name="context">EF Core database context for loading estimates.</param>
    /// <param name="aiService">The Claude API wrapper that sends prompts and parses responses.</param>
    /// <param name="logger">Structured logger for tracking AI analysis requests.</param>
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

    /// <summary>
    /// Validates an estimate for completeness and accuracy using AI.
    /// Claude checks for: missing CSI sections for the project type, quantities that
    /// seem too high or too low for the project size, and labor rates that don't match
    /// prevailing wage or market rates for the project location.
    /// </summary>
    /// <param name="estimateId">The ID of the estimate to validate.</param>
    /// <returns>An <see cref="AIAnalysisResult"/> with severity-tagged findings and an overall score.</returns>
    [HttpPost("validate/{estimateId}")]
    public async Task<IActionResult> ValidateEstimate(Guid estimateId)
    {
        var data = await LoadEstimateForAI(estimateId); // ← load from DB with related data
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

    /// <summary>
    /// Checks each line item's pricing against current market rates for the project's location.
    /// Claude flags items that are BELOW MARKET (quality risk), AT MARKET (good), or
    /// ABOVE MARKET (negotiation opportunity).
    /// </summary>
    /// <param name="estimateId">The ID of the estimate to price-check.</param>
    /// <returns>An <see cref="AIAnalysisResult"/> with per-item pricing assessments.</returns>
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

    /// <summary>
    /// Suggests additional assemblies or line items that are likely missing from the estimate.
    /// Claude compares the existing CSI sections against what a typical project of this type
    /// and size would require, and recommends what to add.
    /// </summary>
    /// <param name="estimateId">The ID of the estimate to analyze for missing items.</param>
    /// <returns>An <see cref="AIAnalysisResult"/> with recommended additions and estimated impacts.</returns>
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

    /// <summary>
    /// Analyzes the estimate for financial and execution risks.
    /// Claude looks for: cost concentration in a single subcontractor, thin profit margins,
    /// inadequate contingency, and scope items that historically cause cost overruns.
    /// </summary>
    /// <param name="estimateId">The ID of the estimate to risk-analyze.</param>
    /// <returns>An <see cref="AIAnalysisResult"/> with risk findings and mitigation strategies.</returns>
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

    /// <summary>
    /// Runs all four AI analyses (validate, pricing, suggest, risk) in parallel and returns a combined report.
    /// This is more efficient than calling each endpoint separately because all four
    /// Claude API calls happen concurrently using Task.WhenAll.
    /// </summary>
    /// <param name="estimateId">The ID of the estimate to fully review.</param>
    /// <returns>An object containing Validation, Pricing, Suggestions, and Risk results plus a timestamp.</returns>
    [HttpPost("full-review/{estimateId}")]
    public async Task<IActionResult> FullReview(Guid estimateId)
    {
        var data = await LoadEstimateForAI(estimateId);
        if (data == null)
            return NotFound($"Estimate with ID {estimateId} not found");

        _logger.LogInformation("AI full review of estimate {Id}", estimateId);

        // Run all analyses in parallel — each makes an independent Claude API call
        var validateTask = _aiService.ValidateEstimate(data);
        var pricingTask = _aiService.CheckPricing(data);
        var suggestTask = _aiService.SuggestAssemblies(data);
        var riskTask = _aiService.AnalyzeRisk(data);

        await Task.WhenAll(validateTask, pricingTask, suggestTask, riskTask); // ← wait for all four to finish

        return Ok(new
        {
            Validation = await validateTask,   // ← safe to await again after WhenAll
            Pricing = await pricingTask,
            Suggestions = await suggestTask,
            Risk = await riskTask,
            Timestamp = DateTime.UtcNow
        });
    }

    // =====================================================================
    // HELPER — Load estimate data for AI analysis
    // =====================================================================

    /// <summary>
    /// Loads an estimate from the database and converts it to the flat
    /// <see cref="EstimateForAI"/> format that the AI service prompts use.
    /// Returns null if the estimate does not exist.
    /// </summary>
    /// <param name="estimateId">The estimate to load.</param>
    private async Task<EstimateForAI?> LoadEstimateForAI(Guid estimateId)
    {
        var estimate = await _context.Estimates
            .Include(e => e.Project)             // ← need project type, size, location
            .Include(e => e.LineItems)
                .ThenInclude(li => li.CSISection) // ← need CSI codes for the prompt
            .FirstOrDefaultAsync(e => e.Id == estimateId);

        if (estimate == null) return null;

        // Map entity to the simplified AI DTO — only the fields the prompts need
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
