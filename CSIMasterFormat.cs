/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

namespace BuildEstimate.Application.DTOs;

// =====================================================================
// TRADE DTOs
// =====================================================================

public class TradeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TradeCode { get; set; }
    public string? Description { get; set; }
    public string? UnionAffiliation { get; set; }
    public bool IsActive { get; set; }
    public int LaborRateCount { get; set; }
    public int ProductionRateCount { get; set; }
}

public class CreateTradeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? TradeCode { get; set; }
    public string? Description { get; set; }
    public string? UnionAffiliation { get; set; }
}

// =====================================================================
// LABOR RATE DTOs
// =====================================================================

public class LaborRateDto
{
    public Guid Id { get; set; }
    public Guid TradeId { get; set; }
    public string TradeName { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;

    // Wage breakdown
    public decimal BaseWage { get; set; }
    public decimal HealthWelfare { get; set; }
    public decimal Pension { get; set; }
    public decimal VacationHoliday { get; set; }
    public decimal Training { get; set; }
    public decimal OtherFringe { get; set; }
    public decimal TotalRate { get; set; }

    public string RateType { get; set; } = string.Empty;
    public decimal OvertimeRate { get; set; }
    public decimal DoubleTimeRate { get; set; }

    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? Source { get; set; }
    public bool IsActive { get; set; }
}

public class CreateLaborRateRequest
{
    public Guid TradeId { get; set; }
    public string County { get; set; } = string.Empty;
    public string State { get; set; } = "CA";
    public decimal BaseWage { get; set; }
    public decimal HealthWelfare { get; set; } = 0;
    public decimal Pension { get; set; } = 0;
    public decimal VacationHoliday { get; set; } = 0;
    public decimal Training { get; set; } = 0;
    public decimal OtherFringe { get; set; } = 0;
    public string RateType { get; set; } = "Market";
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? Source { get; set; }
}

// =====================================================================
// PRODUCTION RATE DTOs
// =====================================================================

public class ProductionRateDto
{
    public Guid Id { get; set; }
    public Guid CSISectionId { get; set; }
    public string CSICode { get; set; } = string.Empty;
    public string CSISectionName { get; set; } = string.Empty;
    public Guid TradeId { get; set; }
    public string TradeName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public decimal HoursPerUnit { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public int CrewSize { get; set; }
    public decimal DailyOutput { get; set; }

    public string Source { get; set; } = string.Empty;
    public string? ConditionFactor { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public class CreateProductionRateRequest
{
    public Guid CSISectionId { get; set; }
    public Guid TradeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal HoursPerUnit { get; set; }
    public string UnitOfMeasure { get; set; } = "SF";
    public int CrewSize { get; set; } = 1;
    public decimal DailyOutput { get; set; } = 0;
    public string Source { get; set; } = "RSMeans";
    public string? ConditionFactor { get; set; }
    public string? Notes { get; set; }
}

// =====================================================================
// RATE LOOKUP — What you get when pricing a line item
// =====================================================================
// "I need to price drywall in Los Angeles on a prevailing wage project"
// The system returns: LaborRate = $65.42/hr, ProductionRate = 0.017 hrs/SF

public class RateLookupResultDto
{
    public LaborRateDto? LaborRate { get; set; }
    public ProductionRateDto? ProductionRate { get; set; }

    // Pre-calculated for convenience
    public decimal LaborCostPerUnit { get; set; }
    // HoursPerUnit × TotalRate = cost per unit of work
    // 0.017 × $65.42 = $1.11 per SF

    public string? Message { get; set; }
    // "Using prevailing wage rate for Los Angeles County"
    // or "No prevailing wage found — using market rate"
}
