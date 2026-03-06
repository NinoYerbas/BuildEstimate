/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// CSI MASTERFORMAT — The Construction Industry's Chart of Accounts
// ============================================================================
//
// WHAT CSI IS:
//   CSI (Construction Specifications Institute) MasterFormat is the universal
//   numbering system that organizes ALL construction work into categories.
//   
//   Every architect, every contractor, every estimator in North America uses
//   this system. It's like GAAP for accountants — the standard everyone follows.
//
// HOW IT MAPS TO JERP:
//   JERP Chart of Accounts:            CSI MasterFormat:
//   ─────────────────────              ──────────────────
//   1000 Assets                        03 Concrete
//     1100 Cash                          03 10 00 Concrete Forming
//       1110 Checking                      03 11 00 Structural Forming
//     1200 Accounts Receivable           03 20 00 Concrete Reinforcing
//   2000 Liabilities                   09 Finishes
//     2100 Accounts Payable              09 20 00 Plaster & Gypsum Board
//                                          09 29 00 Gypsum Board (drywall)
//
//   See the pattern? It's a TREE. Parent → Child → Grandchild.
//   Account 1000 → 1100 → 1110  is the same as  Division 03 → 03 10 → 03 11.
//
// DATABASE CONCEPT — "Self-Referencing Hierarchy":
//   When a table has a foreign key that points back to ITSELF, it creates
//   a tree structure. CSISection has a ParentId that points to another CSISection.
//   
//   This is how you model any hierarchy in a database:
//   - File system (folders contain folders)
//   - Organization chart (employees report to employees)
//   - Comment threads (replies to replies)
//   - Chart of Accounts (sub-accounts under accounts) ← JERP does this!
//
// ============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildEstimate.Core.Entities;

/// <summary>
/// A CSI MasterFormat Division — the TOP level of the hierarchy.
/// There are 50 divisions (00 through 48, with some gaps).
/// 
/// Examples:
///   Division 03: Concrete
///   Division 09: Finishes  
///   Division 26: Electrical
///   
/// This is like the top-level account categories in JERP:
///   1000: Assets
///   2000: Liabilities
///   4000: Revenue
/// </summary>
[Table("CSIDivisions")]
public class CSIDivision
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // =====================================================================
    // THE CSI CODE
    // =====================================================================
    // The 2-digit division number. This is the IDENTITY of the division.
    // 
    // Why [MaxLength(2)]?
    //   Division codes are always 2 digits: "03", "09", "26"
    //   We don't need more than 2 characters, and constraining it
    //   prevents bad data from getting in.
    //
    // DATABASE CONCEPT — "Unique Constraint":
    //   No two divisions can have the same code. We enforce this at the
    //   database level (not just in code) because the database is the
    //   LAST LINE OF DEFENSE against bad data.
    //   
    //   Even if your code has a bug, the database will reject duplicates.
    //   Defense in depth — same as having both a lock and an alarm.
    // =====================================================================
    
    [Required]
    [MaxLength(2)]
    public string Code { get; set; } = string.Empty;
    // "03", "09", "26", etc.

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    // "Concrete", "Finishes", "Electrical"

    [MaxLength(1000)]
    public string? Description { get; set; }
    // Longer description of what this division covers.
    // "Includes all concrete work: forming, reinforcing, placing, finishing, and curing"

    public int SortOrder { get; set; }
    // Display order. Division 03 sorts before Division 09.
    // We use a separate SortOrder instead of sorting by Code because
    // some displays might group divisions differently.

    public bool IsActive { get; set; } = true;
    // Can deactivate divisions you don't use.
    // A residential contractor might deactivate Division 34 (Transportation).

    // =====================================================================
    // NAVIGATION PROPERTY — One Division has Many Sections
    // =====================================================================
    // This tells EF: "Division 03 contains Sections 03 10, 03 20, 03 30..."
    //
    // In the database, this creates:
    //   CSIDivisions table:                CSISections table:
    //   | Id | Code | Name      |         | Id | DivisionId | Code    | Name          |
    //   | A  | 03   | Concrete  |         | X  | A          | 03 10 00| Forming       |
    //                                      | Y  | A          | 03 20 00| Reinforcing   |
    //                                      | Z  | A          | 03 30 00| Cast-in-Place |
    //
    //   DivisionId is the FOREIGN KEY — it says "Section X belongs to Division A"
    // =====================================================================

    public List<CSISection> Sections { get; set; } = new();

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A CSI MasterFormat Section — the MID level of the hierarchy.
/// Sections break divisions into specific work categories.
/// 
/// Examples under Division 03 (Concrete):
///   03 10 00: Concrete Forming
///   03 20 00: Concrete Reinforcing
///   03 30 00: Cast-in-Place Concrete
///   03 40 00: Precast Concrete
///   
/// This is like sub-accounts in JERP:
///   1000 Assets → 1100 Cash → 1200 Accounts Receivable
/// </summary>
[Table("CSISections")]
public class CSISection
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // =====================================================================
    // FOREIGN KEY — Which Division Does This Section Belong To?
    // =====================================================================
    // A foreign key is a column that POINTS TO another table's primary key.
    // 
    // This is THE MOST IMPORTANT DATABASE CONCEPT to understand:
    //   
    //   CSISections.CSIDivisionId  →  points to  →  CSIDivisions.Id
    //   
    // It creates a RELATIONSHIP:
    //   "Section 03 30 00 (Cast-in-Place Concrete) BELONGS TO Division 03 (Concrete)"
    //
    // Without this, sections would float in space with no connection to divisions.
    // It's like JournalEntryLine.JournalEntryId → points to → JournalEntry.Id in JERP.
    //
    // DATABASE CONCEPT — "Referential Integrity":
    //   The database ENFORCES this relationship:
    //   - You CAN'T create a section pointing to a division that doesn't exist
    //   - You CAN'T delete a division that still has sections
    //   This prevents orphaned data (sections with no parent division).
    // =====================================================================

    [Required]
    public Guid CSIDivisionId { get; set; }
    // The ID of the parent division. This is the foreign key.

    [ForeignKey(nameof(CSIDivisionId))]
    public CSIDivision? Division { get; set; }
    // The navigation property. When you load a Section from the database,
    // EF can automatically load the related Division object too.
    // This is called "eager loading" (with .Include()) or "lazy loading".
    //
    // Example in a controller:
    //   var section = await _context.CSISections
    //       .Include(s => s.Division)       // ← "Also load the Division"
    //       .FirstAsync(s => s.Code == "03 30 00");
    //   
    //   section.Division.Name  // → "Concrete"

    // =====================================================================
    // SECTION CODE AND NAME
    // =====================================================================

    [Required]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;
    // The 6-digit section code: "03 30 00"
    // Format: DD SS ss (Division, Section, Subsection)
    //   DD = 03 (Concrete)
    //   SS = 30 (Cast-in-Place)
    //   ss = 00 (General — no further subdivision)

    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;
    // "Cast-in-Place Concrete"

    [MaxLength(1000)]
    public string? Description { get; set; }
    // "Concrete mixed on-site or delivered by truck and poured into forms.
    //  Includes slabs, foundations, walls, columns, beams, and elevated decks."

    // =====================================================================
    // SELF-REFERENCING HIERARCHY — Sections Can Have Sub-Sections
    // =====================================================================
    // A section can be a PARENT of other sections.
    // 
    // Example:
    //   03 30 00 Cast-in-Place Concrete (parent)
    //     03 31 00 Structural Concrete (child)
    //     03 35 00 Concrete Finishing (child)
    //
    // This is the SELF-REFERENCING pattern:
    //   CSISections.ParentSectionId → points to → CSISections.Id
    //   A section points to ANOTHER SECTION as its parent.
    //
    // DATABASE CONCEPT — "Recursive Relationship":
    //   When a table references itself, it's called recursive.
    //   File systems work this way: a folder contains folders.
    //   Org charts work this way: an employee reports to an employee.
    // =====================================================================

    public Guid? ParentSectionId { get; set; }
    // NULL = this is a top-level section (no parent)
    // Has a value = this is a sub-section under another section

    [ForeignKey(nameof(ParentSectionId))]
    public CSISection? ParentSection { get; set; }

    public List<CSISection> SubSections { get; set; } = new();

    // =====================================================================
    // DEFAULT VALUES FOR THIS SECTION
    // =====================================================================
    // Sections can have default units that apply to all line items under them.
    // This saves time — if the section is "Gypsum Board", the unit is always SF.
    // =====================================================================

    [MaxLength(10)]
    public string? DefaultUnitOfMeasure { get; set; }
    // "SF" for drywall, "CY" for concrete, "LF" for pipe

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
