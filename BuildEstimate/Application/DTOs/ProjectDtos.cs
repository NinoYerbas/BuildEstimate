/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// DTOs — Data Transfer Objects
// ============================================================================
//
// WHAT A DTO IS:
//   A DTO is a simplified copy of an entity that gets sent to the frontend.
//   
//   WHY NOT JUST SEND THE ENTITY DIRECTLY?
//   1. Security: Entity has internal fields (CreatedBy, database IDs) you don't
//      want the frontend to see or modify
//   2. Performance: Entity loads navigation properties you might not need
//   3. Flexibility: The API response shape might differ from the database shape
//   4. Protection: If someone sends malicious JSON, they can only set DTO fields,
//      not sneaky entity fields like IsAdmin or Balance
//
// JERP EQUIVALENT:
//   Your AccountDto, JournalEntryDto, SalesOrderDto — same pattern.
//   The entity is the DATABASE shape, the DTO is the API shape.
//
// REAL WORLD ANALOGY:
//   Entity = your complete medical record (private, detailed)
//   DTO = the summary your insurance company gets (only what they need)
//
// ============================================================================

using BuildEstimate.Core.Enums;

namespace BuildEstimate.Application.DTOs;

// =====================================================================
// PROJECT DTOs
// =====================================================================

/// <summary>
/// What the API RETURNS when someone requests project data.
/// Contains everything a user needs to see — nothing they shouldn't.
/// </summary>
public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public ProjectType Type { get; set; }
    public ProjectStatus Status { get; set; }
    public bool IsPrevailingWage { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? ClientContactName { get; set; }
    public string? ClientEmail { get; set; }
    public string? ClientPhone { get; set; }
    public DateTime? BidDueDate { get; set; }
    public decimal BidAmount { get; set; }
    public decimal? GrossSquareFootage { get; set; }
    public int? NumberOfFloors { get; set; }
    public string? ConstructionType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Computed fields — not stored in the database, calculated when building the DTO
    public int EstimateCount { get; set; }
    // How many estimates this project has. Saves a second API call.

    public string StatusDisplay => Status.ToString();
    // "Estimating", "BidSubmitted", etc. — human-readable for the UI.

    public string TypeDisplay => Type.ToString();
    // "Commercial", "Institutional", etc.
}

/// <summary>
/// What the API RECEIVES when someone creates a new project.
/// Only includes fields the user should set at creation time.
/// 
/// Notice: No Id (auto-generated), no CreatedAt (auto-set), no BidAmount (calculated later).
/// </summary>
public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public ProjectType Type { get; set; } = ProjectType.Commercial;
    public bool IsPrevailingWage { get; set; } = false;
    public string ClientName { get; set; } = string.Empty;
    public string? ClientContactName { get; set; }
    public string? ClientEmail { get; set; }
    public string? ClientPhone { get; set; }
    public DateTime? BidDueDate { get; set; }
    public decimal? GrossSquareFootage { get; set; }
    public int? NumberOfFloors { get; set; }
    public string? ConstructionType { get; set; }
}

/// <summary>
/// What the API RECEIVES when someone updates an existing project.
/// Similar to Create but doesn't include fields that shouldn't change
/// (like the original create date).
/// </summary>
public class UpdateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public ProjectType Type { get; set; }
    public ProjectStatus Status { get; set; }
    public bool IsPrevailingWage { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? ClientContactName { get; set; }
    public string? ClientEmail { get; set; }
    public string? ClientPhone { get; set; }
    public DateTime? BidDueDate { get; set; }
    public decimal? GrossSquareFootage { get; set; }
    public int? NumberOfFloors { get; set; }
    public string? ConstructionType { get; set; }
}

// =====================================================================
// CSI MASTERFORMAT DTOs
// =====================================================================

/// <summary>
/// CSI Division as returned by the API.
/// Includes a count of sections for the UI tree view.
/// </summary>
public class CSIDivisionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int SectionCount { get; set; }

    /// <summary>
    /// Display format: "03 — Concrete"
    /// This is how estimators expect to see division codes.
    /// </summary>
    public string Display => $"{Code} — {Name}";
}

/// <summary>
/// CSI Section as returned by the API.
/// Includes the parent division name for context.
/// </summary>
public class CSISectionDto
{
    public Guid Id { get; set; }
    public Guid CSIDivisionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultUnitOfMeasure { get; set; }
    public Guid? ParentSectionId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int SubSectionCount { get; set; }

    // From the parent division (joined data)
    public string DivisionCode { get; set; } = string.Empty;
    public string DivisionName { get; set; } = string.Empty;

    /// <summary>
    /// Display format: "03 30 00 — Cast-in-Place Concrete"
    /// </summary>
    public string Display => $"{Code} — {Name}";
}

/// <summary>
/// A tree node for hierarchical display of the CSI structure.
/// The frontend renders this as a collapsible tree:
///   ▶ 03 — Concrete
///     ▶ 03 10 00 — Concrete Forming
///     ▶ 03 20 00 — Concrete Reinforcing
///     ▶ 03 30 00 — Cast-in-Place Concrete
/// </summary>
public class CSITreeNodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DefaultUnitOfMeasure { get; set; }
    public string NodeType { get; set; } = "Division"; // "Division" or "Section"
    public List<CSITreeNodeDto> Children { get; set; } = new();
}
