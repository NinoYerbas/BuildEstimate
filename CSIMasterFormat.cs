/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// LABOR AND PRODUCTION RATE DTOs
// ============================================================================
//
// This file defines the API shapes (DTOs) for the labor and production data
// that powers construction cost estimating.
//
// THREE KEY CONCEPTS IN CONSTRUCTION PRICING:
//
//   TRADE — Who does the work?
//     "Carpenter", "Electrician", "Drywall Installer"
//     Each trade has its own hourly wage rates.
//
//   LABOR RATE — How much does the trade earn per hour?
//     Varies by: county, state, union vs. non-union, prevailing wage vs. market rate
//     Example: Electrician in Los Angeles County = $89.47/hr (prevailing wage)
//              Electrician in Fresno County = $62.15/hr (market rate)
//
//   PRODUCTION RATE — How fast does the trade work?
//     Measured in HOURS PER UNIT of work (e.g., 0.017 hrs/SF for drywall)
//     Multiply by labor rate to get COST PER UNIT:
//       0.017 hrs/SF × $65/hr = $1.11/SF labor cost
//
// RATE LOOKUP — Combines a labor rate + production rate into a ready-to-use
//   cost per unit for a specific CSI code in a specific location.
//
// ============================================================================

namespace BuildEstimate.Application.DTOs;

// =====================================================================
// TRADE DTOs
// =====================================================================

public class TradeDto
{
    /// <summary>Unique identifier for this trade record in the database.</summary>
    public Guid Id { get; set; }

    /// <summary>The human-readable name of the trade, e.g., "Carpenter" or "Electrician".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short code for the trade, e.g., "CARP" or "ELEC". Used for quick lookup and display.</summary>
    public string? TradeCode { get; set; }

    /// <summary>Optional longer description of the trade's scope of work.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// The union or collective bargaining agreement this trade is affiliated with, if any.
    /// Example: "IBEW Local 11" (electricians' union in Los Angeles).
    /// Affects whether prevailing wage rates apply.
    /// </summary>
    public string? UnionAffiliation { get; set; }

    /// <summary>Whether this trade is currently active and available for use in new estimates.</summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// How many labor rate records exist for this trade.
    /// Included in the DTO so the UI can show "12 rates" without a second API call.
    /// </summary>
    public int LaborRateCount { get; set; }

    /// <summary>How many production rate records exist for this trade across all CSI sections.</summary>
    public int ProductionRateCount { get; set; }
}

/// <summary>
/// Data the API receives when a user creates a new trade.
/// Only includes fields the user should set — Id, active status, and rate counts are managed automatically.
/// </summary>
public class CreateTradeRequest
{
    /// <summary>The display name of the new trade (required).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional short code for the trade, e.g., "CARP". Must be unique if provided.</summary>
    public string? TradeCode { get; set; }

    /// <summary>Optional description of the trade's work scope.</summary>
    public string? Description { get; set; }

    /// <summary>Optional union or collective bargaining agreement affiliation.</summary>
    public string? UnionAffiliation { get; set; }
}

// =====================================================================
// LABOR RATE DTOs
// =====================================================================

/// <summary>
/// A labor rate for a specific trade in a specific county and state.
/// Labor costs vary significantly by location and by project type
/// (prevailing wage projects pay government-mandated rates — typically 20–40% higher than market).
///
/// The total hourly cost = BaseWage + HealthWelfare + Pension + VacationHoliday + Training + OtherFringe.
/// This total rate is what you multiply by labor hours to get the total labor cost for a line item.
/// </summary>
public class LaborRateDto
{
    /// <summary>Unique identifier for this labor rate record.</summary>
    public Guid Id { get; set; }

    /// <summary>The trade this rate applies to.</summary>
    public Guid TradeId { get; set; }

    /// <summary>Human-readable trade name, joined from the Trade table for convenience.</summary>
    public string TradeName { get; set; } = string.Empty;

    /// <summary>The county where this rate applies, e.g., "Los Angeles".</summary>
    public string County { get; set; } = string.Empty;

    /// <summary>The two-letter state abbreviation, e.g., "CA".</summary>
    public string State { get; set; } = string.Empty;

    // Wage breakdown — all components that make up the total hourly cost
    /// <summary>The straight-time hourly wage paid directly to the worker.</summary>
    public decimal BaseWage { get; set; }

    /// <summary>Employer-paid health and welfare benefit contribution per hour worked.</summary>
    public decimal HealthWelfare { get; set; }

    /// <summary>Employer-paid pension (retirement) contribution per hour worked.</summary>
    public decimal Pension { get; set; }

    /// <summary>Employer-paid vacation and holiday accrual per hour worked.</summary>
    public decimal VacationHoliday { get; set; }

    /// <summary>Employer-paid apprenticeship and journeyman training fund contribution.</summary>
    public decimal Training { get; set; }

    /// <summary>Any additional fringe benefit contributions not covered by the other fields.</summary>
    public decimal OtherFringe { get; set; }

    /// <summary>
    /// The all-in hourly cost: BaseWage + all fringe benefits.
    /// This is the number you multiply by labor hours to calculate total labor cost.
    /// </summary>
    public decimal TotalRate { get; set; }

    /// <summary>
    /// Whether this is a "Prevailing" (government-mandated) or "Market" (negotiated) rate.
    /// Prevailing wage is required on most public works projects.
    /// </summary>
    public string RateType { get; set; } = string.Empty;

    /// <summary>The overtime rate (typically 1.5× base wage). Applied after 8 hours/day or 40 hours/week.</summary>
    public decimal OvertimeRate { get; set; }

    /// <summary>The double-time rate (typically 2× base wage). Applied on Sundays and holidays.</summary>
    public decimal DoubleTimeRate { get; set; }

    /// <summary>The date this rate took effect — wage agreements are time-limited and renegotiated regularly.</summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>The date this rate expires (null = no expiration). After this date, a new rate should be used.</summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>Where this rate came from, e.g., "California DIR", "Union CBA 2024", "RSMeans".</summary>
    public string? Source { get; set; }

    /// <summary>Whether this rate is currently active. Expired rates are deactivated, not deleted.</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Data the API receives when creating a new labor rate record.
/// Fringe benefit fields default to 0 so the caller only needs to supply what's known.
/// TotalRate is calculated automatically from the component fields — do not set it directly.
/// </summary>
public class CreateLaborRateRequest
{
    /// <summary>The trade this rate applies to (required).</summary>
    public Guid TradeId { get; set; }

    /// <summary>The county this rate applies to, e.g., "Los Angeles".</summary>
    public string County { get; set; } = string.Empty;

    /// <summary>The state abbreviation. Defaults to "CA" (California) since the app is CA-focused.</summary>
    public string State { get; set; } = "CA";

    /// <summary>The straight-time hourly base wage (required).</summary>
    public decimal BaseWage { get; set; }

    /// <summary>Health and welfare benefit contribution per hour. Defaults to 0 if not union.</summary>
    public decimal HealthWelfare { get; set; } = 0;

    /// <summary>Pension contribution per hour. Defaults to 0 if not union.</summary>
    public decimal Pension { get; set; } = 0;

    /// <summary>Vacation and holiday accrual per hour. Defaults to 0.</summary>
    public decimal VacationHoliday { get; set; } = 0;

    /// <summary>Training fund contribution per hour. Defaults to 0.</summary>
    public decimal Training { get; set; } = 0;

    /// <summary>Other fringe benefits per hour. Defaults to 0.</summary>
    public decimal OtherFringe { get; set; } = 0;

    /// <summary>"Market" for non-union rates, "Prevailing" for government-mandated rates.</summary>
    public string RateType { get; set; } = "Market";

    /// <summary>When this rate goes into effect. Null means effective immediately.</summary>
    public DateTime? EffectiveDate { get; set; }

    /// <summary>When this rate expires. Null means no expiration.</summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>Optional source citation for this rate, e.g., "California DIR 2024" or "Union CBA".</summary>
    public string? Source { get; set; }
}

// =====================================================================
// PRODUCTION RATE DTOs
// =====================================================================

/// <summary>
/// A production rate tells you how fast a specific trade can complete a specific type of work.
/// It is measured in HOURS PER UNIT — how many hours it takes to install one unit of work.
///
/// Example: A drywall installer hangs 0.017 hours per SF (= 1 hour per 59 SF).
///   At $65/hr: 0.017 × $65 = $1.11 per SF in labor cost.
///
/// Production rates are linked to a CSI section (WHAT is being installed) and
/// a trade (WHO is doing the work). They come from industry sources like RSMeans.
/// </summary>
public class ProductionRateDto
{
    /// <summary>Unique identifier for this production rate record.</summary>
    public Guid Id { get; set; }

    /// <summary>The CSI section this rate applies to (e.g., "09 29 00 — Gypsum Board").</summary>
    public Guid CSISectionId { get; set; }

    /// <summary>The CSI code, e.g., "09 29 00". Joined from the CSISection table.</summary>
    public string CSICode { get; set; } = string.Empty;

    /// <summary>The human-readable CSI section name, e.g., "Gypsum Board (Drywall)".</summary>
    public string CSISectionName { get; set; } = string.Empty;

    /// <summary>The trade that performs this work (e.g., Drywall Installer).</summary>
    public Guid TradeId { get; set; }

    /// <summary>Human-readable trade name, joined from the Trade table.</summary>
    public string TradeName { get; set; } = string.Empty;

    /// <summary>A short description of the specific work activity, e.g., "Hang 5/8\" Type-X drywall on metal studs".</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// How many labor hours it takes to install ONE unit of work.
    /// This is the core productivity metric. Lower = faster workers.
    /// Example: 0.017 hrs/SF means a worker installs 59 SF per hour.
    /// </summary>
    public decimal HoursPerUnit { get; set; }

    /// <summary>The unit of measure for this rate (e.g., "SF", "LF", "EA", "CY").</summary>
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>How many workers are in a typical crew for this task. Used for scheduling calculations.</summary>
    public int CrewSize { get; set; }

    /// <summary>
    /// How many units a full crew can complete in an 8-hour day.
    /// DailyOutput = (8 hours × CrewSize) / HoursPerUnit
    /// </summary>
    public decimal DailyOutput { get; set; }

    /// <summary>Where this rate comes from, e.g., "RSMeans", "Historical Project Data", "Subcontractor Quote".</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Optional adjustment factor for difficult conditions.
    /// Example: "Overhead work" might have a 1.3 factor (30% slower than normal).
    /// </summary>
    public string? ConditionFactor { get; set; }

    /// <summary>Additional notes about when this rate applies or how it was derived.</summary>
    public string? Notes { get; set; }

    /// <summary>Whether this rate is currently active and usable in estimates.</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Data the API receives when creating a new production rate record.
/// CSISectionId and TradeId are required to establish the link between "what" and "who".
/// </summary>
public class CreateProductionRateRequest
{
    /// <summary>The CSI section this rate applies to (required).</summary>
    public Guid CSISectionId { get; set; }

    /// <summary>The trade that performs this work (required).</summary>
    public Guid TradeId { get; set; }

    /// <summary>Short description of the specific work activity being rated.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Labor hours per unit of work — the core productivity number (required).</summary>
    public decimal HoursPerUnit { get; set; }

    /// <summary>Unit of measure for this rate. Defaults to "SF" (square feet).</summary>
    public string UnitOfMeasure { get; set; } = "SF";

    /// <summary>Typical crew size for this task. Defaults to 1 worker.</summary>
    public int CrewSize { get; set; } = 1;

    /// <summary>Units completed per 8-hour crew-day. Can be left at 0 to be calculated later.</summary>
    public decimal DailyOutput { get; set; } = 0;

    /// <summary>Source of this productivity data. Defaults to "RSMeans" (the industry standard cost database).</summary>
    public string Source { get; set; } = "RSMeans";

    /// <summary>Optional condition adjustment, e.g., "1.3 for overhead work" or "0.85 for tight spaces".</summary>
    public string? ConditionFactor { get; set; }

    /// <summary>Additional notes about the rate or its applicability.</summary>
    public string? Notes { get; set; }
}

// =====================================================================
// RATE LOOKUP — What you get when pricing a line item
// =====================================================================
// "I need to price drywall in Los Angeles on a prevailing wage project"
// The system returns: LaborRate = $65.42/hr, ProductionRate = 0.017 hrs/SF

/// <summary>
/// The combined result when looking up pricing for a specific CSI section + trade + location combination.
/// This is a convenience object that wraps both the labor rate and production rate in one response,
/// and also pre-calculates the ready-to-use cost per unit so the caller doesn't have to do the math.
///
/// Example usage: "What does it cost to install drywall in LA?"
///   LaborRate.TotalRate = $65.42/hr
///   ProductionRate.HoursPerUnit = 0.017 hrs/SF
///   LaborCostPerUnit = $65.42 × 0.017 = $1.11/SF  ← just use this number
/// </summary>
public class RateLookupResultDto
{
    /// <summary>
    /// The matched labor rate for the requested trade and location.
    /// Null if no rate exists for this trade/location combination.
    /// </summary>
    public LaborRateDto? LaborRate { get; set; }

    /// <summary>
    /// The matched production rate for the requested CSI section and trade.
    /// Null if no production rate has been recorded for this combination.
    /// </summary>
    public ProductionRateDto? ProductionRate { get; set; }

    // Pre-calculated for convenience
    /// <summary>
    /// Ready-to-use labor cost per unit of work.
    /// Formula: ProductionRate.HoursPerUnit × LaborRate.TotalRate
    /// Example: 0.017 hrs/SF × $65.42/hr = $1.11/SF
    /// Copy this directly into the LaborRate field on your estimate line item.
    /// </summary>
    public decimal LaborCostPerUnit { get; set; }

    /// <summary>
    /// A human-readable explanation of what rate was found and why.
    /// Example: "Using prevailing wage rate for Los Angeles County"
    ///          "No prevailing wage found — falling back to market rate"
    ///          "No labor rate found for this trade in this location"
    /// </summary>
    public string? Message { get; set; }
}
