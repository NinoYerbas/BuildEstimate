/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// CSI MASTERFORMAT CONTROLLER — Browse the Division/Section Hierarchy
// ============================================================================
//
// This controller is the construction equivalent of your AccountsController.
// Instead of browsing Account 1000 → 1100 → 1110, users browse:
//   Division 03 (Concrete) → Section 03 30 00 (Cast-in-Place Concrete)
//
// The frontend renders this as a tree that estimators click through
// when adding line items to an estimate:
//
//   ▶ 03 — Concrete
//     ▶ 03 10 00 — Concrete Forming
//     ▶ 03 20 00 — Concrete Reinforcing
//     ▶ 03 30 00 — Cast-in-Place Concrete  ← user clicks this
//       → System adds a line item with CSI code 03 30 00
//
// ENDPOINTS:
//   GET /api/v1/csi/divisions           → All divisions (top level)
//   GET /api/v1/csi/divisions/{id}      → One division with its sections
//   GET /api/v1/csi/sections/{id}       → One section with details
//   GET /api/v1/csi/search?q=concrete   → Search across all codes
//   GET /api/v1/csi/tree                → Full tree for the sidebar
//
// ============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BuildEstimate.Application.DTOs;
using BuildEstimate.Infrastructure.Data;

namespace BuildEstimate.Api.Controllers;

/// <summary>
/// CSI MasterFormat code browsing and search.
/// Read-only — CSI codes are seeded data, not user-created.
/// JERP EQUIVALENT: Like a read-only version of AccountsController
/// where the Chart of Accounts is pre-loaded and users browse it.
/// </summary>
[Route("api/v1/csi")]
[Authorize]
public class CSIMasterFormatController : BaseApiController
{
    private readonly BuildEstimateDbContext _context;
    private readonly ILogger<CSIMasterFormatController> _logger;

    /// <summary>
    /// Constructs the controller with injected database context and logger.
    /// </summary>
    public CSIMasterFormatController(
        BuildEstimateDbContext context,
        ILogger<CSIMasterFormatController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =====================================================================
    // GET /api/v1/csi/divisions — List All CSI Divisions
    // =====================================================================
    //
    // Returns all 34 active CSI divisions with section counts.
    // The frontend shows this as the first level of the code browser.
    //
    // DATABASE CONCEPT — "Projection with .Select()":
    //   Instead of SELECT * (which loads ALL columns), we use .Select()
    //   to load ONLY the columns we need for the DTO.
    //   
    //   This is important for performance:
    //   SELECT * FROM CSIDivisions  →  loads Description (1000 chars) even if unused
    //   SELECT Code, Name, ...      →  loads only what the DTO needs
    //
    //   With .Select(), EF generates the efficient SQL automatically.
    // =====================================================================

    /// <summary>
    /// Returns all CSI MasterFormat divisions, optionally filtered to active-only.
    /// Each division includes a count of its active sections for the tree UI.
    /// Results are sorted in the official CSI numerical order.
    /// </summary>
    /// <param name="activeOnly">When true (default), returns only active divisions.</param>
    [HttpGet("divisions")]
    public async Task<IActionResult> GetDivisions([FromQuery] bool activeOnly = true)
    {
        var query = _context.CSIDivisions.AsQueryable();

        if (activeOnly)
            query = query.Where(d => d.IsActive);

        var divisions = await query
            .OrderBy(d => d.SortOrder)
            .Select(d => new CSIDivisionDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Description = d.Description,
                SortOrder = d.SortOrder,
                IsActive = d.IsActive,
                SectionCount = d.Sections.Count(s => s.IsActive)
                // EF translates this to: SELECT ..., (SELECT COUNT(*) FROM CSISections WHERE ...) 
                // It's a subquery — fast because of the foreign key index
            })
            .ToListAsync();

        return Ok(divisions);
    }

    // =====================================================================
    // GET /api/v1/csi/divisions/{id} — One Division With Its Sections
    // =====================================================================
    //
    // When a user clicks on "03 — Concrete", the frontend calls this
    // endpoint to load all sections under that division.
    //
    // DATABASE CONCEPT — ".Include() vs .Select()":
    //   .Include(d => d.Sections) loads the FULL Section entities.
    //   .Select() builds custom DTOs from the join.
    //   
    //   We use .Select() because:
    //   1. We don't need all Section columns
    //   2. We want to add computed fields (SubSectionCount)
    //   3. It generates more efficient SQL
    // =====================================================================

    /// <summary>
    /// Returns a division plus all of its top-level sections.
    /// Sub-sections are not included — use GetSection for sub-section drill-down.
    /// Commonly used when the user clicks on a division in the code browser.
    /// </summary>
    /// <param name="id">The division ID to retrieve.</param>
    [HttpGet("divisions/{id}")]
    public async Task<IActionResult> GetDivision(Guid id)
    {
        var division = await _context.CSIDivisions
            .Where(d => d.Id == id)
            .Select(d => new
            {
                Division = new CSIDivisionDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    Description = d.Description,
                    SortOrder = d.SortOrder,
                    IsActive = d.IsActive,
                    SectionCount = d.Sections.Count(s => s.IsActive)
                },
                Sections = d.Sections
                    .Where(s => s.IsActive && s.ParentSectionId == null)  // Top-level sections only
                    .OrderBy(s => s.SortOrder)
                    .Select(s => new CSISectionDto
                    {
                        Id = s.Id,
                        CSIDivisionId = s.CSIDivisionId,
                        Code = s.Code,
                        Name = s.Name,
                        Description = s.Description,
                        DefaultUnitOfMeasure = s.DefaultUnitOfMeasure,
                        ParentSectionId = s.ParentSectionId,
                        SortOrder = s.SortOrder,
                        IsActive = s.IsActive,
                        SubSectionCount = s.SubSections.Count(ss => ss.IsActive),
                        DivisionCode = d.Code,
                        DivisionName = d.Name
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (division == null)
            return NotFound($"Division with ID {id} not found");

        return Ok(division);
    }

    // =====================================================================
    // GET /api/v1/csi/sections/{id} — One Section With Details
    // =====================================================================

    /// <summary>
    /// Returns a single CSI section with its parent division info and any sub-sections.
    /// Used when an estimator clicks on a section code to see the full detail and sub-items.
    /// </summary>
    /// <param name="id">The section ID to retrieve.</param>
    [HttpGet("sections/{id}")]
    public async Task<IActionResult> GetSection(Guid id)
    {
        var section = await _context.CSISections
            .Where(s => s.Id == id)
            .Include(s => s.Division)
            .Include(s => s.SubSections)
            .Select(s => new
            {
                Section = new CSISectionDto
                {
                    Id = s.Id,
                    CSIDivisionId = s.CSIDivisionId,
                    Code = s.Code,
                    Name = s.Name,
                    Description = s.Description,
                    DefaultUnitOfMeasure = s.DefaultUnitOfMeasure,
                    ParentSectionId = s.ParentSectionId,
                    SortOrder = s.SortOrder,
                    IsActive = s.IsActive,
                    SubSectionCount = s.SubSections.Count(ss => ss.IsActive),
                    DivisionCode = s.Division!.Code,
                    DivisionName = s.Division!.Name
                },
                SubSections = s.SubSections
                    .Where(ss => ss.IsActive)
                    .OrderBy(ss => ss.SortOrder)
                    .Select(ss => new CSISectionDto
                    {
                        Id = ss.Id,
                        CSIDivisionId = ss.CSIDivisionId,
                        Code = ss.Code,
                        Name = ss.Name,
                        Description = ss.Description,
                        DefaultUnitOfMeasure = ss.DefaultUnitOfMeasure,
                        SortOrder = ss.SortOrder,
                        IsActive = ss.IsActive,
                        DivisionCode = s.Division!.Code,
                        DivisionName = s.Division!.Name
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (section == null)
            return NotFound($"Section with ID {id} not found");

        return Ok(section);
    }

    // =====================================================================
    // GET /api/v1/csi/search?q=concrete — Search CSI Codes
    // =====================================================================
    //
    // Estimators need to FIND codes quickly. They might type "drywall"
    // and expect to see "09 29 00 — Gypsum Board (Drywall)" appear.
    //
    // This searches both division names and section names.
    //
    // DATABASE CONCEPT — "LIKE query":
    //   .Contains("drywall") generates: WHERE Name LIKE '%drywall%'
    //   The % means "any characters before and after."
    //   This is called a wildcard search.
    //   
    //   For LARGE datasets, you'd use Full-Text Search instead.
    //   For our seed data (< 200 sections), LIKE is perfectly fast.
    // =====================================================================

    /// <summary>
    /// Full-text search across all CSI codes and names.
    /// Searches both division names and section names/codes for the query string.
    /// Returns up to 20 matching sections and 10 matching divisions.
    /// Query must be at least 2 characters to prevent too-broad results.
    /// </summary>
    /// <param name="q">The search term, e.g., "drywall" or "03 30".</param>
    [HttpGet("search")]
    public async Task<IActionResult> SearchCSI([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest("Search query must be at least 2 characters");

        var searchTerm = q.Trim().ToLower();

        // Search sections (more granular, more useful results)
        var sections = await _context.CSISections
            .Where(s => s.IsActive && (
                s.Code.ToLower().Contains(searchTerm) ||
                s.Name.ToLower().Contains(searchTerm) ||
                (s.Description != null && s.Description.ToLower().Contains(searchTerm))
            ))
            .Include(s => s.Division)
            .OrderBy(s => s.Code)
            .Take(20)  // Limit results to prevent huge responses
            .Select(s => new CSISectionDto
            {
                Id = s.Id,
                CSIDivisionId = s.CSIDivisionId,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                DefaultUnitOfMeasure = s.DefaultUnitOfMeasure,
                SortOrder = s.SortOrder,
                IsActive = s.IsActive,
                DivisionCode = s.Division!.Code,
                DivisionName = s.Division!.Name
            })
            .ToListAsync();

        // Also search divisions
        var divisions = await _context.CSIDivisions
            .Where(d => d.IsActive && (
                d.Code.ToLower().Contains(searchTerm) ||
                d.Name.ToLower().Contains(searchTerm)
            ))
            .OrderBy(d => d.SortOrder)
            .Take(10)
            .Select(d => new CSIDivisionDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Description = d.Description,
                SortOrder = d.SortOrder,
                IsActive = d.IsActive,
                SectionCount = d.Sections.Count(s => s.IsActive)
            })
            .ToListAsync();

        return Ok(new { divisions, sections });
    }

    // =====================================================================
    // GET /api/v1/csi/tree — Full Tree For Sidebar Navigation
    // =====================================================================
    //
    // Returns the complete hierarchy as a tree structure.
    // The frontend renders this in a sidebar as a collapsible tree.
    //
    // This is a DENORMALIZED view — we take the relational data
    // (separate Division and Section tables) and reshape it into
    // a nested tree (each Division contains its Sections as children).
    //
    // DATABASE CONCEPT — "Denormalization":
    //   Normalization = data split into separate tables (no duplication)
    //   Denormalization = data combined for convenience (some duplication)
    //   
    //   The DATABASE is normalized (separate tables).
    //   The API RESPONSE is denormalized (nested tree).
    //   
    //   This is normal and correct — normalize for storage, denormalize for display.
    // =====================================================================

    /// <summary>
    /// Returns the complete CSI hierarchy as a nested tree structure for sidebar navigation.
    /// Each division contains its sections as child nodes; each section contains its sub-sections.
    /// This is the same data as the divisions/sections endpoints but pre-assembled for direct use
    /// in a collapsible tree UI component.
    /// </summary>
    [HttpGet("tree")]
    public async Task<IActionResult> GetCSITree()
    {
        var tree = await _context.CSIDivisions
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .Select(d => new CSITreeNodeDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                NodeType = "Division",
                Children = d.Sections
                    .Where(s => s.IsActive && s.ParentSectionId == null)
                    .OrderBy(s => s.SortOrder)
                    .Select(s => new CSITreeNodeDto
                    {
                        Id = s.Id,
                        Code = s.Code,
                        Name = s.Name,
                        DefaultUnitOfMeasure = s.DefaultUnitOfMeasure,
                        NodeType = "Section",
                        Children = s.SubSections
                            .Where(ss => ss.IsActive)
                            .OrderBy(ss => ss.SortOrder)
                            .Select(ss => new CSITreeNodeDto
                            {
                                Id = ss.Id,
                                Code = ss.Code,
                                Name = ss.Name,
                                DefaultUnitOfMeasure = ss.DefaultUnitOfMeasure,
                                NodeType = "SubSection"
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(tree);
    }
}
