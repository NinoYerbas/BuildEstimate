/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 * 
 * PROPRIETARY AND CONFIDENTIAL
 * For licensing inquiries: ichbincesartobar@yahoo.com
 */

// ============================================================================
// PROJECT.CS — The Construction Project Entity
// ============================================================================
// 
// WHAT THIS FILE IS:
//   This is an "entity" — a C# class that represents one row in the "Projects"
//   table of your database. Every property (Name, Address, City...) becomes a
//   column in that table.
//
// JERP EQUIVALENT:
//   This is like your Company entity in JERP. In JERP, everything (accounts,
//   employees, journal entries) belongs to a Company. Here, everything 
//   (estimates, takeoffs, line items) belongs to a Project.
//
// DATABASE CONCEPT — "Entity":
//   An entity is simply a C# class that Entity Framework maps to a database table.
//   - The class name "Project" → becomes table name "Projects"
//   - Each property → becomes a column
//   - Each instance of the class → becomes one row
//   
//   Example:
//   Project object in C#          →  Row in Projects table
//   { Name = "Sunrise Medical" }  →  | Id | Name             | City        |
//                                     | 1  | Sunrise Medical  | Los Angeles |
//
// ============================================================================

using System.ComponentModel.DataAnnotations;      // Gives us [Required], [MaxLength], etc.
using System.ComponentModel.DataAnnotations.Schema; // Gives us [Table], [Column], etc.
using BuildEstimate.Core.Enums;                    // Gives us ProjectType, ProjectStatus enums

namespace BuildEstimate.Core.Entities;

/// <summary>
/// Represents a construction project that will be estimated.
/// This is the TOP-LEVEL entity — everything else hangs off a Project.
/// 
/// Think of it like this:
///   JERP:    Company → has → Accounts, Employees, Journal Entries
///   Here:    Project → has → Estimates, Takeoffs, Crews
/// </summary>
[Table("Projects")]  // Tells EF: "Put this in a table called 'Projects'"
public class Project
{
    // =====================================================================
    // PRIMARY KEY
    // =====================================================================
    // Every table needs a primary key — a unique identifier for each row.
    // We use Guid (globally unique identifier) instead of int because:
    //   - int: 1, 2, 3... works fine for one database, but if you merge
    //     two databases, you get conflicts (both have ID = 1)
    //   - Guid: "a1b2c3d4-e5f6-..." is unique across the entire world,
    //     so you can merge databases without conflicts
    //
    // JERP uses Guid too — look at your Account.Id, Employee.Id, etc.
    // =====================================================================
    [Key]  // Tells EF: "This is the primary key"
    public Guid Id { get; set; } = Guid.NewGuid();  // Auto-generates a unique ID

    // =====================================================================
    // PROJECT INFORMATION
    // =====================================================================
    // These are the columns that store the basic project details.
    // [Required] means the column cannot be NULL in the database.
    // [MaxLength(200)] means the column is VARCHAR(200) — max 200 characters.
    //
    // WHY lengths matter:
    //   - Without MaxLength, EF creates NVARCHAR(MAX) which is unlimited
    //   - Unlimited columns can't be indexed efficiently
    //   - Setting a reasonable max prevents wasted storage and helps performance
    // =====================================================================

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    // Example: "Sunrise Medical Center - Phase 2"
    // This is the project's display name, like AccountName in JERP.

    [MaxLength(500)]
    public string? Description { get; set; }
    // Example: "New 45,000 SF medical office building, 3 stories, Type II-B construction"
    // The ? makes this NULLABLE — it's optional. Not every project needs a description.
    // In the database, this column allows NULL.

    // =====================================================================
    // LOCATION — Critical for Prevailing Wage Lookups
    // =====================================================================
    // These fields determine which prevailing wage rates apply.
    // When a user types "Los Angeles, CA" — the system queries:
    //   SELECT * FROM WageDeterminations WHERE State = 'CA' AND County = 'Los Angeles'
    // and auto-loads all trade rates (Carpenter: $81.07/hr, Electrician: $92.50/hr, etc.)
    // =====================================================================

    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;
    // Example: "1234 Healthcare Drive"

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;
    // Example: "Los Angeles"
    // THIS IS THE KEY FIELD for wage lookups!

    [Required]
    [MaxLength(100)]
    public string County { get; set; } = string.Empty;
    // Example: "Los Angeles"
    // Davis-Bacon wages are published BY COUNTY, not by city.
    // Some counties have different rates than the city inside them.

    [Required]
    [MaxLength(2)]
    public string State { get; set; } = string.Empty;
    // Example: "CA"
    // Two-letter state code. Used for state prevailing wage lookups.
    // California (CA) has its own rates PLUS federal Davis-Bacon rates.

    [Required]
    [MaxLength(10)]
    public string ZipCode { get; set; } = string.Empty;
    // Example: "90012"

    // =====================================================================
    // PROJECT CLASSIFICATION
    // =====================================================================
    // These determine rules, markups, and compliance requirements.
    // A government school project has VERY different requirements than
    // a private office building.
    // =====================================================================

    [Required]
    public ProjectType Type { get; set; } = ProjectType.Commercial;
    // See Enums/ProjectType.cs for all types.
    // This is like AccountType in JERP (Asset, Liability, Revenue, etc.)

    [Required]
    public ProjectStatus Status { get; set; } = ProjectStatus.Estimating;
    // See Enums/ProjectStatus.cs for all statuses.
    // Tracks where this project is in its lifecycle.

    public bool IsPrevailingWage { get; set; } = false;
    // TRUE = government-funded project, MUST pay prevailing wages (Davis-Bacon)
    // FALSE = private project, can pay market rates
    // This flag changes how labor costs are calculated throughout the estimate.

    // =====================================================================
    // CLIENT INFORMATION
    // =====================================================================

    [Required]
    [MaxLength(200)]
    public string ClientName { get; set; } = string.Empty;
    // Example: "Sunrise Health Systems, Inc."
    // The entity that's paying for the construction.
    // In JERP terms, this is like a Customer.

    [MaxLength(200)]
    public string? ClientContactName { get; set; }
    // Example: "Maria Rodriguez, VP of Facilities"

    [MaxLength(100)]
    public string? ClientEmail { get; set; }

    [MaxLength(20)]
    public string? ClientPhone { get; set; }

    // =====================================================================
    // BID INFORMATION
    // =====================================================================
    // Construction projects are won through competitive bidding.
    // The contractor with the lowest responsible bid usually wins.
    // =====================================================================

    public DateTime? BidDueDate { get; set; }
    // The DEADLINE to submit your bid. Miss it and you're disqualified.
    // DateTime? (nullable) because not all projects are competitive bids.

    [Column(TypeName = "decimal(18,2)")]  // Tells EF: use DECIMAL(18,2) in SQL
    public decimal BidAmount { get; set; } = 0;
    // The final bid price your estimate produces.
    // decimal(18,2) means: up to 18 digits total, 2 after the decimal.
    // Example: 4,350,000.00 (a $4.35 million bid)
    //
    // WHY decimal and not double?
    //   double: 0.1 + 0.2 = 0.30000000000000004 (floating point error)
    //   decimal: 0.1 + 0.2 = 0.3 (exact, perfect for money)
    //   RULE: ALWAYS use decimal for money. Your JERP does this too.

    // =====================================================================
    // BUILDING DETAILS (optional but useful for AI analysis)
    // =====================================================================

    [Column(TypeName = "decimal(18,2)")]
    public decimal? GrossSquareFootage { get; set; }
    // Total building area. Used for $/SF calculations.
    // Example: 45000.00 (45,000 SF medical office)
    // AI can flag: "Your estimate is $185/SF but similar projects average $210/SF"

    public int? NumberOfFloors { get; set; }
    // Example: 3

    [MaxLength(100)]
    public string? ConstructionType { get; set; }
    // Example: "Type II-B (Non-combustible, Unprotected)"
    // Building code classification that affects materials and cost.

    // =====================================================================
    // NAVIGATION PROPERTIES — How Entities Connect
    // =====================================================================
    // In a relational database, tables connect through FOREIGN KEYS.
    // In Entity Framework, we represent these connections as "navigation properties."
    //
    // This is exactly what you have in JERP:
    //   Account has: List<JournalEntryLine> JournalEntryLines
    //   Company has: List<Account> Accounts, List<Employee> Employees
    //
    // Here:
    //   Project has: List<Estimate> Estimates (one project, many estimates)
    //
    // EF reads this and knows:
    //   "The Estimates table needs a column called ProjectId 
    //    that references the Projects table."
    // =====================================================================

    public List<Estimate> Estimates { get; set; } = new();
    // A project can have MULTIPLE estimates:
    //   - "v1.0 - Original"
    //   - "v2.0 - Value Engineered" (cheaper alternatives)
    //   - "v3.0 - Revised per Addendum #1"

    public List<TakeoffItem> TakeoffItems { get; set; } = new();
    // Measurements from the blueprints, shared across all estimates.

    // =====================================================================
    // AUDIT FIELDS — Who Created/Modified This Record
    // =====================================================================
    // These exist on EVERY entity. They track when records were created and
    // modified. Essential for: debugging, compliance, dispute resolution.
    //
    // Your JERP has CreatedAt and UpdatedAt on every entity too.
    // =====================================================================

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // When this record was first inserted.
    // DateTime.UtcNow = UTC time (no timezone confusion).

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    // When this record was last modified.
    // Your middleware or DbContext override updates this automatically.

    [MaxLength(100)]
    public string? CreatedBy { get; set; }
    // Which user created this. Populated from the JWT token.

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }
    // Which user last modified this.
}
