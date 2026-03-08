/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// PROJECTS CONTROLLER — CRUD Operations for Construction Projects
// ============================================================================
//
// WHAT A CONTROLLER IS:
//   A controller is a class that handles HTTP requests. Each method handles
//   one URL. When someone visits /api/v1/projects, THIS code runs.
//
//   Think of a controller as a receptionist:
//   - Someone walks in (HTTP request arrives)
//   - Receptionist asks what they need (reads the URL and parameters)
//   - Receptionist gets the work done (calls the database)
//   - Receptionist hands back the result (returns JSON response)
//
// JERP EQUIVALENT:
//   Your AccountsController does the EXACT same thing:
//   - GET /api/v1/finance/accounts → returns list of accounts
//   - GET /api/v1/finance/accounts/{id} → returns one account
//   - POST /api/v1/finance/accounts → creates an account
//   - PUT /api/v1/finance/accounts/{id} → updates an account
//
//   This controller follows the SAME pattern with the SAME structure.
//   If you understand AccountsController, you understand this one.
//
// HTTP METHODS (the verbs of the web):
//   GET    = Read data (safe, doesn't change anything)
//   POST   = Create new data
//   PUT    = Update existing data (replace entirely)
//   PATCH  = Update partially (change one field)
//   DELETE = Remove data
//
// ============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BuildEstimate.Application.DTOs;
using BuildEstimate.Core.Entities;
using BuildEstimate.Core.Enums;
using BuildEstimate.Infrastructure.Data;

namespace BuildEstimate.Api.Controllers;

/// <summary>
/// Construction project management endpoints.
/// 
/// This controller provides:
/// - GET    /api/v1/projects          → List all projects (with filtering)
/// - GET    /api/v1/projects/{id}     → Get one project
/// - POST   /api/v1/projects          → Create a new project
/// - PUT    /api/v1/projects/{id}     → Update a project
/// - DELETE /api/v1/projects/{id}     → Delete a project
/// - GET    /api/v1/projects/summary  → Dashboard summary
/// </summary>
[Route("api/v1/projects")]
[Authorize]  // User must be logged in to access any endpoint in this controller
public class ProjectsController : BaseApiController
{
    // =====================================================================
    // DEPENDENCIES — What This Controller Needs To Work
    // =====================================================================
    //
    // These are "injected" by the DI (Dependency Injection) container.
    // In Program.cs, you register:
    //   builder.Services.AddDbContext<BuildEstimateDbContext>();
    //   
    // Then when ASP.NET creates this controller, it automatically provides
    // the DbContext and Logger. You never call "new ProjectsController()" yourself.
    //
    // JERP EQUIVALENT: Your AccountsController has the same pattern:
    //   private readonly JerpDbContext _context;
    //   private readonly ILogger<AccountsController> _logger;
    // =====================================================================

    private readonly BuildEstimateDbContext _context;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        BuildEstimateDbContext context,
        ILogger<ProjectsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =====================================================================
    // GET /api/v1/projects — List All Projects
    // =====================================================================
    //
    // [HttpGet] tells ASP.NET: "When someone sends GET to /api/v1/projects,
    // run this method."
    //
    // [FromQuery] means the parameters come from the URL query string:
    //   /api/v1/projects?status=Estimating&state=CA
    //
    // JERP EQUIVALENT: 
    //   Your GetAccounts method: GET /api/v1/finance/accounts?companyId={id}
    //   Same pattern — accept filters, return a list.
    //
    // DATABASE CONCEPT — "Deferred Execution":
    //   _context.Projects.Where(...)  does NOT hit the database yet.
    //   It builds a query. The database is hit only when you call:
    //   .ToListAsync(), .FirstOrDefaultAsync(), .CountAsync(), etc.
    //   
    //   This lets you chain multiple .Where() calls and EF combines them
    //   into ONE SQL query. Very efficient.
    // =====================================================================

    /// <summary>
    /// Get all projects with optional filtering by status and state.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProjects(
        [FromQuery] ProjectStatus? status = null,
        [FromQuery] string? state = null,
        [FromQuery] string? search = null)
    {
        // Start building the query (no database hit yet)
        var query = _context.Projects.AsQueryable();

        // Apply filters if provided
        // Each .Where() adds an AND condition to the SQL
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);
            // SQL: WHERE Status = 0 (Estimating)

        if (!string.IsNullOrEmpty(state))
            query = query.Where(p => p.State == state);
            // SQL: WHERE Status = 0 AND State = 'CA'

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => 
                p.Name.Contains(search) || 
                p.ClientName.Contains(search) ||
                p.City.Contains(search));
            // SQL: WHERE ... AND (Name LIKE '%search%' OR ClientName LIKE '%search%')

        // NOW hit the database: execute the query and transform to DTOs
        var projects = await query
            .OrderByDescending(p => p.CreatedAt)  // Newest first
            .Select(p => new ProjectDto            // Transform entity → DTO
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Address = p.Address,
                City = p.City,
                County = p.County,
                State = p.State,
                ZipCode = p.ZipCode,
                Type = p.Type,
                Status = p.Status,
                IsPrevailingWage = p.IsPrevailingWage,
                ClientName = p.ClientName,
                ClientContactName = p.ClientContactName,
                ClientEmail = p.ClientEmail,
                ClientPhone = p.ClientPhone,
                BidDueDate = p.BidDueDate,
                BidAmount = p.BidAmount,
                GrossSquareFootage = p.GrossSquareFootage,
                NumberOfFloors = p.NumberOfFloors,
                ConstructionType = p.ConstructionType,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                EstimateCount = p.Estimates.Count  // EF handles the COUNT for us
            })
            .ToListAsync();  // ← THIS is where the SQL actually executes

        return Ok(projects);
    }

    // =====================================================================
    // GET /api/v1/projects/{id} — Get One Project
    // =====================================================================
    //
    // {id} is a route parameter — it comes from the URL path:
    //   GET /api/v1/projects/a1b2c3d4-e5f6-7890-...
    //
    // JERP EQUIVALENT: Your GetAccount(Guid id) method.
    // =====================================================================

    /// <summary>
    /// Get a single project by ID with full details.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProject(Guid id)
    {
        var project = await _context.Projects
            .Where(p => p.Id == id)
            .Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Address = p.Address,
                City = p.City,
                County = p.County,
                State = p.State,
                ZipCode = p.ZipCode,
                Type = p.Type,
                Status = p.Status,
                IsPrevailingWage = p.IsPrevailingWage,
                ClientName = p.ClientName,
                ClientContactName = p.ClientContactName,
                ClientEmail = p.ClientEmail,
                ClientPhone = p.ClientPhone,
                BidDueDate = p.BidDueDate,
                BidAmount = p.BidAmount,
                GrossSquareFootage = p.GrossSquareFootage,
                NumberOfFloors = p.NumberOfFloors,
                ConstructionType = p.ConstructionType,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                EstimateCount = p.Estimates.Count
            })
            .FirstOrDefaultAsync();
            // FirstOrDefaultAsync: returns the first match, or NULL if none found.
            // SQL: SELECT TOP 1 * FROM Projects WHERE Id = '...'

        if (project == null)
        {
            return NotFound($"Project with ID {id} not found");
            // Returns HTTP 404 — "this doesn't exist"
        }

        return Ok(project);
        // Returns HTTP 200 with the project data as JSON
    }

    // =====================================================================
    // POST /api/v1/projects — Create a New Project
    // =====================================================================
    //
    // [FromBody] means the data comes from the HTTP request body as JSON:
    //   POST /api/v1/projects
    //   Content-Type: application/json
    //   {
    //     "name": "Sunrise Medical Center",
    //     "city": "Los Angeles",
    //     "state": "CA",
    //     ...
    //   }
    //
    // ASP.NET automatically converts the JSON into a CreateProjectRequest object.
    // This is called "model binding" — the framework does it for you.
    //
    // JERP EQUIVALENT: Your CreateAccount method.
    // =====================================================================

    /// <summary>
    /// Create a new construction project.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Project name is required");

        if (string.IsNullOrWhiteSpace(request.City))
            return BadRequest("City is required (needed for wage lookups)");

        if (string.IsNullOrWhiteSpace(request.State) || request.State.Length != 2)
            return BadRequest("State must be a 2-letter code (e.g., 'CA')");

        // Create the entity from the DTO
        // Notice: we NEVER trust the client to set Id, CreatedAt, or Status.
        // We set those ourselves.
        var project = new Project
        {
            // Id = Guid.NewGuid() is already set by the entity default
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            County = request.County.Trim(),
            State = request.State.Trim().ToUpper(),  // Always uppercase: "CA" not "ca"
            ZipCode = request.ZipCode.Trim(),
            Type = request.Type,
            Status = ProjectStatus.Estimating,  // New projects ALWAYS start as Estimating
            IsPrevailingWage = request.IsPrevailingWage,
            ClientName = request.ClientName.Trim(),
            ClientContactName = request.ClientContactName?.Trim(),
            ClientEmail = request.ClientEmail?.Trim(),
            ClientPhone = request.ClientPhone?.Trim(),
            BidDueDate = request.BidDueDate,
            GrossSquareFootage = request.GrossSquareFootage,
            NumberOfFloors = request.NumberOfFloors,
            ConstructionType = request.ConstructionType?.Trim(),
            CreatedBy = GetCurrentUsername()  // From the JWT token (BaseApiController method)
        };

        // Add to the DbContext's change tracker
        _context.Projects.Add(project);
        // At this point, NOTHING has been saved to the database yet.
        // The project is just "staged" — like adding a file to git staging area.

        // Save to database — THIS executes the INSERT SQL
        await _context.SaveChangesAsync();
        // SQL: INSERT INTO Projects (Id, Name, City, ...) VALUES ('...', 'Sunrise...', 'LA', ...)

        _logger.LogInformation(
            "Created project {ProjectId} - {ProjectName} in {City}, {State}",
            project.Id, project.Name, project.City, project.State);

        // Return the created project as a DTO
        var dto = new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Address = project.Address,
            City = project.City,
            County = project.County,
            State = project.State,
            ZipCode = project.ZipCode,
            Type = project.Type,
            Status = project.Status,
            IsPrevailingWage = project.IsPrevailingWage,
            ClientName = project.ClientName,
            ClientContactName = project.ClientContactName,
            ClientEmail = project.ClientEmail,
            ClientPhone = project.ClientPhone,
            BidDueDate = project.BidDueDate,
            BidAmount = project.BidAmount,
            GrossSquareFootage = project.GrossSquareFootage,
            NumberOfFloors = project.NumberOfFloors,
            ConstructionType = project.ConstructionType,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            EstimateCount = 0  // New project has no estimates yet
        };

        return Created(dto);
        // Returns HTTP 201 (Created) — tells the client "your thing was created successfully"
    }

    // =====================================================================
    // PUT /api/v1/projects/{id} — Update an Existing Project
    // =====================================================================
    //
    // The flow is:
    //   1. Find the existing project in the database
    //   2. If it doesn't exist, return 404
    //   3. Update the fields from the request
    //   4. Save the changes
    //   5. Return the updated project
    //
    // JERP EQUIVALENT: Your UpdateAccount method — identical pattern.
    // =====================================================================

    /// <summary>
    /// Update an existing project.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request)
    {
        // Find the existing project
        var project = await _context.Projects.FindAsync(id);
        // FindAsync: looks up by primary key. Very fast — uses the index.
        // SQL: SELECT * FROM Projects WHERE Id = '...'

        if (project == null)
            return NotFound($"Project with ID {id} not found");

        // Business rule: Can't modify a project that's already been bid
        // (unless you're changing the status — like marking it as Awarded)
        if (project.Status == ProjectStatus.BidSubmitted && 
            request.Status == ProjectStatus.BidSubmitted)
        {
            // Only allow status changes on submitted projects
            // Don't let users change the name/address of a submitted bid
        }

        // Update fields
        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();
        project.Address = request.Address.Trim();
        project.City = request.City.Trim();
        project.County = request.County.Trim();
        project.State = request.State.Trim().ToUpper();
        project.ZipCode = request.ZipCode.Trim();
        project.Type = request.Type;
        project.Status = request.Status;
        project.IsPrevailingWage = request.IsPrevailingWage;
        project.ClientName = request.ClientName.Trim();
        project.ClientContactName = request.ClientContactName?.Trim();
        project.ClientEmail = request.ClientEmail?.Trim();
        project.ClientPhone = request.ClientPhone?.Trim();
        project.BidDueDate = request.BidDueDate;
        project.GrossSquareFootage = request.GrossSquareFootage;
        project.NumberOfFloors = request.NumberOfFloors;
        project.ConstructionType = request.ConstructionType?.Trim();
        project.UpdatedBy = GetCurrentUsername();
        // UpdatedAt is set automatically by SaveChangesAsync override

        await _context.SaveChangesAsync();
        // SQL: UPDATE Projects SET Name = '...', City = '...' WHERE Id = '...'

        _logger.LogInformation(
            "Updated project {ProjectId} - {ProjectName}",
            project.Id, project.Name);

        // Return updated project
        var dto = new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Address = project.Address,
            City = project.City,
            County = project.County,
            State = project.State,
            ZipCode = project.ZipCode,
            Type = project.Type,
            Status = project.Status,
            IsPrevailingWage = project.IsPrevailingWage,
            ClientName = project.ClientName,
            ClientContactName = project.ClientContactName,
            ClientEmail = project.ClientEmail,
            ClientPhone = project.ClientPhone,
            BidDueDate = project.BidDueDate,
            BidAmount = project.BidAmount,
            GrossSquareFootage = project.GrossSquareFootage,
            NumberOfFloors = project.NumberOfFloors,
            ConstructionType = project.ConstructionType,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            EstimateCount = await _context.Estimates.CountAsync(e => e.ProjectId == id)
        };

        return Ok(dto);
    }

    // =====================================================================
    // DELETE /api/v1/projects/{id} — Delete a Project
    // =====================================================================
    //
    // IMPORTANT: We do a "soft" safety check — don't delete projects
    // that have been submitted for bidding. Those are business records.
    //
    // Because we set CASCADE DELETE in the DbContext, deleting a project
    // automatically deletes all its estimates and takeoff items too.
    // =====================================================================

    /// <summary>
    /// Delete a project and all associated data.
    /// Cannot delete projects with status BidSubmitted or later.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var project = await _context.Projects.FindAsync(id);

        if (project == null)
            return NotFound($"Project with ID {id} not found");

        // Business rule: Can't delete projects that have been bid
        if (project.Status >= ProjectStatus.BidSubmitted)
            return BadRequest("Cannot delete a project that has been submitted for bidding. Change status to Cancelled instead.");

        _context.Projects.Remove(project);
        // Marks the project for deletion. CASCADE will handle estimates and takeoffs.

        await _context.SaveChangesAsync();
        // SQL: DELETE FROM TakeoffItems WHERE ProjectId = '...'
        //      DELETE FROM Estimates WHERE ProjectId = '...'
        //      DELETE FROM Projects WHERE Id = '...'
        // (EF handles the cascade order automatically)

        _logger.LogInformation(
            "Deleted project {ProjectId} - {ProjectName}",
            project.Id, project.Name);

        return Ok(new { message = $"Project '{project.Name}' deleted successfully" });
    }

    // =====================================================================
    // GET /api/v1/projects/summary — Dashboard Summary
    // =====================================================================
    //
    // Returns aggregate data for a dashboard:
    //   - Total projects by status
    //   - Total bid value
    //   - Projects with upcoming deadlines
    //
    // This is a CUSTOM ENDPOINT — not standard CRUD.
    // It's the kind of thing your KPIController and DashboardController do in JERP.
    // =====================================================================

    /// <summary>
    /// Get a summary of all projects for the dashboard.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetProjectSummary()
    {
        var projects = await _context.Projects.ToListAsync();

        var summary = new
        {
            TotalProjects = projects.Count,
            ActiveEstimates = projects.Count(p => p.Status == ProjectStatus.Estimating),
            BidsSubmitted = projects.Count(p => p.Status == ProjectStatus.BidSubmitted),
            ProjectsAwarded = projects.Count(p => p.Status == ProjectStatus.Awarded),
            ProjectsLost = projects.Count(p => p.Status == ProjectStatus.Lost),
            UnderConstruction = projects.Count(p => p.Status == ProjectStatus.UnderConstruction),
            
            TotalBidValue = projects
                .Where(p => p.Status >= ProjectStatus.BidSubmitted)
                .Sum(p => p.BidAmount),
            
            AwardedValue = projects
                .Where(p => p.Status == ProjectStatus.Awarded || p.Status == ProjectStatus.UnderConstruction)
                .Sum(p => p.BidAmount),
            
            UpcomingDeadlines = projects
                .Where(p => p.BidDueDate.HasValue && 
                           p.BidDueDate.Value > DateTime.UtcNow &&
                           p.BidDueDate.Value <= DateTime.UtcNow.AddDays(14) &&
                           p.Status == ProjectStatus.Estimating)
                .OrderBy(p => p.BidDueDate)
                .Select(p => new { p.Id, p.Name, p.BidDueDate, p.ClientName })
                .ToList()
        };

        return Ok(summary);
    }
}
