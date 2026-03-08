/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// LABOR CONTROLLER — Wages, Trades, and Production Rates
// ============================================================================
//
// THE SMART RATE LOOKUP:
//   User selects a CSI code (e.g., "09 29 00 Gypsum Board")
//   and a project location (e.g., "Los Angeles, CA, Prevailing Wage")
//   
//   The system automatically finds:
//   - The correct TRADE (Drywall Installer)
//   - The correct LABOR RATE ($65.42/hr for LA County prevailing wage)
//   - The correct PRODUCTION RATE (0.017 hrs/SF)
//   - Pre-calculates: LABOR COST PER UNIT ($1.11/SF)
//
//   This eliminates the #1 source of estimating errors:
//   using the wrong wage rate or production rate.
//
// JERP CONNECTION:
//   LaborRate feeds into JERP Payroll (Employee.HourlyRate)
//   When a project is awarded, the labor rates become payroll rates.
//   BuildEstimate ESTIMATES the cost → JERP PAYS it.
//
// ============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BuildEstimate.Application.DTOs;
using BuildEstimate.Core.Entities;
using BuildEstimate.Infrastructure.Data;

namespace BuildEstimate.Api.Controllers;

[Route("api/v1/labor")]
[AllowAnonymous]
public class LaborController : BaseApiController
{
    private readonly BuildEstimateDbContext _context;
    private readonly ILogger<LaborController> _logger;

    public LaborController(
        BuildEstimateDbContext context,
        ILogger<LaborController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =================================================================
    // ████████╗██████╗  █████╗ ██████╗ ███████╗███████╗
    //    ██╔══╝██╔══██╗██╔══██╗██╔══██╗██╔════╝██╔════╝
    //    ██║   ██████╔╝███████║██║  ██║█████╗  ███████╗
    //    ██║   ██╔══██╗██╔══██║██║  ██║██╔══╝  ╚════██║
    //    ██║   ██║  ██║██║  ██║██████╔╝███████╗███████║
    //    ╚═╝   ╚═╝  ╚═╝╚═╝  ╚═╝╚═════╝ ╚══════╝╚══════╝
    // =================================================================

    // GET /api/v1/labor/trades
    [HttpGet("trades")]
    public async Task<IActionResult> GetTrades()
    {
        var trades = await _context.Trades
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new TradeDto
            {
                Id = t.Id,
                Name = t.Name,
                TradeCode = t.TradeCode,
                Description = t.Description,
                UnionAffiliation = t.UnionAffiliation,
                IsActive = t.IsActive,
                LaborRateCount = t.LaborRates.Count,
                ProductionRateCount = _context.ProductionRates.Count(pr => pr.TradeId == t.Id)
            })
            .ToListAsync();

        return Ok(trades);
    }

    // GET /api/v1/labor/trades/{id}
    [HttpGet("trades/{id}")]
    public async Task<IActionResult> GetTrade(Guid id)
    {
        var trade = await _context.Trades
            .Where(t => t.Id == id)
            .Select(t => new TradeDto
            {
                Id = t.Id,
                Name = t.Name,
                TradeCode = t.TradeCode,
                Description = t.Description,
                UnionAffiliation = t.UnionAffiliation,
                IsActive = t.IsActive,
                LaborRateCount = t.LaborRates.Count,
                ProductionRateCount = _context.ProductionRates.Count(pr => pr.TradeId == t.Id)
            })
            .FirstOrDefaultAsync();

        if (trade == null)
            return NotFound($"Trade with ID {id} not found");

        return Ok(trade);
    }

    // POST /api/v1/labor/trades
    [HttpPost("trades")]
    public async Task<IActionResult> CreateTrade([FromBody] CreateTradeRequest request)
    {
        var trade = new Trade
        {
            Name = request.Name.Trim(),
            TradeCode = request.TradeCode?.Trim().ToUpper(),
            Description = request.Description?.Trim(),
            UnionAffiliation = request.UnionAffiliation?.Trim()
        };

        _context.Trades.Add(trade);
        await _context.SaveChangesAsync();

        return Created(new TradeDto
        {
            Id = trade.Id,
            Name = trade.Name,
            TradeCode = trade.TradeCode,
            Description = trade.Description,
            UnionAffiliation = trade.UnionAffiliation,
            IsActive = true,
            LaborRateCount = 0,
            ProductionRateCount = 0
        });
    }

    // =================================================================
    // ██╗      █████╗ ██████╗  ██████╗ ██████╗     ██████╗  █████╗ ████████╗███████╗███████╗
    // ██║     ██╔══██╗██╔══██╗██╔═══██╗██╔══██╗    ██╔══██╗██╔══██╗╚══██╔══╝██╔════╝██╔════╝
    // ██║     ███████║██████╔╝██║   ██║██████╔╝    ██████╔╝███████║   ██║   █████╗  ███████╗
    // ██║     ██╔══██║██╔══██╗██║   ██║██╔══██╗    ██╔══██╗██╔══██║   ██║   ██╔══╝  ╚════██║
    // ███████╗██║  ██║██████╔╝╚██████╔╝██║  ██║    ██║  ██║██║  ██║   ██║   ███████╗███████║
    // ╚══════╝╚═╝  ╚═╝╚═════╝  ╚═════╝ ╚═╝  ╚═╝    ╚═╝  ╚═╝╚═╝  ╚═╝   ╚═╝   ╚══════╝╚══════╝
    // =================================================================

    // GET /api/v1/labor/rates?tradeId=&county=&state=&rateType=
    [HttpGet("rates")]
    public async Task<IActionResult> GetLaborRates(
        [FromQuery] Guid? tradeId = null,
        [FromQuery] string? county = null,
        [FromQuery] string? state = null,
        [FromQuery] string? rateType = null)
    {
        var query = _context.LaborRates
            .Include(r => r.Trade)
            .Where(r => r.IsActive)
            .AsQueryable();

        if (tradeId.HasValue)
            query = query.Where(r => r.TradeId == tradeId.Value);
        if (!string.IsNullOrEmpty(county))
            query = query.Where(r => r.County.Contains(county));
        if (!string.IsNullOrEmpty(state))
            query = query.Where(r => r.State == state.ToUpper());
        if (!string.IsNullOrEmpty(rateType))
            query = query.Where(r => r.RateType == rateType);

        var rates = await query
            .OrderBy(r => r.Trade!.Name)
            .ThenBy(r => r.State)
            .ThenBy(r => r.County)
            .Select(r => new LaborRateDto
            {
                Id = r.Id,
                TradeId = r.TradeId,
                TradeName = r.Trade != null ? r.Trade.Name : "",
                County = r.County,
                State = r.State,
                BaseWage = r.BaseWage,
                HealthWelfare = r.HealthWelfare,
                Pension = r.Pension,
                VacationHoliday = r.VacationHoliday,
                Training = r.Training,
                OtherFringe = r.OtherFringe,
                TotalRate = r.TotalRate,
                RateType = r.RateType,
                OvertimeRate = r.OvertimeRate,
                DoubleTimeRate = r.DoubleTimeRate,
                EffectiveDate = r.EffectiveDate,
                ExpirationDate = r.ExpirationDate,
                Source = r.Source,
                IsActive = r.IsActive
            })
            .ToListAsync();

        return Ok(rates);
    }

    // POST /api/v1/labor/rates
    [HttpPost("rates")]
    public async Task<IActionResult> CreateLaborRate([FromBody] CreateLaborRateRequest request)
    {
        var trade = await _context.Trades.FindAsync(request.TradeId);
        if (trade == null)
            return BadRequest($"Trade with ID {request.TradeId} not found");

        // Auto-calculate total rate and overtime rates
        var totalRate = request.BaseWage + request.HealthWelfare + request.Pension
                      + request.VacationHoliday + request.Training + request.OtherFringe;

        var overtimeRate = (request.BaseWage * 1.5m) + request.HealthWelfare
                         + request.Pension + request.VacationHoliday
                         + request.Training + request.OtherFringe;

        var doubleTimeRate = (request.BaseWage * 2.0m) + request.HealthWelfare
                           + request.Pension + request.VacationHoliday
                           + request.Training + request.OtherFringe;

        var rate = new LaborRate
        {
            TradeId = request.TradeId,
            County = request.County.Trim(),
            State = request.State.Trim().ToUpper(),
            BaseWage = request.BaseWage,
            HealthWelfare = request.HealthWelfare,
            Pension = request.Pension,
            VacationHoliday = request.VacationHoliday,
            Training = request.Training,
            OtherFringe = request.OtherFringe,
            TotalRate = Math.Round(totalRate, 2),
            RateType = request.RateType.Trim(),
            OvertimeRate = Math.Round(overtimeRate, 2),
            DoubleTimeRate = Math.Round(doubleTimeRate, 2),
            EffectiveDate = request.EffectiveDate ?? DateTime.UtcNow,
            ExpirationDate = request.ExpirationDate,
            Source = request.Source?.Trim()
        };

        _context.LaborRates.Add(rate);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created labor rate: {Trade} in {County}, {State} = {Rate:C}/hr ({Type})",
            trade.Name, rate.County, rate.State, rate.TotalRate, rate.RateType);

        return Created(new LaborRateDto
        {
            Id = rate.Id,
            TradeId = rate.TradeId,
            TradeName = trade.Name,
            County = rate.County,
            State = rate.State,
            BaseWage = rate.BaseWage,
            HealthWelfare = rate.HealthWelfare,
            Pension = rate.Pension,
            VacationHoliday = rate.VacationHoliday,
            Training = rate.Training,
            OtherFringe = rate.OtherFringe,
            TotalRate = rate.TotalRate,
            RateType = rate.RateType,
            OvertimeRate = rate.OvertimeRate,
            DoubleTimeRate = rate.DoubleTimeRate,
            EffectiveDate = rate.EffectiveDate,
            ExpirationDate = rate.ExpirationDate,
            Source = rate.Source,
            IsActive = true
        });
    }

    // =================================================================
    // ██████╗ ██████╗  ██████╗ ██████╗ ██╗   ██╗ ██████╗████████╗██╗ ██████╗ ███╗   ██╗
    // ██╔══██╗██╔══██╗██╔═══██╗██╔══██╗██║   ██║██╔════╝╚══██╔══╝██║██╔═══██╗████╗  ██║
    // ██████╔╝██████╔╝██║   ██║██║  ██║██║   ██║██║        ██║   ██║██║   ██║██╔██╗ ██║
    // ██╔═══╝ ██╔══██╗██║   ██║██║  ██║██║   ██║██║        ██║   ██║██║   ██║██║╚██╗██║
    // ██║     ██║  ██║╚██████╔╝██████╔╝╚██████╔╝╚██████╗   ██║   ██║╚██████╔╝██║ ╚████║
    // ╚═╝     ╚═╝  ╚═╝ ╚═════╝ ╚═════╝  ╚═════╝  ╚═════╝   ╚═╝   ╚═╝ ╚═════╝ ╚═╝  ╚═══╝
    // =================================================================

    // GET /api/v1/labor/production-rates?csiSectionId=&tradeId=
    [HttpGet("production-rates")]
    public async Task<IActionResult> GetProductionRates(
        [FromQuery] Guid? csiSectionId = null,
        [FromQuery] Guid? tradeId = null)
    {
        var query = _context.ProductionRates
            .Include(pr => pr.CSISection)
            .Include(pr => pr.Trade)
            .Where(pr => pr.IsActive)
            .AsQueryable();

        if (csiSectionId.HasValue)
            query = query.Where(pr => pr.CSISectionId == csiSectionId.Value);
        if (tradeId.HasValue)
            query = query.Where(pr => pr.TradeId == tradeId.Value);

        var rates = await query
            .OrderBy(pr => pr.CSISection!.Code)
            .Select(pr => new ProductionRateDto
            {
                Id = pr.Id,
                CSISectionId = pr.CSISectionId,
                CSICode = pr.CSISection != null ? pr.CSISection.Code : "",
                CSISectionName = pr.CSISection != null ? pr.CSISection.Name : "",
                TradeId = pr.TradeId,
                TradeName = pr.Trade != null ? pr.Trade.Name : "",
                Description = pr.Description,
                HoursPerUnit = pr.HoursPerUnit,
                UnitOfMeasure = pr.UnitOfMeasure,
                CrewSize = pr.CrewSize,
                DailyOutput = pr.DailyOutput,
                Source = pr.Source,
                ConditionFactor = pr.ConditionFactor,
                Notes = pr.Notes,
                IsActive = pr.IsActive
            })
            .ToListAsync();

        return Ok(rates);
    }

    // POST /api/v1/labor/production-rates
    [HttpPost("production-rates")]
    public async Task<IActionResult> CreateProductionRate([FromBody] CreateProductionRateRequest request)
    {
        var section = await _context.CSISections.FindAsync(request.CSISectionId);
        if (section == null)
            return BadRequest($"CSI Section with ID {request.CSISectionId} not found");

        var trade = await _context.Trades.FindAsync(request.TradeId);
        if (trade == null)
            return BadRequest($"Trade with ID {request.TradeId} not found");

        // Auto-calculate daily output if not provided
        var dailyOutput = request.DailyOutput;
        if (dailyOutput == 0 && request.HoursPerUnit > 0)
        {
            // 8-hour day ÷ hours per unit × crew size = daily output
            dailyOutput = Math.Round(
                (8.0m / request.HoursPerUnit) * request.CrewSize, 2);
        }

        var rate = new ProductionRate
        {
            CSISectionId = request.CSISectionId,
            TradeId = request.TradeId,
            Description = request.Description.Trim(),
            HoursPerUnit = request.HoursPerUnit,
            UnitOfMeasure = request.UnitOfMeasure.Trim().ToUpper(),
            CrewSize = request.CrewSize,
            DailyOutput = dailyOutput,
            Source = request.Source.Trim(),
            ConditionFactor = request.ConditionFactor?.Trim(),
            Notes = request.Notes?.Trim()
        };

        _context.ProductionRates.Add(rate);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created production rate: {Description} = {Rate} hrs/{UOM} ({Source})",
            rate.Description, rate.HoursPerUnit, rate.UnitOfMeasure, rate.Source);

        return Created(new ProductionRateDto
        {
            Id = rate.Id,
            CSISectionId = rate.CSISectionId,
            CSICode = section.Code,
            CSISectionName = section.Name,
            TradeId = rate.TradeId,
            TradeName = trade.Name,
            Description = rate.Description,
            HoursPerUnit = rate.HoursPerUnit,
            UnitOfMeasure = rate.UnitOfMeasure,
            CrewSize = rate.CrewSize,
            DailyOutput = rate.DailyOutput,
            Source = rate.Source,
            ConditionFactor = rate.ConditionFactor,
            Notes = rate.Notes,
            IsActive = true
        });
    }

    // =================================================================
    //  ██████╗  █████╗ ████████╗███████╗    ██╗      ██████╗  ██████╗ ██╗  ██╗██╗   ██╗██████╗
    //  ██╔══██╗██╔══██╗╚══██╔══╝██╔════╝    ██║     ██╔═══██╗██╔═══██╗██║ ██╔╝██║   ██║██╔══██╗
    //  ██████╔╝███████║   ██║   █████╗      ██║     ██║   ██║██║   ██║█████╔╝ ██║   ██║██████╔╝
    //  ██╔══██╗██╔══██║   ██║   ██╔══╝      ██║     ██║   ██║██║   ██║██╔═██╗ ██║   ██║██╔═══╝
    //  ██║  ██║██║  ██║   ██║   ███████╗    ███████╗╚██████╔╝╚██████╔╝██║  ██╗╚██████╔╝██║
    //  ╚═╝  ╚═╝╚═╝  ╚═╝   ╚═╝   ╚══════╝    ╚══════╝ ╚═════╝  ╚═════╝ ╚═╝  ╚═╝ ╚═════╝ ╚═╝
    // =================================================================

    // =====================================================================
    // GET /api/v1/labor/lookup?csiSectionId=&county=&state=&isPrevailingWage=
    // =====================================================================
    //
    // THE SMART RATE LOOKUP — The most powerful endpoint in Phase 4.
    //
    // Given a CSI code and project location, it finds:
    //   1. Which TRADE does this work (via ProductionRate → TradeId)
    //   2. What's the WAGE in this location for that trade
    //   3. What's the PRODUCTION RATE for this work
    //   4. Pre-calculates the LABOR COST PER UNIT
    //
    // This is like asking: "What does drywall cost per SF in Los Angeles?"
    // Answer: 0.017 hrs/SF × $65.42/hr = $1.11/SF
    //
    // The estimator can then plug this into the line item without
    // manually looking up wages and production rates.
    // =====================================================================

    [HttpGet("lookup")]
    public async Task<IActionResult> LookupRates(
        [FromQuery] Guid csiSectionId,
        [FromQuery] string county,
        [FromQuery] string state,
        [FromQuery] bool isPrevailingWage = false)
    {
        // Step 1: Find the production rate for this CSI section
        var productionRate = await _context.ProductionRates
            .Include(pr => pr.CSISection)
            .Include(pr => pr.Trade)
            .Where(pr => pr.CSISectionId == csiSectionId && pr.IsActive)
            .FirstOrDefaultAsync();

        if (productionRate == null)
        {
            return Ok(new RateLookupResultDto
            {
                Message = "No production rate found for this CSI section. Add one first."
            });
        }

        // Step 2: Find the labor rate for this trade + location
        var rateType = isPrevailingWage ? "Prevailing" : "Market";

        var laborRate = await _context.LaborRates
            .Include(r => r.Trade)
            .Where(r => r.TradeId == productionRate.TradeId
                     && r.County.Contains(county)
                     && r.State == state.ToUpper()
                     && r.RateType == rateType
                     && r.IsActive)
            .FirstOrDefaultAsync();

        // Fall back to market rate if no prevailing wage found
        string message;
        if (laborRate == null && isPrevailingWage)
        {
            laborRate = await _context.LaborRates
                .Include(r => r.Trade)
                .Where(r => r.TradeId == productionRate.TradeId
                         && r.County.Contains(county)
                         && r.State == state.ToUpper()
                         && r.IsActive)
                .FirstOrDefaultAsync();

            message = laborRate != null
                ? $"No prevailing wage found — using {laborRate.RateType} rate instead"
                : $"No labor rate found for {productionRate.Trade?.Name} in {county}, {state}";
        }
        else if (laborRate != null)
        {
            message = $"Using {rateType} rate for {productionRate.Trade?.Name} in {county} County, {state}";
        }
        else
        {
            message = $"No labor rate found for {productionRate.Trade?.Name} in {county}, {state}. Add one first.";
        }

        // Step 3: Calculate labor cost per unit
        decimal laborCostPerUnit = 0;
        if (laborRate != null)
        {
            laborCostPerUnit = Math.Round(
                productionRate.HoursPerUnit * laborRate.TotalRate, 4);
        }

        return Ok(new RateLookupResultDto
        {
            LaborRate = laborRate != null ? new LaborRateDto
            {
                Id = laborRate.Id,
                TradeId = laborRate.TradeId,
                TradeName = laborRate.Trade?.Name ?? "",
                County = laborRate.County,
                State = laborRate.State,
                BaseWage = laborRate.BaseWage,
                HealthWelfare = laborRate.HealthWelfare,
                Pension = laborRate.Pension,
                VacationHoliday = laborRate.VacationHoliday,
                Training = laborRate.Training,
                OtherFringe = laborRate.OtherFringe,
                TotalRate = laborRate.TotalRate,
                RateType = laborRate.RateType,
                OvertimeRate = laborRate.OvertimeRate,
                DoubleTimeRate = laborRate.DoubleTimeRate,
                EffectiveDate = laborRate.EffectiveDate,
                ExpirationDate = laborRate.ExpirationDate,
                Source = laborRate.Source,
                IsActive = laborRate.IsActive
            } : null,
            ProductionRate = new ProductionRateDto
            {
                Id = productionRate.Id,
                CSISectionId = productionRate.CSISectionId,
                CSICode = productionRate.CSISection?.Code ?? "",
                CSISectionName = productionRate.CSISection?.Name ?? "",
                TradeId = productionRate.TradeId,
                TradeName = productionRate.Trade?.Name ?? "",
                Description = productionRate.Description,
                HoursPerUnit = productionRate.HoursPerUnit,
                UnitOfMeasure = productionRate.UnitOfMeasure,
                CrewSize = productionRate.CrewSize,
                DailyOutput = productionRate.DailyOutput,
                Source = productionRate.Source,
                ConditionFactor = productionRate.ConditionFactor,
                Notes = productionRate.Notes,
                IsActive = productionRate.IsActive
            },
            LaborCostPerUnit = laborCostPerUnit,
            Message = message
        });
    }
}
