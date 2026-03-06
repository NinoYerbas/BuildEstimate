/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

namespace BuildEstimate.Application.DTOs;

public class AssemblyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AssemblyCode { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal MaterialCostPerUnit { get; set; }
    public decimal LaborCostPerUnit { get; set; }
    public decimal EquipmentCostPerUnit { get; set; }
    public decimal TotalCostPerUnit { get; set; }
    public int ComponentCount { get; set; }
    public bool IsGlobal { get; set; }
    public string? Source { get; set; }
    public List<AssemblyComponentDto> Components { get; set; } = new();
}

public class AssemblyComponentDto
{
    public Guid Id { get; set; }
    public Guid CSISectionId { get; set; }
    public string CSICode { get; set; } = string.Empty;
    public string CSISectionName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal QuantityFactor { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal WasteFactor { get; set; }
    public decimal MaterialUnitCost { get; set; }
    public decimal LaborHoursPerUnit { get; set; }
    public decimal LaborRate { get; set; }
    public decimal EquipmentCost { get; set; }
    public Guid? TradeId { get; set; }
    public string? TradeName { get; set; }
    public int SortOrder { get; set; }
}

public class CreateAssemblyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? AssemblyCode { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; } = "General";
    public string UnitOfMeasure { get; set; } = "SF";
    public string? Source { get; set; }
    public List<CreateAssemblyComponentRequest> Components { get; set; } = new();
}

public class CreateAssemblyComponentRequest
{
    public Guid CSISectionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal QuantityFactor { get; set; } = 1.0m;
    public string UnitOfMeasure { get; set; } = "SF";
    public decimal WasteFactor { get; set; } = 1.00m;
    public decimal MaterialUnitCost { get; set; } = 0;
    public decimal LaborHoursPerUnit { get; set; } = 0;
    public decimal LaborRate { get; set; } = 0;
    public decimal EquipmentCost { get; set; } = 0;
    public Guid? TradeId { get; set; }
    public string? Notes { get; set; }
}

public class ApplyAssemblyRequest
{
    public Guid EstimateId { get; set; }
    public decimal Quantity { get; set; }
    public string? Location { get; set; }
    public bool OverrideLaborRates { get; set; } = false;
}

public class ApplyAssemblyResultDto
{
    public string AssemblyName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public int LineItemsCreated { get; set; }
    public decimal TotalMaterial { get; set; }
    public decimal TotalLabor { get; set; }
    public decimal TotalEquipment { get; set; }
    public decimal TotalDirectCost { get; set; }
    public decimal UpdatedBidPrice { get; set; }
    public List<EstimateLineItemDto> CreatedLineItems { get; set; } = new();
}
