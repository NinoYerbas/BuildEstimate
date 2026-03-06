/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// ENUMS — Named Constants for Construction Projects
// ============================================================================
//
// WHAT AN ENUM IS:
//   An enum (enumeration) is a set of named constants. Instead of storing
//   "Commercial" as a string in the database (which could have typos like
//   "comercial" or "Commecial"), you store it as a number:
//     0 = Residential, 1 = Commercial, 2 = Industrial, etc.
//
//   The database stores the NUMBER (small, fast, no typos).
//   Your code uses the NAME (readable, meaningful).
//
// JERP EQUIVALENT:
//   Your AccountType enum: Asset = 0, Liability = 1, Equity = 2, etc.
//   Same concept, different domain.
//
// DATABASE CONCEPT — "Why not just use strings?":
//   String:  "Commercial"  →  10 bytes, can have typos, slow to compare
//   Enum:    1              →  4 bytes, impossible to mistype, fast to compare
//   
//   When EF creates the table, this becomes an INT column:
//   | Id | Name             | Type |
//   | 1  | Sunrise Medical  | 1    |  ← 1 = Commercial
//
// ============================================================================

namespace BuildEstimate.Core.Enums;

/// <summary>
/// The type of construction project.
/// This determines typical markups, compliance requirements, and which
/// assemblies and production rates are most relevant.
/// </summary>
public enum ProjectType
{
    /// <summary>
    /// Houses, apartments, condos, townhomes.
    /// Typically smaller scale, wood frame construction.
    /// Markup: 15-25% is common.
    /// </summary>
    Residential = 0,

    /// <summary>
    /// Offices, retail stores, restaurants, hotels.
    /// Steel or concrete frame, more complex systems.
    /// Markup: 10-20% is common.
    /// This is the DEFAULT because it's the most common project type.
    /// </summary>
    Commercial = 1,

    /// <summary>
    /// Factories, warehouses, distribution centers.
    /// Large open spaces, heavy equipment, specialized systems.
    /// Markup: 8-15% is common (tighter margins, bigger volume).
    /// </summary>
    Industrial = 2,

    /// <summary>
    /// Schools, hospitals, government buildings, churches.
    /// MOST LIKELY to require prevailing wages (government-funded).
    /// Highest compliance requirements. Must follow Davis-Bacon.
    /// Markup: 10-18% is common.
    /// </summary>
    Institutional = 3,

    /// <summary>
    /// Roads, bridges, highways, utilities, water treatment.
    /// Heavy civil work. Almost ALWAYS prevailing wage.
    /// Specialized equipment (cranes, excavators, pavers).
    /// Markup: 8-12% (very competitive bidding).
    /// </summary>
    Infrastructure = 4,

    /// <summary>
    /// Mixed-use developments (retail on ground floor, apartments above).
    /// Combines residential and commercial requirements.
    /// </summary>
    MixedUse = 5
}

/// <summary>
/// The current status of a construction project in its lifecycle.
/// 
/// The lifecycle flows like this:
///   Estimating → BidSubmitted → [Awarded OR Lost]
///                                  ↓
///                           UnderConstruction → Completed → Closed
///                           
/// Think of this like a SalesOrder status in JERP:
///   Draft → Submitted → Approved → Shipped → Completed
/// </summary>
public enum ProjectStatus
{
    /// <summary>
    /// Currently creating the estimate. This is where most work happens.
    /// The estimate is still being built — adding line items, adjusting quantities,
    /// refining costs. The bid has NOT been submitted yet.
    /// </summary>
    Estimating = 0,

    /// <summary>
    /// The bid has been submitted to the client/owner.
    /// Now you wait. The estimate is LOCKED (no more changes to submitted version).
    /// You can still create new estimate versions if addenda come out.
    /// </summary>
    BidSubmitted = 1,

    /// <summary>
    /// YOU WON THE PROJECT! The client accepted your bid.
    /// This is when the estimate flows into JERP:
    ///   - Create Purchase Orders for materials
    ///   - Set up Payroll for workers
    ///   - Create Vendor Bills for subcontractors
    ///   - Open Job Cost accounts in the General Ledger
    /// </summary>
    Awarded = 2,

    /// <summary>
    /// You lost the bid. Someone else was cheaper or better qualified.
    /// The project stays in the system for historical analysis.
    /// AI can learn: "We lost this one — were we too high? Too low?"
    /// </summary>
    Lost = 3,

    /// <summary>
    /// Construction is happening. The estimate is now being compared
    /// to ACTUAL costs (from JERP purchase orders and payroll).
    /// This is where the estimate-to-actuals feedback loop lives.
    /// </summary>
    UnderConstruction = 4,

    /// <summary>
    /// Building is done. Final costs are known.
    /// You can now compare: Estimated $4.35M vs Actual $4.52M.
    /// This data feeds back into future estimates to improve accuracy.
    /// </summary>
    Completed = 5,

    /// <summary>
    /// All paperwork done, final payment received, warranty period started.
    /// Project is archived but data remains for historical analysis.
    /// </summary>
    Closed = 6,

    /// <summary>
    /// Project was cancelled before or during construction.
    /// Happens sometimes — funding falls through, permits denied, etc.
    /// </summary>
    Cancelled = 7
}

/// <summary>
/// Standard construction units of measure.
/// 
/// Why this matters: You can't add 500 SF of drywall to 200 LF of pipe.
/// Units enforce dimensional correctness in your calculations.
/// 
/// Every line item in an estimate has a UOM. The production rate for that
/// item must use the SAME UOM, or the math breaks.
/// </summary>
public enum UnitOfMeasure
{
    /// <summary>Square Feet — drywall, flooring, painting, roofing membrane</summary>
    SF = 0,

    /// <summary>Linear Feet — pipe, wire, baseboard, curbing, fencing</summary>
    LF = 1,

    /// <summary>Cubic Yards — concrete, excavation, fill dirt, gravel</summary>
    CY = 2,

    /// <summary>Each — doors, windows, fixtures, light poles, trees</summary>
    EA = 3,

    /// <summary>Lump Sum — subcontractor bids, allowances, misc items</summary>
    LS = 4,

    /// <summary>Tons — structural steel, asphalt, rebar</summary>
    TON = 5,

    /// <summary>Gallons — paint, sealant, adhesive</summary>
    GAL = 6,

    /// <summary>Hours — equipment rental (crane at $350/hr)</summary>
    HR = 7,

    /// <summary>Days — temporary facilities, equipment rental by day</summary>
    DAY = 8,

    /// <summary>Thousand Board Feet — lumber (yes, lumber has its own unit)</summary>
    MBF = 9,

    /// <summary>Squares (100 SF) — roofing shingles are sold in "squares"</summary>
    SQ = 10,

    /// <summary>Cubic Feet — insulation, some aggregates</summary>
    CF = 11,

    /// <summary>Pounds — nails, adhesive in bulk, misc hardware</summary>
    LB = 12
}
