/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// PHASE 4: LABOR RATES & PRODUCTION RATES
// ============================================================================
//
// TWO CRITICAL DATABASES FOR CONSTRUCTION ESTIMATING:
//
// 1. LABOR RATES — How much do you PAY a worker per hour?
//    Source: Government prevailing wage determinations (Davis-Bacon Act)
//    Example: "Drywall Installer in Los Angeles County = $65.42/hr"
//    This includes base wage + fringe benefits (health, pension, vacation)
//
// 2. PRODUCTION RATES — How FAST does a worker install things?
//    Source: RSMeans data, company historical data, or field experience
//    Example: "Drywall installation = 0.017 labor-hours per SF"
//    Meaning: one worker installs ~59 SF per hour
//
// THE CONNECTION:
//    Production Rate × Labor Rate = Labor Cost Per Unit
//    0.017 hrs/SF × $65.42/hr = $1.11 per SF for labor
//    Then: 9,400 SF × $1.11/SF = $10,434 total labor cost
//
// JERP EQUIVALENT:
//    LaborRate ≈ JERP Employee.PayRate (what you pay someone per hour)
//    ProductionRate ≈ JERP BillOfMaterials (how much input per output)
//    Together they feed into the estimate like BOM feeds into manufacturing cost.
//
// PREVAILING WAGE — WHY IT MATTERS:
//    On government-funded projects (schools, hospitals, highways),
//    federal law REQUIRES contractors to pay "prevailing wages."
//    These rates are set by the Department of Labor, not the market.
//    A drywall installer might earn $35/hr on a private job
//    but MUST be paid $65/hr on a prevailing wage job.
//    Getting this wrong = federal violations, fines, debarment.
//
//    Your Project.IsPrevailingWage flag determines which rates to use.
//
// ============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildEstimate.Core.Entities;

// ============================================================================
// TRADE — A type of construction worker
// ============================================================================
// Trades are the specialties in construction:
//   Carpenter, Electrician, Plumber, Ironworker, Painter, etc.
//
// Each trade has different wage rates in different locations.
// A Carpenter in LA earns differently than a Carpenter in rural Iowa.
//
// JERP EQUIVALENT: Department (organizational grouping of employees)
// ============================================================================

[Table("Trades")]
public class Trade
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    // "Carpenter", "Electrician", "Plumber", "Drywall Installer"

    [MaxLength(20)]
    public string? TradeCode { get; set; }
    // "CARP", "ELEC", "PLUM", "DRYW" — short codes for reports

    [MaxLength(500)]
    public string? Description { get; set; }
    // "Installs and repairs wooden structures, forms, and frameworks"

    [MaxLength(200)]
    public string? UnionAffiliation { get; set; }
    // "United Brotherhood of Carpenters Local 409"
    // Many construction trades are unionized — wage rates come from union contracts.

    public bool IsActive { get; set; } = true;

    // Navigation
    public List<LaborRate> LaborRates { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ============================================================================
// LABOR RATE — How much a worker costs per hour
// ============================================================================
//
// This is the TOTAL cost to the contractor, not just the paycheck.
//
// A worker's "rate" has multiple components:
//   BASE WAGE:         $42.50/hr (what the worker takes home)
//   HEALTH & WELFARE:  $12.80/hr (medical insurance)
//   PENSION:           $ 8.50/hr (retirement fund)
//   VACATION/HOLIDAY:  $ 3.25/hr (paid time off accrual)
//   TRAINING:          $ 0.85/hr (apprenticeship programs)
//   ─────────────────────────────
//   TOTAL RATE:        $67.90/hr (what it COSTS the contractor)
//
// The worker sees $42.50 on their paycheck.
// The contractor pays $67.90 per hour total.
// The estimate must use $67.90 — the TOTAL cost.
//
// This maps DIRECTLY to JERP Payroll:
//   Employee.HourlyRate = BaseWage
//   Deductions = Health, Pension, etc.
//   Total employer cost = what BuildEstimate uses
//
// ============================================================================

[Table("LaborRates")]
public class LaborRate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // =====================================================================
    // WHO — Which trade?
    // =====================================================================
    [Required]
    public Guid TradeId { get; set; }

    [ForeignKey(nameof(TradeId))]
    public Trade? Trade { get; set; }

    // =====================================================================
    // WHERE — Location determines the rate
    // =====================================================================
    [Required]
    [MaxLength(100)]
    public string County { get; set; } = string.Empty;
    // "Los Angeles" — prevailing wages are set by county

    [Required]
    [MaxLength(2)]
    public string State { get; set; } = string.Empty;
    // "CA"

    // =====================================================================
    // WAGE COMPONENTS
    // =====================================================================

    [Column(TypeName = "decimal(10,2)")]
    public decimal BaseWage { get; set; } = 0;
    // The worker's hourly pay: $42.50

    [Column(TypeName = "decimal(10,2)")]
    public decimal HealthWelfare { get; set; } = 0;
    // Medical insurance contribution: $12.80

    [Column(TypeName = "decimal(10,2)")]
    public decimal Pension { get; set; } = 0;
    // Retirement fund contribution: $8.50

    [Column(TypeName = "decimal(10,2)")]
    public decimal VacationHoliday { get; set; } = 0;
    // Paid time off accrual: $3.25

    [Column(TypeName = "decimal(10,2)")]
    public decimal Training { get; set; } = 0;
    // Apprenticeship fund: $0.85

    [Column(TypeName = "decimal(10,2)")]
    public decimal OtherFringe { get; set; } = 0;
    // Any other benefits

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalRate { get; set; } = 0;
    // Sum of all components — THIS is what the estimate uses.
    // Base + Health + Pension + Vacation + Training + Other

    // =====================================================================
    // RATE TYPE — Prevailing vs Market
    // =====================================================================

    [Required]
    [MaxLength(20)]
    public string RateType { get; set; } = "Market";
    // "Prevailing" — government-mandated rate (Davis-Bacon)
    // "Market" — what the open market pays (private projects)
    // "Union" — negotiated union scale

    // =====================================================================
    // OVERTIME RATES
    // =====================================================================

    [Column(TypeName = "decimal(10,2)")]
    public decimal OvertimeRate { get; set; } = 0;
    // 1.5× base wage for hours over 8/day or 40/week
    // Base $42.50 × 1.5 = $63.75 + fringes = OT rate

    [Column(TypeName = "decimal(10,2)")]
    public decimal DoubleTimeRate { get; set; } = 0;
    // 2× base wage for Sundays/holidays in some jurisdictions

    // =====================================================================
    // VALIDITY PERIOD
    // =====================================================================

    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    // When this rate takes effect

    public DateTime? ExpirationDate { get; set; }
    // When this rate expires (new wage determination issued)
    // NULL = currently active

    [MaxLength(100)]
    public string? Source { get; set; }
    // "DIR 2026-001" — Department of Industrial Relations determination number
    // "Union Contract 2025-2028"
    // This is the audit trail — where did this number come from?

    public bool IsActive { get; set; } = true;

    // AUDIT
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ============================================================================
// PRODUCTION RATE — How fast work gets done
// ============================================================================
//
// Also called "labor productivity" or "labor units."
//
// This answers: "How many labor-hours does it take to install one unit?"
//
// Examples:
//   Drywall (hang):    0.017 hrs/SF  (one worker hangs ~59 SF per hour)
//   Drywall (tape):    0.012 hrs/SF  (taping is faster than hanging)
//   Concrete (place):  0.050 hrs/CY  (place 20 CY per hour with crew)
//   Door (install):    2.000 hrs/EA  (2 hours per door)
//   Paint (walls):     0.008 hrs/SF  (one painter covers 125 SF per hour)
//
// CREW SIZE affects this:
//   If the rate is 0.050 hrs/CY and the crew is 4 workers:
//   Total labor = 0.050 × 4 = 0.200 labor-hours per CY
//   But they finish 4× faster in real time.
//
// SOURCE of production rates:
//   1. RSMeans — industry-standard book (like a textbook)
//   2. Company historical data — "last 5 drywall jobs averaged 0.018 hrs/SF"
//   3. Subcontractor quotes — "we can do 10,000 SF in 170 hours"
//
// This data feeds DIRECTLY into the EstimateLineItem:
//   LineItem.LaborHoursPerUnit = ProductionRate.HoursPerUnit
//   LineItem.LaborRate = LaborRate.TotalRate
//   Then the calculation engine computes the cost.
//
// JERP EQUIVALENT:
//   Bill of Materials quantity-per-assembly
//   "To make 1 widget, you need 0.5 hours of labor"
// ============================================================================

[Table("ProductionRates")]
public class ProductionRate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // =====================================================================
    // WHAT WORK — Linked to CSI section
    // =====================================================================
    [Required]
    public Guid CSISectionId { get; set; }

    [ForeignKey(nameof(CSISectionId))]
    public CSISection? CSISection { get; set; }

    // =====================================================================
    // WHO DOES IT — Linked to trade
    // =====================================================================
    [Required]
    public Guid TradeId { get; set; }

    [ForeignKey(nameof(TradeId))]
    public Trade? Trade { get; set; }

    // =====================================================================
    // THE RATE
    // =====================================================================

    [Required]
    [MaxLength(300)]
    public string Description { get; set; } = string.Empty;
    // "Hang 5/8\" gypsum board on metal studs, walls"
    // More specific than the CSI section name

    [Column(TypeName = "decimal(10,4)")]
    public decimal HoursPerUnit { get; set; } = 0;
    // 0.017 labor-hours per SF
    // This is THE number the estimate uses.

    [Required]
    [MaxLength(10)]
    public string UnitOfMeasure { get; set; } = "SF";

    // =====================================================================
    // CREW INFORMATION
    // =====================================================================

    public int CrewSize { get; set; } = 1;
    // How many workers in the crew for this task
    // Drywall hanging: typically 2-person crew
    // Concrete placement: typically 4-6 person crew

    [Column(TypeName = "decimal(10,4)")]
    public decimal DailyOutput { get; set; } = 0;
    // How much the crew produces in one 8-hour day
    // Drywall crew of 2: ~940 SF per day
    // Useful for scheduling: "9,400 SF ÷ 940 SF/day = 10 days"

    // =====================================================================
    // RATE SOURCE & CONFIDENCE
    // =====================================================================

    [Required]
    [MaxLength(20)]
    public string Source { get; set; } = "RSMeans";
    // "RSMeans" — industry standard reference book
    // "Historical" — from company's past project data
    // "SubQuote" — derived from a subcontractor's bid
    // "FieldEstimate" — estimator's professional judgment

    [MaxLength(20)]
    public string? ConditionFactor { get; set; }
    // "Normal", "Difficult", "Ideal"
    // Working above 10 feet = Difficult (slower)
    // Open floor with good access = Ideal (faster)
    // This affects the HoursPerUnit rate.

    [MaxLength(500)]
    public string? Notes { get; set; }
    // "Based on 5 similar medical office projects 2024-2025"
    // "Rate increases 15% above 10 FT height"

    public bool IsActive { get; set; } = true;

    // AUDIT
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
