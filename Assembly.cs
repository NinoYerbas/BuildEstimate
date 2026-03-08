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
    /// <summary>Unique identifier for this project.</summary>
    public Guid Id { get; set; }

    /// <summary>The project name, e.g., "Sunrise Medical Center — Phase 1".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional project description or scope notes.</summary>
    public string? Description { get; set; }

    /// <summary>Street address of the construction site.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>City where the project is located. Used for labor rate lookups.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>County where the project is located. Critical for prevailing wage rate lookups.</summary>
    public string County { get; set; } = string.Empty;

    /// <summary>State abbreviation, e.g., "CA". Used with county for wage rate lookups.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Zip code of the project site.</summary>
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>The type of construction project (Commercial, Institutional, Industrial, Residential, etc.).</summary>
    public ProjectType Type { get; set; }

    /// <summary>Current workflow status of the project (Estimating, BidSubmitted, Awarded, etc.).</summary>
    public ProjectStatus Status { get; set; }

    /// <summary>
    /// Whether this project requires prevailing wage rates.
    /// True for most public works projects (schools, hospitals, government buildings).
    /// When true, the system uses prevailing wage labor rates instead of market rates.
    /// </summary>
    public bool IsPrevailingWage { get; set; }

    /// <summary>Name of the client or project owner hiring the contractor.</summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>Name of the specific contact person at the client organization.</summary>
    public string? ClientContactName { get; set; }

    /// <summary>Email address of the client contact.</summary>
    public string? ClientEmail { get; set; }

    /// <summary>Phone number of the client contact.</summary>
    public string? ClientPhone { get; set; }

    /// <summary>When the bid must be submitted. Acts as a countdown clock for the estimating team.</summary>
    public DateTime? BidDueDate { get; set; }

    /// <summary>The current bid price from the most recent calculated estimate. Updated automatically when estimates are recalculated.</summary>
    public decimal BidAmount { get; set; }

    /// <summary>Total floor area of the building in square feet. Used to calculate cost-per-SF benchmarks.</summary>
    public decimal? GrossSquareFootage { get; set; }

    /// <summary>Number of stories in the building. Affects labor productivity assumptions for upper floors.</summary>
    public int? NumberOfFloors { get; set; }

    /// <summary>The construction system type, e.g., "Steel Frame", "Wood Frame", "Tilt-Up Concrete".</summary>
    public string? ConstructionType { get; set; }

    /// <summary>When this project record was first created in the system.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When this project record was last modified.</summary>
    public DateTime UpdatedAt { get; set; }

    // Computed fields — not stored in the database, calculated when building the DTO
    /// <summary>How many estimates exist for this project. Saves a second API call to check.</summary>
    public int EstimateCount { get; set; }
    // How many estimates this project has. Saves a second API call.

    /// <summary>Human-readable project status label, e.g., "Estimating", "BidSubmitted".</summary>
    public string StatusDisplay => Status.ToString();
    // "Estimating", "BidSubmitted", etc. — human-readable for the UI.

    /// <summary>Human-readable project type label, e.g., "Commercial", "Institutional".</summary>
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
    /// <summary>The project name (required).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional scope description or project notes.</summary>
    public string? Description { get; set; }

    /// <summary>Street address of the construction site.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>City where the project is located.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>County — used for prevailing wage and labor rate lookups.</summary>
    public string County { get; set; } = string.Empty;

    /// <summary>State abbreviation, e.g., "CA".</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Zip code of the project site.</summary>
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>Project type. Defaults to Commercial.</summary>
    public ProjectType Type { get; set; } = ProjectType.Commercial;

    /// <summary>Whether this is a prevailing wage project. Defaults to false.</summary>
    public bool IsPrevailingWage { get; set; } = false;

    /// <summary>Name of the project owner or client.</summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>Specific contact person at the client organization.</summary>
    public string? ClientContactName { get; set; }

    /// <summary>Client contact's email address.</summary>
    public string? ClientEmail { get; set; }

    /// <summary>Client contact's phone number.</summary>
    public string? ClientPhone { get; set; }

    /// <summary>When the bid is due. Creates urgency for the estimating team.</summary>
    public DateTime? BidDueDate { get; set; }

    /// <summary>Gross square footage — enables cost-per-SF benchmarking when provided.</summary>
    public decimal? GrossSquareFootage { get; set; }

    /// <summary>Number of floors in the building.</summary>
    public int? NumberOfFloors { get; set; }

    /// <summary>Construction system type, e.g., "Steel Frame", "Wood Frame".</summary>
    public string? ConstructionType { get; set; }
}

/// <summary>
/// What the API RECEIVES when someone updates an existing project.
/// Similar to Create but doesn't include fields that shouldn't change
/// (like the original create date).
/// </summary>
public class UpdateProjectRequest
{
    /// <summary>Updated project name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Updated project description or scope notes.</summary>
    public string? Description { get; set; }

    /// <summary>Updated street address.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Updated city.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Updated county (affects labor rate lookups).</summary>
    public string County { get; set; } = string.Empty;

    /// <summary>Updated state abbreviation.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Updated zip code.</summary>
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>Updated project type.</summary>
    public ProjectType Type { get; set; }

    /// <summary>Updated project status (e.g., change from Estimating to BidSubmitted).</summary>
    public ProjectStatus Status { get; set; }

    /// <summary>Updated prevailing wage flag.</summary>
    public bool IsPrevailingWage { get; set; }

    /// <summary>Updated client name.</summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>Updated client contact person.</summary>
    public string? ClientContactName { get; set; }

    /// <summary>Updated client email.</summary>
    public string? ClientEmail { get; set; }

    /// <summary>Updated client phone.</summary>
    public string? ClientPhone { get; set; }

    /// <summary>Updated bid due date.</summary>
    public DateTime? BidDueDate { get; set; }

    /// <summary>Updated gross square footage.</summary>
    public decimal? GrossSquareFootage { get; set; }

    /// <summary>Updated number of floors.</summary>
    public int? NumberOfFloors { get; set; }

    /// <summary>Updated construction type.</summary>
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
    /// <summary>Unique identifier for this CSI division.</summary>
    public Guid Id { get; set; }

    /// <summary>The two-digit CSI division code, e.g., "03" for Concrete.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>The division name, e.g., "Concrete".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description of what work falls under this division.</summary>
    public string? Description { get; set; }

    /// <summary>Display order — CSI divisions are shown in numeric code order.</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether this division is active in the system. Inactive divisions are hidden from pickers.</summary>
    public bool IsActive { get; set; }

    /// <summary>How many sections exist under this division. Shown in the tree UI to indicate expandability.</summary>
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
    /// <summary>Unique identifier for this CSI section.</summary>
    public Guid Id { get; set; }

    /// <summary>The parent division this section belongs to.</summary>
    public Guid CSIDivisionId { get; set; }

    /// <summary>The six-digit CSI section code, e.g., "03 30 00".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>The section name, e.g., "Cast-in-Place Concrete".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description of the work covered by this section.</summary>
    public string? Description { get; set; }

    /// <summary>The most common unit of measure for this type of work (e.g., "SF", "CY", "EA").</summary>
    public string? DefaultUnitOfMeasure { get; set; }

    /// <summary>If this is a sub-section, the ID of its parent section. Null for top-level sections.</summary>
    public Guid? ParentSectionId { get; set; }

    /// <summary>Display order within the division.</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether this section is active and available for use in estimates.</summary>
    public bool IsActive { get; set; }

    /// <summary>How many sub-sections exist under this section. Used to show expand arrows in the tree UI.</summary>
    public int SubSectionCount { get; set; }

    // From the parent division (joined data)
    /// <summary>The parent division code, e.g., "03". Joined for display context.</summary>
    public string DivisionCode { get; set; } = string.Empty;

    /// <summary>The parent division name, e.g., "Concrete". Joined for display context.</summary>
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
    /// <summary>Unique identifier for this node (matches Division or Section ID).</summary>
    public Guid Id { get; set; }

    /// <summary>The CSI code — "03" for a division node, "03 30 00" for a section node.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>The display name — "Concrete" for a division, "Cast-in-Place Concrete" for a section.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The default unit of measure for this section (null for division nodes).</summary>
    public string? DefaultUnitOfMeasure { get; set; }

    /// <summary>
    /// Indicates whether this node is a "Division", "Section", or "SubSection".
    /// The frontend uses this to render different icons or indentation levels.
    /// </summary>
    public string NodeType { get; set; } = "Division"; // "Division" or "Section"

    /// <summary>
    /// Child nodes nested under this node — sections under a division, sub-sections under a section.
    /// This recursive structure enables the frontend to render the full collapsible tree.
    /// </summary>
    public List<CSITreeNodeDto> Children { get; set; } = new();
}
