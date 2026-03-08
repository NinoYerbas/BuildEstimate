/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// PHASE 7: REPORTS — Making It Presentable
// ============================================================================
//
// AN ESTIMATE IS USELESS IF YOU CAN'T PRESENT IT.
//
// Reports are how contractors communicate with:
//   1. PROJECT OWNERS — Bid Proposal Sheet (the price you submit)
//   2. MANAGEMENT — Cost Summary (internal review before submitting)
//   3. SUBCONTRACTORS — Scope Breakdown (what you need them to bid)
//   4. ACCOUNTING — Labor Analysis (hours for payroll planning)
//
// Every number in these reports comes from the calculation engine.
// Reports don't calculate — they PRESENT what's already calculated.
//
// JERP EQUIVALENT: FinancialReportsController (P&L, Balance Sheet, Trial Balance)
//   Your financial reports pull from JournalEntries and present them.
//   Estimate reports pull from LineItems and present them.
//   Same pattern, different domain.
//
// ============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BuildEstimate.Infrastructure.Data;
using BuildEstimate.Core.Entities;

namespace BuildEstimate.Api.Controllers;

/// <summary>
/// Generates presentation-ready reports from calculated estimate data.
/// All five endpoints are read-only — they never modify any data.
/// Each report formats the same underlying estimate data for a different audience
/// (project owner, management, labor planning, comparison analysis, dashboard).
/// </summary>
[Route("api/v1/reports")]
[AllowAnonymous]
public class ReportsController : BaseApiController
{
    private readonly BuildEstimateDbContext _context;

    /// <summary>
    /// Constructs the controller with the EF Core database context injected.
    /// </summary>
    public ReportsController(BuildEstimateDbContext context)
    {
        _context = context;
    }

    // =====================================================================
    // GET /api/v1/reports/bid-summary/{estimateId}
    // =====================================================================
    // THE BID PROPOSAL — What you submit to the project owner.
    // This is the single most important document in construction.
    // Get it wrong → lose the job (too high) or lose money (too low).
    // =====================================================================

    /// <summary>
    /// Generates the formal Bid Proposal Summary — the document submitted to the project owner.
    /// Groups line item costs by CSI division, shows the complete markup breakdown,
    /// and produces the final bid price.
    /// </summary>
    /// <param name="estimateId">The estimate to generate a bid proposal for.</param>
    /// <returns>A structured bid proposal with project info, cost breakdown by division, and final bid price.</returns>
    [HttpGet("bid-summary/{estimateId}")]
    public async Task<IActionResult> GetBidSummary(Guid estimateId)
    {
        var estimate = await _context.Estimates
            .Include(e => e.Project)
            .Include(e => e.LineItems)
                .ThenInclude(li => li.CSISection)
                    .ThenInclude(s => s!.Division) // ← need division info for grouping
            .FirstOrDefaultAsync(e => e.Id == estimateId);

        if (estimate == null)
            return NotFound($"Estimate with ID {estimateId} not found");

        // Group line items by CSI Division to show which trades cost the most
        var divisionSummaries = estimate.LineItems
            .Where(li => li.CSISection?.Division != null) // ← only lines with a known CSI division
            .GroupBy(li => new
            {
                DivCode = li.CSISection!.Division!.Code,
                DivName = li.CSISection!.Division!.Name
            })
            .Select(g => new
            {
                Division = $"{g.Key.DivCode} - {g.Key.DivName}",
                MaterialTotal = g.Sum(li => li.MaterialTotal),
                LaborTotal = g.Sum(li => li.LaborTotal),
                EquipmentTotal = g.Sum(li => li.EquipmentTotal),
                SubcontractorTotal = g.Sum(li => li.SubcontractorTotal),
                DirectCost = g.Sum(li => li.LineTotal),
                LineItemCount = g.Count()
            })
            .OrderBy(d => d.Division)
            .ToList();

        return Ok(new
        {
            ReportTitle = "BID PROPOSAL SUMMARY",
            GeneratedAt = DateTime.UtcNow,

            Project = new
            {
                estimate.Project?.Name,
                Type = estimate.Project?.Type.ToString(),
                estimate.Project?.Address,
                Location = $"{estimate.Project?.County} County, {estimate.Project?.State}",
                SquareFootage = estimate.Project?.GrossSquareFootage ?? 0m,
                IsPrevailingWage = estimate.Project?.IsPrevailingWage,
                estimate.Project?.BidDueDate
            },

            Estimate = new
            {
                estimate.Version,
                estimate.Description,
                LineItemCount = estimate.LineItems.Count,
                estimate.LastCalculatedAt
            },

            CostByDivision = divisionSummaries,

            DirectCosts = new
            {
                estimate.MaterialTotal,
                estimate.LaborTotal,
                estimate.EquipmentTotal,
                estimate.SubcontractorTotal,
                Total = estimate.DirectCost
            },

            Markups = new
            {
                Overhead = new { estimate.OverheadPercent, estimate.OverheadAmount },
                Profit = new { estimate.ProfitPercent, estimate.ProfitAmount },
                Bond = new { estimate.BondPercent, estimate.BondAmount },
                Tax = new { estimate.TaxPercent, estimate.TaxAmount },
                Contingency = new { estimate.ContingencyPercent, estimate.ContingencyAmount },
                TotalMarkups = estimate.OverheadAmount + estimate.ProfitAmount
                             + estimate.BondAmount + estimate.TaxAmount + estimate.ContingencyAmount
            },

            BidPrice = estimate.TotalBidPrice,
            CostPerSquareFoot = estimate.CostPerSquareFoot ?? 0m
        });
    }

    // =====================================================================
    // GET /api/v1/reports/detailed-cost/{estimateId}
    // =====================================================================
    // INTERNAL COST REPORT — Every line item with full detail.
    // This is what the estimator and project manager review.
    // =====================================================================

    /// <summary>
    /// Generates the Detailed Cost Report showing every line item with full pricing detail.
    /// Items are grouped by CSI division and sorted by CSI code within each division.
    /// Used internally by estimators and project managers for detailed cost review.
    /// </summary>
    /// <param name="estimateId">The estimate to generate a detailed cost report for.</param>
    /// <returns>All line items organized by division with a summary of totals.</returns>
    [HttpGet("detailed-cost/{estimateId}")]
    public async Task<IActionResult> GetDetailedCost(Guid estimateId)
    {
        var estimate = await _context.Estimates
            .Include(e => e.Project)
            .Include(e => e.LineItems)
                .ThenInclude(li => li.CSISection)
                    .ThenInclude(s => s!.Division)
            .FirstOrDefaultAsync(e => e.Id == estimateId);

        if (estimate == null)
            return NotFound($"Estimate with ID {estimateId} not found");

        var groupedItems = estimate.LineItems
            .Where(li => li.CSISection?.Division != null)
            .GroupBy(li => new
            {
                DivCode = li.CSISection!.Division!.Code,
                DivName = li.CSISection!.Division!.Name
            })
            .OrderBy(g => g.Key.DivCode)
            .Select(g => new
            {
                Division = $"{g.Key.DivCode} - {g.Key.DivName}",
                Items = g.OrderBy(li => li.CSISection!.Code).Select(li => new
                {
                    CSICode = li.CSISection!.Code,
                    li.Description,
                    li.Quantity,
                    li.UnitOfMeasure,
                    li.WasteFactor,
                    li.AdjustedQuantity,
                    li.MaterialUnitCost,
                    li.MaterialTotal,
                    li.LaborHoursPerUnit,
                    li.LaborHours,
                    li.LaborRate,
                    li.LaborTotal,
                    li.EquipmentTotal,
                    li.SubcontractorTotal,
                    li.LineTotal,
                    li.TakeoffSource,
                    li.Notes
                }).ToList(),
                Subtotal = g.Sum(li => li.LineTotal)
            })
            .ToList();

        return Ok(new
        {
            ReportTitle = "DETAILED COST REPORT",
            GeneratedAt = DateTime.UtcNow,
            ProjectName = estimate.Project?.Name,
            EstimateVersion = estimate.Version,
            Divisions = groupedItems,
            Summary = new
            {
                estimate.DirectCost,
                estimate.TotalBidPrice,
                estimate.CostPerSquareFoot,
                TotalLaborHours = estimate.LineItems.Sum(li => li.LaborHours) // ← sum across all line items
            }
        });
    }

    // =====================================================================
    // GET /api/v1/reports/labor-analysis/{estimateId}
    // =====================================================================
    // LABOR ANALYSIS — Total hours by trade for payroll planning.
    // When you WIN the job, this tells you how many workers you need.
    //
    // JERP CONNECTION: This feeds directly into payroll planning.
    //   "We need 1,200 hours of drywall work over 10 weeks"
    //   = 3 drywall workers for 10 weeks = 30 week-equivalents of payroll
    // =====================================================================

    /// <summary>
    /// Generates the Labor Analysis report showing total hours and costs by CSI division (trade).
    /// Includes scheduling estimates showing how many calendar days the labor represents
    /// for crews of various sizes, plus a weekly payroll estimate.
    /// </summary>
    /// <param name="estimateId">The estimate to analyze labor for.</param>
    /// <returns>Labor hours and costs by division, scheduling estimates, and a payroll preview.</returns>
    [HttpGet("labor-analysis/{estimateId}")]
    public async Task<IActionResult> GetLaborAnalysis(Guid estimateId)
    {
        var estimate = await _context.Estimates
            .Include(e => e.Project)
            .Include(e => e.LineItems)
                .ThenInclude(li => li.CSISection)
                    .ThenInclude(s => s!.Division)
            .FirstOrDefaultAsync(e => e.Id == estimateId);

        if (estimate == null)
            return NotFound($"Estimate with ID {estimateId} not found");

        var totalLaborHours = estimate.LineItems.Sum(li => li.LaborHours);

        // Group by CSI Division to approximate trades
        var laborByDivision = estimate.LineItems
            .Where(li => li.LaborHours > 0 && li.CSISection?.Division != null)
            .GroupBy(li => new
            {
                DivCode = li.CSISection!.Division!.Code,
                DivName = li.CSISection!.Division!.Name
            })
            .Select(g => new
            {
                Division = $"{g.Key.DivCode} - {g.Key.DivName}",
                TotalHours = g.Sum(li => li.LaborHours),
                TotalCost = g.Sum(li => li.LaborTotal),
                AverageRate = g.Sum(li => li.LaborHours) > 0
                    ? Math.Round(g.Sum(li => li.LaborTotal) / g.Sum(li => li.LaborHours), 2)
                    : 0m,
                PercentOfTotal = totalLaborHours > 0
                    ? Math.Round(g.Sum(li => li.LaborHours) / totalLaborHours * 100, 1)
                    : 0m,
                // Scheduling: how many 8-hour worker-days?
                WorkerDays = Math.Ceiling(g.Sum(li => li.LaborHours) / 8m),
                // With a crew of 2, how many calendar days?
                CalendarDaysWithCrew2 = Math.Ceiling(g.Sum(li => li.LaborHours) / 16m)
            })
            .OrderByDescending(d => d.TotalHours) // ← most labor-intensive trades first
            .ToList();

        return Ok(new
        {
            ReportTitle = "LABOR ANALYSIS",
            GeneratedAt = DateTime.UtcNow,
            ProjectName = estimate.Project?.Name,
            EstimateVersion = estimate.Version,

            TotalLaborHours = totalLaborHours,
            TotalLaborCost = estimate.LaborTotal,
            AverageLaborRate = totalLaborHours > 0
                ? Math.Round(estimate.LaborTotal / totalLaborHours, 2) : 0m, // ← blended rate across all trades

            // Quick scheduling estimate
            Scheduling = new
            {
                TotalWorkerDays = Math.Ceiling(totalLaborHours / 8m), // ← 1 worker working 8-hr days
                WithCrewOf5 = new
                {
                    CalendarDays = Math.Ceiling(totalLaborHours / 40m),  // ← 5 workers × 8 hrs = 40 hrs/day
                    Weeks = Math.Ceiling(totalLaborHours / 200m)         // ← 5 workers × 40 hrs/week
                },
                WithCrewOf10 = new
                {
                    CalendarDays = Math.Ceiling(totalLaborHours / 80m),
                    Weeks = Math.Ceiling(totalLaborHours / 400m)
                }
            },

            ByDivision = laborByDivision,

            // JERP Payroll preview
            PayrollPreview = new
            {
                EstimatedWeeklyPayroll = totalLaborHours > 0
                    ? Math.Round(estimate.LaborTotal / Math.Ceiling(totalLaborHours / 200m), 2)
                    : 0m, // ← total labor ÷ number of weeks at crew of 5
                Note = "Based on crew of 5 workers, 40 hrs/week"
            }
        });
    }

    // =====================================================================
    // GET /api/v1/reports/comparison?projectId={id}
    // =====================================================================
    // ESTIMATE COMPARISON — Compare multiple versions side by side.
    // "Version 1 was $2.1M, Version 2 after VE is $1.85M — 12% savings"
    // =====================================================================

    /// <summary>
    /// Compares all estimate versions for a given project side-by-side.
    /// Also calculates version-to-version deltas showing how the bid price evolved
    /// (e.g., after value engineering or scope changes).
    /// </summary>
    /// <param name="projectId">The project whose estimates to compare.</param>
    /// <returns>All estimate versions plus calculated deltas between consecutive versions.</returns>
    [HttpGet("comparison")]
    public async Task<IActionResult> GetEstimateComparison([FromQuery] Guid projectId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null)
            return NotFound($"Project with ID {projectId} not found");

        var estimates = await _context.Estimates
            .Where(e => e.ProjectId == projectId)
            .Include(e => e.LineItems)
            .OrderBy(e => e.Version) // ← compare in version order
            .ToListAsync();

        if (estimates.Count == 0)
            return Ok(new { Message = "No estimates found for this project" });

        var comparison = estimates.Select(e => new
        {
            e.Version,
            e.Description,
            LineItemCount = e.LineItems.Count,
            e.MaterialTotal,
            e.LaborTotal,
            e.EquipmentTotal,
            e.SubcontractorTotal,
            e.DirectCost,
            e.OverheadAmount,
            e.ProfitAmount,
            e.TotalBidPrice,
            e.CostPerSquareFoot,
            e.IsSubmitted,
            e.LastCalculatedAt
        }).ToList();

        // Calculate deltas between consecutive versions to show how the bid changed
        var deltas = new List<object>();
        for (int i = 1; i < comparison.Count; i++)
        {
            var prev = comparison[i - 1];
            var curr = comparison[i];
            deltas.Add(new
            {
                From = prev.Version,
                To = curr.Version,
                BidPriceChange = curr.TotalBidPrice - prev.TotalBidPrice, // ← positive = more expensive
                PercentChange = prev.TotalBidPrice > 0
                    ? Math.Round((curr.TotalBidPrice - prev.TotalBidPrice) / prev.TotalBidPrice * 100, 1)
                    : 0m,
                MaterialChange = curr.MaterialTotal - prev.MaterialTotal,
                LaborChange = curr.LaborTotal - prev.LaborTotal
            });
        }

        return Ok(new
        {
            ReportTitle = "ESTIMATE COMPARISON",
            GeneratedAt = DateTime.UtcNow,
            ProjectName = project.Name,
            EstimateCount = estimates.Count,
            Estimates = comparison,
            VersionDeltas = deltas
        });
    }

    // =====================================================================
    // GET /api/v1/reports/project-dashboard/{projectId}
    // =====================================================================
    // EXECUTIVE DASHBOARD — Everything about a project at a glance.
    // =====================================================================

    /// <summary>
    /// Returns a comprehensive project dashboard with status, estimate summary, takeoff completion,
    /// and cost breakdown from the most recently calculated estimate.
    /// Designed for a single-screen executive overview of a project.
    /// </summary>
    /// <param name="projectId">The project to generate a dashboard for.</param>
    /// <returns>Project metadata, estimate totals, takeoff progress, and cost type breakdown.</returns>
    [HttpGet("project-dashboard/{projectId}")]
    public async Task<IActionResult> GetProjectDashboard(Guid projectId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null)
            return NotFound($"Project with ID {projectId} not found");

        var estimates = await _context.Estimates
            .Where(e => e.ProjectId == projectId)
            .Include(e => e.LineItems)
            .ToListAsync();

        var takeoffs = await _context.TakeoffItems
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        // The most recently calculated estimate is the "current" version
        var latestEstimate = estimates
            .OrderByDescending(e => e.LastCalculatedAt)
            .FirstOrDefault();

        return Ok(new
        {
            ReportTitle = "PROJECT DASHBOARD",
            GeneratedAt = DateTime.UtcNow,

            Project = new
            {
                project.Name,
                Type = project.Type.ToString(),
                Status = project.Status.ToString(),
                project.Address,
                Location = $"{project.County} County, {project.State}",
                SquareFootage = project.GrossSquareFootage ?? 0m,
                project.IsPrevailingWage,
                project.BidDueDate,
                DaysUntilBid = project.BidDueDate.HasValue
                    ? (project.BidDueDate.Value - DateTime.UtcNow).Days // ← countdown to bid day
                    : (int?)null
            },

            Estimates = new
            {
                Count = estimates.Count,
                LatestVersion = latestEstimate?.Version,
                LatestBidPrice = latestEstimate?.TotalBidPrice ?? 0,
                LatestCostPerSF = latestEstimate?.CostPerSquareFoot ?? 0,
                TotalLineItems = estimates.Sum(e => e.LineItems.Count),
                HasSubmitted = estimates.Any(e => e.IsSubmitted) // ← has any version been submitted?
            },

            Takeoffs = new
            {
                TotalItems = takeoffs.Count,
                LinkedItems = takeoffs.Count(t => t.IsLinkedToEstimate),
                UnlinkedItems = takeoffs.Count(t => !t.IsLinkedToEstimate), // ← measurements not yet priced
                CompletionPercent = takeoffs.Count > 0
                    ? Math.Round((decimal)takeoffs.Count(t => t.IsLinkedToEstimate) / takeoffs.Count * 100, 1)
                    : 0m // ← what % of takeoff has been converted to estimate lines?
            },

            // Cost mix breakdown from the latest estimate — used for pie charts in the UI
            CostBreakdown = latestEstimate != null ? new
            {
                Material = new { Total = latestEstimate.MaterialTotal, Percent = latestEstimate.DirectCost > 0 ? Math.Round(latestEstimate.MaterialTotal / latestEstimate.DirectCost * 100, 1) : 0m },
                Labor = new { Total = latestEstimate.LaborTotal, Percent = latestEstimate.DirectCost > 0 ? Math.Round(latestEstimate.LaborTotal / latestEstimate.DirectCost * 100, 1) : 0m },
                Equipment = new { Total = latestEstimate.EquipmentTotal, Percent = latestEstimate.DirectCost > 0 ? Math.Round(latestEstimate.EquipmentTotal / latestEstimate.DirectCost * 100, 1) : 0m },
                Subcontractor = new { Total = latestEstimate.SubcontractorTotal, Percent = latestEstimate.DirectCost > 0 ? Math.Round(latestEstimate.SubcontractorTotal / latestEstimate.DirectCost * 100, 1) : 0m }
            } : null
        });
    }
}
