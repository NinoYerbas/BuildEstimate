/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// PHASE 5: ASSEMBLIES — The Killer Feature
// ============================================================================
//
// WHAT AN ASSEMBLY IS:
//   A reusable TEMPLATE that EXPLODES into multiple line items.
//
//   Example: "Interior Partition Wall, 10 FT"
//   Apply 500 SF and it creates 8 line items automatically:
//     1. Metal Studs            → 500 LF
//     2. Drywall (both sides)   → 1,000 SF
//     3. Insulation             → 500 SF
//     4. Tape & Compound        → 1,000 SF
//     5. Paint                  → 1,000 SF
//     6. Acoustic Sealant       → 200 LF
//     7. Base Trim              → 100 LF
//     8. Fire Caulk             → 50 EA
//
// WHY THIS IS THE KILLER FEATURE:
//   Without assemblies: 20 wall types × 8 items = 160 manual entries
//   With assemblies: 20 wall types × 1 click = 20 entries. Done.
//
// JERP EQUIVALENT: Bill of Materials (BOM)
//   "To make 1 Widget, you need: 2 screws, 1 bracket, 0.5 hrs labor"
//   "To build 1 SF of wall, you need: 1 LF studs, 2 SF drywall..."
//
// ============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildEstimate.Core.Entities;

[Table("Assemblies")]
public class Assembly
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? AssemblyCode { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "General";
    // "Walls", "Floors", "Ceilings", "Roofing", "Concrete", "MEP"

    public Guid? PrimaryCSIDivisionId { get; set; }

    [ForeignKey(nameof(PrimaryCSIDivisionId))]
    public CSIDivision? PrimaryCSIDivision { get; set; }

    [Required]
    [MaxLength(10)]
    public string UnitOfMeasure { get; set; } = "SF";
    // The user enters "500 SF" — components multiply from this

    // Pre-calculated cost per assembly unit (for quick browsing)
    [Column(TypeName = "decimal(18,2)")]
    public decimal MaterialCostPerUnit { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal LaborCostPerUnit { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal EquipmentCostPerUnit { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCostPerUnit { get; set; } = 0;

    public int ComponentCount { get; set; } = 0;

    public bool IsActive { get; set; } = true;
    public bool IsGlobal { get; set; } = true;
    public Guid? ProjectId { get; set; }

    [MaxLength(100)]
    public string? Source { get; set; }

    public List<AssemblyComponent> Components { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }
}

// ============================================================================
// ASSEMBLY COMPONENT — One ingredient in the recipe
// ============================================================================
// QuantityFactor = how many units of this per 1 unit of assembly
//   Drywall both sides: 2.0 (2 SF drywall per 1 SF wall)
//   Metal studs @ 16" OC: 0.75 LF per 1 SF wall
//   Insulation: 1.0 SF per 1 SF wall
// ============================================================================

[Table("AssemblyComponents")]
public class AssemblyComponent
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AssemblyId { get; set; }

    [ForeignKey(nameof(AssemblyId))]
    public Assembly? Assembly { get; set; }

    [Required]
    public Guid CSISectionId { get; set; }

    [ForeignKey(nameof(CSISectionId))]
    public CSISection? CSISection { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,4)")]
    public decimal QuantityFactor { get; set; } = 1.0m;

    [Required]
    [MaxLength(10)]
    public string UnitOfMeasure { get; set; } = "SF";

    [Column(TypeName = "decimal(5,2)")]
    public decimal WasteFactor { get; set; } = 1.00m;

    [Column(TypeName = "decimal(18,4)")]
    public decimal MaterialUnitCost { get; set; } = 0;

    [Column(TypeName = "decimal(10,4)")]
    public decimal LaborHoursPerUnit { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal LaborRate { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal EquipmentCost { get; set; } = 0;

    public Guid? TradeId { get; set; }

    [ForeignKey(nameof(TradeId))]
    public Trade? Trade { get; set; }

    public int SortOrder { get; set; } = 0;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
