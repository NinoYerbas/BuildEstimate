/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// BuildEstimateDbContext.cs — THE DATABASE MAP
// ============================================================================
//
// WHAT THIS FILE IS:
//   This is the MOST IMPORTANT FILE for understanding databases.
//   It's the bridge between your C# code and your SQL Server database.
//
//   Entity Framework (EF) reads this file and:
//   1. Creates all your database tables (from the DbSet properties)
//   2. Creates all relationships between tables (from the foreign keys)
//   3. Creates indexes for fast searches (from OnModelCreating)
//   4. Seeds initial data (from OnModelCreating → HasData)
//   5. Translates your C# queries into SQL
//
// JERP EQUIVALENT:
//   Your JerpDbContext does the same thing. It has:
//     public DbSet<Account> Accounts { get; set; }
//     public DbSet<JournalEntry> JournalEntries { get; set; }
//   And OnModelCreating configures relationships and seeds data.
//
// HOW EF WORKS (the 30-second version):
//   
//   YOU WRITE:
//     var projects = await _context.Projects
//         .Where(p => p.City == "Los Angeles")
//         .ToListAsync();
//   
//   EF TRANSLATES TO:
//     SELECT * FROM Projects WHERE City = 'Los Angeles'
//   
//   EF RETURNS:
//     A List<Project> with C# objects you can use
//   
//   You never write SQL directly. EF does it for you.
//   This is called an ORM — Object-Relational Mapper.
//
// ============================================================================

using Microsoft.EntityFrameworkCore;
using BuildEstimate.Core.Entities;

namespace BuildEstimate.Infrastructure.Data;

/// <summary>
/// The database context — maps C# classes to SQL Server tables.
/// 
/// Every DbSet<T> property creates a TABLE in the database.
/// The property NAME becomes the table name (or you override with [Table("...")]).
/// </summary>
public class BuildEstimateDbContext : DbContext
{
    // =====================================================================
    // CONSTRUCTOR
    // =====================================================================
    // The constructor receives configuration (connection string, etc.)
    // from Program.cs via dependency injection.
    //
    // DATABASE CONCEPT — "Connection String":
    //   A connection string tells EF WHERE the database is:
    //   "Server=localhost;Database=BuildEstimate;User Id=sa;Password=..."
    //   
    //   It's like a phone number — it tells your app how to reach the database.
    //   You store it in appsettings.json, NOT in code (security!).
    //
    // DEPENDENCY INJECTION CONCEPT:
    //   Your DbContext doesn't CREATE its own configuration.
    //   Program.cs creates it and GIVES IT (injects it) to the DbContext.
    //   This is like a restaurant: the waiter (DI) brings the food (config)
    //   to your table (DbContext). You don't go to the kitchen yourself.
    // =====================================================================

    public BuildEstimateDbContext(DbContextOptions<BuildEstimateDbContext> options) 
        : base(options)
    {
        // "base(options)" passes the configuration up to the parent class (DbContext)
        // which uses it to connect to the database.
    }

    // =====================================================================
    // DbSet PROPERTIES — Each One Creates a Database Table
    // =====================================================================
    //
    // DbSet<Project> Projects  →  Creates table "Projects" in SQL Server
    // DbSet<CSIDivision> CSIDivisions  →  Creates table "CSIDivisions"
    //
    // Once created, you interact with tables through these properties:
    //   _context.Projects.Add(newProject);           // INSERT
    //   _context.Projects.Where(p => p.City == "LA") // SELECT WHERE
    //   _context.Projects.Find(id);                  // SELECT by primary key
    //   _context.SaveChangesAsync();                 // Executes all pending changes
    //
    // JERP EQUIVALENT:
    //   public DbSet<Account> Accounts { get; set; }
    //   public DbSet<JournalEntry> JournalEntries { get; set; }
    //   public DbSet<Employee> Employees { get; set; }
    // =====================================================================

    public DbSet<Project> Projects { get; set; }
    public DbSet<Estimate> Estimates { get; set; }
    public DbSet<EstimateLineItem> EstimateLineItems { get; set; }
    public DbSet<CSIDivision> CSIDivisions { get; set; }
    public DbSet<CSISection> CSISections { get; set; }
    public DbSet<TakeoffItem> TakeoffItems { get; set; }
    public DbSet<Trade> Trades { get; set; }
    public DbSet<LaborRate> LaborRates { get; set; }
    public DbSet<ProductionRate> ProductionRates { get; set; }
    public DbSet<Assembly> Assemblies { get; set; }
    public DbSet<AssemblyComponent> AssemblyComponents { get; set; }

    // =====================================================================
    // OnModelCreating — Database Configuration & Seed Data
    // =====================================================================
    //
    // This method runs ONCE when EF creates or migrates your database.
    // It's where you:
    //   1. Configure relationships between tables
    //   2. Create indexes for fast searches
    //   3. Set default values
    //   4. Seed initial data (like CSI divisions)
    //
    // JERP EQUIVALENT:
    //   Your JerpDbContext.OnModelCreating does the same thing to
    //   configure Account, JournalEntry, Employee relationships.
    // =====================================================================

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =================================================================
        // TABLE CONFIGURATIONS
        // =================================================================

        // --- PROJECT ---
        modelBuilder.Entity<Project>(entity =>
        {
            // INDEX on City + State for fast wage lookups
            // When a user sets project city, we query wages by city/state.
            // Without this index, SQL Server scans EVERY row. With it, it jumps directly.
            //
            // DATABASE CONCEPT — "Index":
            //   Think of an index like the index at the back of a textbook.
            //   Without it: you flip through every page to find "Concrete" (slow).
            //   With it: you look up "Concrete" in the index → page 47 (fast).
            //   
            //   Indexes make reads FASTER but writes SLIGHTLY SLOWER
            //   (because the index must be updated too).
            //   Rule: Create indexes on columns you frequently search/filter by.
            entity.HasIndex(p => new { p.State, p.City })
                .HasDatabaseName("IX_Projects_State_City");

            // INDEX on Status for filtering active projects
            entity.HasIndex(p => p.Status)
                .HasDatabaseName("IX_Projects_Status");

            // RELATIONSHIP: Project has many Estimates
            // This tells EF: when you delete a Project, delete all its Estimates too.
            // This is called CASCADE DELETE.
            //
            // DATABASE CONCEPT — "Cascade Delete":
            //   If you delete Project "Sunrise Medical", what happens to its estimates?
            //   CASCADE: Delete them too (they're meaningless without the project)
            //   RESTRICT: Refuse to delete the project until estimates are deleted first
            //   SET NULL: Keep the estimates but set ProjectId to NULL
            //   
            //   We use CASCADE because estimates don't exist without a project.
            entity.HasMany(p => p.Estimates)
                .WithOne(e => e.Project)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.TakeoffItems)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- ESTIMATE ---
        modelBuilder.Entity<Estimate>(entity =>
        {
            // INDEX on ProjectId — fast lookup of all estimates for a project
            entity.HasIndex(e => e.ProjectId)
                .HasDatabaseName("IX_Estimates_ProjectId");

            // RELATIONSHIP: Estimate has many LineItems (CASCADE delete)
            // Delete the estimate → all its line items disappear too.
            entity.HasMany(e => e.LineItems)
                .WithOne(li => li.Estimate)
                .HasForeignKey(li => li.EstimateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- ESTIMATE LINE ITEM ---
        modelBuilder.Entity<EstimateLineItem>(entity =>
        {
            // INDEX on EstimateId — fast lookup of all lines for an estimate
            entity.HasIndex(li => li.EstimateId)
                .HasDatabaseName("IX_EstimateLineItems_EstimateId");

            // INDEX on CSISectionId — group line items by CSI code
            entity.HasIndex(li => li.CSISectionId)
                .HasDatabaseName("IX_EstimateLineItems_CSISectionId");

            // RELATIONSHIP: LineItem references CSISection (RESTRICT delete)
            // Can't delete a CSI code that's used by line items.
            entity.HasOne(li => li.CSISection)
                .WithMany()
                .HasForeignKey(li => li.CSISectionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- CSI DIVISION ---
        modelBuilder.Entity<CSIDivision>(entity =>
        {
            // UNIQUE INDEX on Code — no two divisions can have the same code
            entity.HasIndex(d => d.Code)
                .IsUnique()
                .HasDatabaseName("IX_CSIDivisions_Code");

            entity.HasMany(d => d.Sections)
                .WithOne(s => s.Division)
                .HasForeignKey(s => s.CSIDivisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- CSI SECTION ---
        modelBuilder.Entity<CSISection>(entity =>
        {
            entity.HasIndex(s => s.Code)
                .IsUnique()
                .HasDatabaseName("IX_CSISections_Code");

            // SELF-REFERENCING relationship (section → parent section)
            // RESTRICT delete: can't delete a parent section that has children
            entity.HasMany(s => s.SubSections)
                .WithOne(s => s.ParentSection)
                .HasForeignKey(s => s.ParentSectionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- TAKEOFF ITEM ---
        modelBuilder.Entity<TakeoffItem>(entity =>
        {
            entity.HasIndex(t => t.ProjectId)
                .HasDatabaseName("IX_TakeoffItems_ProjectId");

            entity.HasIndex(t => t.DrawingSheet)
                .HasDatabaseName("IX_TakeoffItems_DrawingSheet");

            // Optional FK to CSISection (RESTRICT — can't delete used codes)
            entity.HasOne(t => t.CSISection)
                .WithMany()
                .HasForeignKey(t => t.CSISectionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- TRADE ---
        modelBuilder.Entity<Trade>(entity =>
        {
            entity.HasIndex(t => t.TradeCode)
                .IsUnique()
                .HasFilter("[TradeCode] IS NOT NULL")
                .HasDatabaseName("IX_Trades_TradeCode");

            entity.HasMany(t => t.LaborRates)
                .WithOne(r => r.Trade)
                .HasForeignKey(r => r.TradeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- LABOR RATE ---
        modelBuilder.Entity<LaborRate>(entity =>
        {
            // Fast lookup: "What's the prevailing wage for carpenters in LA?"
            entity.HasIndex(r => new { r.TradeId, r.County, r.State, r.RateType })
                .HasDatabaseName("IX_LaborRates_Trade_Location_Type");
        });

        // --- PRODUCTION RATE ---
        modelBuilder.Entity<ProductionRate>(entity =>
        {
            // Fast lookup: "What's the production rate for drywall?"
            entity.HasIndex(pr => new { pr.CSISectionId, pr.TradeId })
                .HasDatabaseName("IX_ProductionRates_CSI_Trade");

            entity.HasOne(pr => pr.CSISection)
                .WithMany()
                .HasForeignKey(pr => pr.CSISectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(pr => pr.Trade)
                .WithMany()
                .HasForeignKey(pr => pr.TradeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- ASSEMBLY ---
        modelBuilder.Entity<Assembly>(entity =>
        {
            entity.HasIndex(a => a.AssemblyCode)
                .IsUnique()
                .HasFilter("[AssemblyCode] IS NOT NULL")
                .HasDatabaseName("IX_Assemblies_Code");

            entity.HasIndex(a => a.Category)
                .HasDatabaseName("IX_Assemblies_Category");

            entity.HasMany(a => a.Components)
                .WithOne(c => c.Assembly)
                .HasForeignKey(c => c.AssemblyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- ASSEMBLY COMPONENT ---
        modelBuilder.Entity<AssemblyComponent>(entity =>
        {
            entity.HasOne(c => c.CSISection)
                .WithMany()
                .HasForeignKey(c => c.CSISectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Trade)
                .WithMany()
                .HasForeignKey(c => c.TradeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =================================================================
        // SEED DATA — Common Construction Trades
        // =================================================================

        SeedTrades(modelBuilder);

        // =================================================================
        // SEED DATA — CSI MasterFormat Divisions
        // =================================================================
        //
        // WHAT SEED DATA IS:
        //   Data that ships with your application — it exists from day one.
        //   Users don't have to enter it manually. It's "built-in."
        //
        //   Like your JERP Chart of Accounts template:
        //   When a user creates a company, they get pre-loaded accounts
        //   (1000 Assets, 2000 Liabilities, etc.).
        //
        //   Here, when the database is created, all 50 CSI divisions are
        //   pre-loaded so users can immediately start creating estimates.
        //
        // HOW HasData WORKS:
        //   modelBuilder.Entity<T>().HasData(new T { ... })
        //   
        //   EF generates a migration that INSERTs these rows.
        //   On first run: rows are inserted.
        //   On subsequent runs: EF checks if they exist and skips them.
        //
        // IMPORTANT: Seed data REQUIRES explicit Guid IDs.
        //   Normal code uses Guid.NewGuid() which generates random IDs.
        //   Seed data needs FIXED IDs so EF can track them across migrations.
        //   We use deterministic GUIDs based on the division code.
        // =================================================================

        SeedCSIDivisions(modelBuilder);
        SeedCSISections(modelBuilder);
    }

    // =====================================================================
    // SEED METHOD — All 50 CSI MasterFormat Divisions
    // =====================================================================
    // This pre-loads the complete CSI division structure.
    // Every construction estimating software ships with this data.
    // =====================================================================

    private static void SeedCSIDivisions(ModelBuilder modelBuilder)
    {
        // Helper to create deterministic GUIDs from division codes.
        // We need fixed GUIDs so migrations are repeatable.
        // "CSI-DIV-03" always produces the same GUID.
        // LESSON LEARNED: The prefix CANNOT be all zeros — Guid.Empty
        // (00000000-0000-0000-0000-000000000000) is rejected by EF Core
        // because it looks like an uninitialized ID.
        static Guid DivId(string code) => Guid.Parse(
            $"CC100000-0000-0000-0000-0000000000{code.PadLeft(2, '0')}");

        modelBuilder.Entity<CSIDivision>().HasData(
            // ── PROCUREMENT & GENERAL ──
            new CSIDivision { Id = DivId("00"), Code = "00", Name = "Procurement and Contracting Requirements", SortOrder = 0,
                Description = "Bidding documents, contracts, bonds, insurance, permits. Not actual construction — this is the paperwork." },
            new CSIDivision { Id = DivId("01"), Code = "01", Name = "General Requirements", SortOrder = 1,
                Description = "Project mobilization, temporary facilities, site cleanup, project management, safety. The 'overhead' of running a jobsite." },

            // ── SITE & EXISTING CONDITIONS ──
            new CSIDivision { Id = DivId("02"), Code = "02", Name = "Existing Conditions", SortOrder = 2,
                Description = "Demolition, site assessment, hazardous material abatement (asbestos, lead), subsurface investigation." },

            // ── CONCRETE ──
            new CSIDivision { Id = DivId("03"), Code = "03", Name = "Concrete", SortOrder = 3,
                Description = "ALL concrete work: forming, reinforcing (rebar), placing, finishing, curing. Foundations, slabs, walls, columns, beams." },

            // ── MASONRY ──
            new CSIDivision { Id = DivId("04"), Code = "04", Name = "Masonry", SortOrder = 4,
                Description = "Brick, concrete block (CMU), stone, mortar, grout. Masonry walls, veneer, fireplaces." },

            // ── METALS ──
            new CSIDivision { Id = DivId("05"), Code = "05", Name = "Metals", SortOrder = 5,
                Description = "Structural steel, metal deck, stairs, railings, misc metals. The skeleton of commercial buildings." },

            // ── WOOD, PLASTICS, COMPOSITES ──
            new CSIDivision { Id = DivId("06"), Code = "06", Name = "Wood, Plastics, and Composites", SortOrder = 6,
                Description = "Rough carpentry (framing), finish carpentry (trim), casework (cabinets), architectural woodwork." },

            // ── THERMAL & MOISTURE PROTECTION ──
            new CSIDivision { Id = DivId("07"), Code = "07", Name = "Thermal and Moisture Protection", SortOrder = 7,
                Description = "Roofing, insulation, waterproofing, fireproofing, sealants, vapor barriers. Keeps water out and heat in." },

            // ── OPENINGS ──
            new CSIDivision { Id = DivId("08"), Code = "08", Name = "Openings", SortOrder = 8,
                Description = "Doors (wood, metal, glass), windows, storefronts, curtain walls, hardware (knobs, hinges, closers)." },

            // ── FINISHES ──
            new CSIDivision { Id = DivId("09"), Code = "09", Name = "Finishes", SortOrder = 9,
                Description = "Drywall (gypsum board), plaster, tile, flooring (carpet, VCT, wood), painting, wall coverings, ceilings." },

            // ── SPECIALTIES ──
            new CSIDivision { Id = DivId("10"), Code = "10", Name = "Specialties", SortOrder = 10,
                Description = "Signage, toilet partitions, lockers, fire extinguishers, corner guards, marker boards." },

            // ── EQUIPMENT ──
            new CSIDivision { Id = DivId("11"), Code = "11", Name = "Equipment", SortOrder = 11,
                Description = "Built-in equipment: kitchen equipment, laundry, medical equipment, lab equipment." },

            // ── FURNISHINGS ──
            new CSIDivision { Id = DivId("12"), Code = "12", Name = "Furnishings", SortOrder = 12,
                Description = "Furniture, window treatments, rugs, artwork. Usually FF&E (Furniture, Fixtures & Equipment)." },

            // ── SPECIAL CONSTRUCTION ──
            new CSIDivision { Id = DivId("13"), Code = "13", Name = "Special Construction", SortOrder = 13,
                Description = "Clean rooms, swimming pools, ice rinks, radiation protection, seismic isolation." },

            // ── CONVEYING EQUIPMENT ──
            new CSIDivision { Id = DivId("14"), Code = "14", Name = "Conveying Equipment", SortOrder = 14,
                Description = "Elevators, escalators, dumbwaiters, material handling systems." },

            // ── RESERVED (15-20) — gaps in the numbering ──

            // ── FIRE SUPPRESSION ──
            new CSIDivision { Id = DivId("21"), Code = "21", Name = "Fire Suppression", SortOrder = 21,
                Description = "Sprinkler systems, standpipes, fire pumps. Required by code in most commercial buildings." },

            // ── PLUMBING ──
            new CSIDivision { Id = DivId("22"), Code = "22", Name = "Plumbing", SortOrder = 22,
                Description = "Water supply, drainage, fixtures (sinks, toilets), water heaters, gas piping." },

            // ── HVAC ──
            new CSIDivision { Id = DivId("23"), Code = "23", Name = "Heating, Ventilating, and Air Conditioning", SortOrder = 23,
                Description = "HVAC systems: furnaces, AC units, ductwork, controls, ventilation. Usually 15-20% of total project cost." },

            // ── RESERVED (24) ──

            // ── INTEGRATED AUTOMATION ──
            new CSIDivision { Id = DivId("25"), Code = "25", Name = "Integrated Automation", SortOrder = 25,
                Description = "Building automation systems (BAS), energy management, integrated controls." },

            // ── ELECTRICAL ──
            new CSIDivision { Id = DivId("26"), Code = "26", Name = "Electrical", SortOrder = 26,
                Description = "Power distribution, lighting, wiring, panels, transformers, generators. Usually 12-18% of total project cost." },

            // ── COMMUNICATIONS ──
            new CSIDivision { Id = DivId("27"), Code = "27", Name = "Communications", SortOrder = 27,
                Description = "Data/telecom cabling, fire alarm, security, intercom, AV systems." },

            // ── ELECTRONIC SAFETY & SECURITY ──
            new CSIDivision { Id = DivId("28"), Code = "28", Name = "Electronic Safety and Security", SortOrder = 28,
                Description = "Access control, video surveillance, intrusion detection, mass notification." },

            // ── RESERVED (29-30) ──

            // ── EARTHWORK ──
            new CSIDivision { Id = DivId("31"), Code = "31", Name = "Earthwork", SortOrder = 31,
                Description = "Site grading, excavation, fill, compaction, soil stabilization. Preparing the ground for construction." },

            // ── EXTERIOR IMPROVEMENTS ──
            new CSIDivision { Id = DivId("32"), Code = "32", Name = "Exterior Improvements", SortOrder = 32,
                Description = "Parking lots, sidewalks, landscaping, fencing, retaining walls, irrigation." },

            // ── UTILITIES ──
            new CSIDivision { Id = DivId("33"), Code = "33", Name = "Utilities", SortOrder = 33,
                Description = "Underground utilities: water mains, sanitary sewer, storm drainage, gas lines." },

            // ── TRANSPORTATION ──
            new CSIDivision { Id = DivId("34"), Code = "34", Name = "Transportation", SortOrder = 34,
                Description = "Highways, railroads, bridges, airport pavements. Heavy civil infrastructure." },

            // ── WATERWAY & MARINE ──
            new CSIDivision { Id = DivId("35"), Code = "35", Name = "Waterway and Marine Construction", SortOrder = 35,
                Description = "Dams, levees, docks, piers, dredging, coastal protection." },

            // ── RESERVED (36-39) ──

            // ── PROCESS INTEGRATION (40-48) — Industrial ──
            new CSIDivision { Id = DivId("40"), Code = "40", Name = "Process Integration", SortOrder = 40,
                Description = "Industrial process piping, instrumentation, controls. Factories, refineries, plants." },

            new CSIDivision { Id = DivId("41"), Code = "41", Name = "Material Processing and Handling Equipment", SortOrder = 41,
                Description = "Conveyors, cranes, hoists, storage equipment for industrial facilities." },

            new CSIDivision { Id = DivId("42"), Code = "42", Name = "Process Heating, Cooling, and Drying Equipment", SortOrder = 42,
                Description = "Boilers, furnaces, heat exchangers, cooling towers for industrial processes." },

            new CSIDivision { Id = DivId("43"), Code = "43", Name = "Process Gas and Liquid Handling, Purification, and Storage Equipment", SortOrder = 43,
                Description = "Tanks, vessels, pumps, compressors, filters for industrial processing." },

            new CSIDivision { Id = DivId("44"), Code = "44", Name = "Pollution and Waste Control Equipment", SortOrder = 44,
                Description = "Air scrubbers, water treatment, waste handling for environmental compliance." },

            new CSIDivision { Id = DivId("46"), Code = "46", Name = "Water and Wastewater Equipment", SortOrder = 46,
                Description = "Water treatment plants, wastewater processing, pumping stations." },

            new CSIDivision { Id = DivId("48"), Code = "48", Name = "Electrical Power Generation", SortOrder = 48,
                Description = "Power plants, solar arrays, wind turbines, generators, transformers." }
        );
    }

    // =====================================================================
    // SEED METHOD — Common CSI Sections (most-used sections per division)
    // =====================================================================
    // We don't seed ALL sections (there are thousands). We seed the most
    // commonly used ones — about 5-8 per major division. Users can add more.
    // =====================================================================

    private static void SeedCSISections(ModelBuilder modelBuilder)
    {
        // Helper: creates a deterministic GUID from a section code
        static Guid SecId(string code)
        {
            var hash = code.Replace(" ", "").PadRight(12, '0');
            return Guid.Parse($"10000000-0000-0000-0000-{hash.Substring(0, 12)}");
        }
        static Guid DivId(string code) => Guid.Parse(
            $"CC100000-0000-0000-0000-0000000000{code.PadLeft(2, '0')}");

        modelBuilder.Entity<CSISection>().HasData(

            // ── DIVISION 01: GENERAL REQUIREMENTS ──
            new CSISection { Id = SecId("01 10 00"), CSIDivisionId = DivId("01"), Code = "01 10 00",
                Name = "Summary of Work", SortOrder = 1, DefaultUnitOfMeasure = "LS" },
            new CSISection { Id = SecId("01 50 00"), CSIDivisionId = DivId("01"), Code = "01 50 00",
                Name = "Temporary Facilities and Controls", SortOrder = 2, DefaultUnitOfMeasure = "LS" },
            new CSISection { Id = SecId("01 70 00"), CSIDivisionId = DivId("01"), Code = "01 70 00",
                Name = "Execution and Closeout Requirements", SortOrder = 3, DefaultUnitOfMeasure = "LS" },
            new CSISection { Id = SecId("01 74 00"), CSIDivisionId = DivId("01"), Code = "01 74 00",
                Name = "Construction Waste Management", SortOrder = 4, DefaultUnitOfMeasure = "LS" },

            // ── DIVISION 02: EXISTING CONDITIONS ──
            new CSISection { Id = SecId("02 41 00"), CSIDivisionId = DivId("02"), Code = "02 41 00",
                Name = "Demolition", SortOrder = 1, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("02 82 00"), CSIDivisionId = DivId("02"), Code = "02 82 00",
                Name = "Asbestos Remediation", SortOrder = 2, DefaultUnitOfMeasure = "SF" },

            // ── DIVISION 03: CONCRETE ──
            new CSISection { Id = SecId("03 10 00"), CSIDivisionId = DivId("03"), Code = "03 10 00",
                Name = "Concrete Forming and Accessories", SortOrder = 1, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("03 20 00"), CSIDivisionId = DivId("03"), Code = "03 20 00",
                Name = "Concrete Reinforcing", SortOrder = 2, DefaultUnitOfMeasure = "TON" },
            new CSISection { Id = SecId("03 30 00"), CSIDivisionId = DivId("03"), Code = "03 30 00",
                Name = "Cast-in-Place Concrete", SortOrder = 3, DefaultUnitOfMeasure = "CY" },
            new CSISection { Id = SecId("03 35 00"), CSIDivisionId = DivId("03"), Code = "03 35 00",
                Name = "Concrete Finishing", SortOrder = 4, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("03 40 00"), CSIDivisionId = DivId("03"), Code = "03 40 00",
                Name = "Precast Concrete", SortOrder = 5, DefaultUnitOfMeasure = "SF" },

            // ── DIVISION 04: MASONRY ──
            new CSISection { Id = SecId("04 20 00"), CSIDivisionId = DivId("04"), Code = "04 20 00",
                Name = "Unit Masonry (CMU Block)", SortOrder = 1, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("04 21 00"), CSIDivisionId = DivId("04"), Code = "04 21 00",
                Name = "Clay Unit Masonry (Brick)", SortOrder = 2, DefaultUnitOfMeasure = "SF" },

            // ── DIVISION 05: METALS ──
            new CSISection { Id = SecId("05 10 00"), CSIDivisionId = DivId("05"), Code = "05 10 00",
                Name = "Structural Metal Framing", SortOrder = 1, DefaultUnitOfMeasure = "TON" },
            new CSISection { Id = SecId("05 21 00"), CSIDivisionId = DivId("05"), Code = "05 21 00",
                Name = "Steel Joist Framing", SortOrder = 2, DefaultUnitOfMeasure = "TON" },
            new CSISection { Id = SecId("05 31 00"), CSIDivisionId = DivId("05"), Code = "05 31 00",
                Name = "Steel Decking", SortOrder = 3, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("05 50 00"), CSIDivisionId = DivId("05"), Code = "05 50 00",
                Name = "Metal Fabrications (Miscellaneous)", SortOrder = 4, DefaultUnitOfMeasure = "LB" },
            new CSISection { Id = SecId("05 51 00"), CSIDivisionId = DivId("05"), Code = "05 51 00",
                Name = "Metal Stairs", SortOrder = 5, DefaultUnitOfMeasure = "EA" },
            new CSISection { Id = SecId("05 52 00"), CSIDivisionId = DivId("05"), Code = "05 52 00",
                Name = "Metal Railings", SortOrder = 6, DefaultUnitOfMeasure = "LF" },

            // ── DIVISION 06: WOOD / CARPENTRY ──
            new CSISection { Id = SecId("06 10 00"), CSIDivisionId = DivId("06"), Code = "06 10 00",
                Name = "Rough Carpentry (Framing)", SortOrder = 1, DefaultUnitOfMeasure = "MBF" },
            new CSISection { Id = SecId("06 20 00"), CSIDivisionId = DivId("06"), Code = "06 20 00",
                Name = "Finish Carpentry (Trim)", SortOrder = 2, DefaultUnitOfMeasure = "LF" },
            new CSISection { Id = SecId("06 41 00"), CSIDivisionId = DivId("06"), Code = "06 41 00",
                Name = "Architectural Casework (Cabinets)", SortOrder = 3, DefaultUnitOfMeasure = "LF" },

            // ── DIVISION 07: THERMAL & MOISTURE ──
            new CSISection { Id = SecId("07 10 00"), CSIDivisionId = DivId("07"), Code = "07 10 00",
                Name = "Dampproofing and Waterproofing", SortOrder = 1, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("07 21 00"), CSIDivisionId = DivId("07"), Code = "07 21 00",
                Name = "Thermal Insulation", SortOrder = 2, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("07 50 00"), CSIDivisionId = DivId("07"), Code = "07 50 00",
                Name = "Membrane Roofing", SortOrder = 3, DefaultUnitOfMeasure = "SQ" },
            new CSISection { Id = SecId("07 60 00"), CSIDivisionId = DivId("07"), Code = "07 60 00",
                Name = "Flashing and Sheet Metal", SortOrder = 4, DefaultUnitOfMeasure = "LF" },
            new CSISection { Id = SecId("07 92 00"), CSIDivisionId = DivId("07"), Code = "07 92 00",
                Name = "Joint Sealants (Caulking)", SortOrder = 5, DefaultUnitOfMeasure = "LF" },

            // ── DIVISION 08: OPENINGS ──
            new CSISection { Id = SecId("08 11 00"), CSIDivisionId = DivId("08"), Code = "08 11 00",
                Name = "Metal Doors and Frames", SortOrder = 1, DefaultUnitOfMeasure = "EA" },
            new CSISection { Id = SecId("08 14 00"), CSIDivisionId = DivId("08"), Code = "08 14 00",
                Name = "Wood Doors", SortOrder = 2, DefaultUnitOfMeasure = "EA" },
            new CSISection { Id = SecId("08 41 00"), CSIDivisionId = DivId("08"), Code = "08 41 00",
                Name = "Entrances and Storefronts", SortOrder = 3, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("08 50 00"), CSIDivisionId = DivId("08"), Code = "08 50 00",
                Name = "Windows", SortOrder = 4, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("08 71 00"), CSIDivisionId = DivId("08"), Code = "08 71 00",
                Name = "Door Hardware", SortOrder = 5, DefaultUnitOfMeasure = "EA" },

            // ── DIVISION 09: FINISHES (most line items in typical estimates) ──
            new CSISection { Id = SecId("09 21 00"), CSIDivisionId = DivId("09"), Code = "09 21 00",
                Name = "Plaster and Gypsum Board Assemblies", SortOrder = 1, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("09 29 00"), CSIDivisionId = DivId("09"), Code = "09 29 00",
                Name = "Gypsum Board (Drywall)", SortOrder = 2, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("09 30 00"), CSIDivisionId = DivId("09"), Code = "09 30 00",
                Name = "Tiling", SortOrder = 3, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("09 51 00"), CSIDivisionId = DivId("09"), Code = "09 51 00",
                Name = "Acoustical Ceilings", SortOrder = 4, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("09 65 00"), CSIDivisionId = DivId("09"), Code = "09 65 00",
                Name = "Resilient Flooring (VCT, Sheet Vinyl)", SortOrder = 5, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("09 68 00"), CSIDivisionId = DivId("09"), Code = "09 68 00",
                Name = "Carpeting", SortOrder = 6, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("09 91 00"), CSIDivisionId = DivId("09"), Code = "09 91 00",
                Name = "Painting", SortOrder = 7, DefaultUnitOfMeasure = "SF" },

            // ── DIVISION 10: SPECIALTIES ──
            new CSISection { Id = SecId("10 14 00"), CSIDivisionId = DivId("10"), Code = "10 14 00",
                Name = "Signage", SortOrder = 1, DefaultUnitOfMeasure = "EA" },
            new CSISection { Id = SecId("10 21 00"), CSIDivisionId = DivId("10"), Code = "10 21 00",
                Name = "Toilet Compartments (Partitions)", SortOrder = 2, DefaultUnitOfMeasure = "EA" },
            new CSISection { Id = SecId("10 28 00"), CSIDivisionId = DivId("10"), Code = "10 28 00",
                Name = "Toilet, Bath, and Laundry Accessories", SortOrder = 3, DefaultUnitOfMeasure = "EA" },
            new CSISection { Id = SecId("10 44 00"), CSIDivisionId = DivId("10"), Code = "10 44 00",
                Name = "Fire Protection Specialties (Extinguishers)", SortOrder = 4, DefaultUnitOfMeasure = "EA" },

            // ── DIVISION 14: CONVEYING ──
            new CSISection { Id = SecId("14 20 00"), CSIDivisionId = DivId("14"), Code = "14 20 00",
                Name = "Elevators", SortOrder = 1, DefaultUnitOfMeasure = "EA" },

            // ── DIVISION 21: FIRE SUPPRESSION ──
            new CSISection { Id = SecId("21 10 00"), CSIDivisionId = DivId("21"), Code = "21 10 00",
                Name = "Water-Based Fire Suppression (Sprinklers)", SortOrder = 1, DefaultUnitOfMeasure = "SF" },

            // ── DIVISION 22: PLUMBING ──
            new CSISection { Id = SecId("22 10 00"), CSIDivisionId = DivId("22"), Code = "22 10 00",
                Name = "Plumbing Piping", SortOrder = 1, DefaultUnitOfMeasure = "LF" },
            new CSISection { Id = SecId("22 40 00"), CSIDivisionId = DivId("22"), Code = "22 40 00",
                Name = "Plumbing Fixtures", SortOrder = 2, DefaultUnitOfMeasure = "EA" },
            new CSISection { Id = SecId("22 33 00"), CSIDivisionId = DivId("22"), Code = "22 33 00",
                Name = "Electric Domestic Water Heaters", SortOrder = 3, DefaultUnitOfMeasure = "EA" },

            // ── DIVISION 23: HVAC ──
            new CSISection { Id = SecId("23 30 00"), CSIDivisionId = DivId("23"), Code = "23 30 00",
                Name = "HVAC Air Distribution (Ductwork)", SortOrder = 1, DefaultUnitOfMeasure = "LB" },
            new CSISection { Id = SecId("23 73 00"), CSIDivisionId = DivId("23"), Code = "23 73 00",
                Name = "Indoor Central-Station Air-Handling Units", SortOrder = 2, DefaultUnitOfMeasure = "EA" },
            new CSISection { Id = SecId("23 81 00"), CSIDivisionId = DivId("23"), Code = "23 81 00",
                Name = "Decentralized HVAC Equipment (Split Systems)", SortOrder = 3, DefaultUnitOfMeasure = "EA" },

            // ── DIVISION 26: ELECTRICAL ──
            new CSISection { Id = SecId("26 05 00"), CSIDivisionId = DivId("26"), Code = "26 05 00",
                Name = "Common Work Results for Electrical", SortOrder = 1, DefaultUnitOfMeasure = "LS" },
            new CSISection { Id = SecId("26 20 00"), CSIDivisionId = DivId("26"), Code = "26 20 00",
                Name = "Low-Voltage Electrical Power Generation (Panels, Transformers)", SortOrder = 2, DefaultUnitOfMeasure = "EA" },
            new CSISection { Id = SecId("26 24 00"), CSIDivisionId = DivId("26"), Code = "26 24 00",
                Name = "Switchboards and Panelboards", SortOrder = 3, DefaultUnitOfMeasure = "EA" },
            new CSISection { Id = SecId("26 27 00"), CSIDivisionId = DivId("26"), Code = "26 27 00",
                Name = "Low-Voltage Distribution Equipment", SortOrder = 4, DefaultUnitOfMeasure = "EA" },
            new CSISection { Id = SecId("26 50 00"), CSIDivisionId = DivId("26"), Code = "26 50 00",
                Name = "Lighting", SortOrder = 5, DefaultUnitOfMeasure = "EA" },

            // ── DIVISION 27: COMMUNICATIONS ──
            new CSISection { Id = SecId("27 10 00"), CSIDivisionId = DivId("27"), Code = "27 10 00",
                Name = "Structured Cabling", SortOrder = 1, DefaultUnitOfMeasure = "EA" },

            // ── DIVISION 28: SAFETY & SECURITY ──
            new CSISection { Id = SecId("28 31 00"), CSIDivisionId = DivId("28"), Code = "28 31 00",
                Name = "Fire Detection and Alarm", SortOrder = 1, DefaultUnitOfMeasure = "EA" },

            // ── DIVISION 31: EARTHWORK ──
            new CSISection { Id = SecId("31 10 00"), CSIDivisionId = DivId("31"), Code = "31 10 00",
                Name = "Site Clearing", SortOrder = 1, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("31 20 00"), CSIDivisionId = DivId("31"), Code = "31 20 00",
                Name = "Earth Moving (Excavation/Grading)", SortOrder = 2, DefaultUnitOfMeasure = "CY" },
            new CSISection { Id = SecId("31 23 00"), CSIDivisionId = DivId("31"), Code = "31 23 00",
                Name = "Excavation and Fill", SortOrder = 3, DefaultUnitOfMeasure = "CY" },

            // ── DIVISION 32: EXTERIOR IMPROVEMENTS ──
            new CSISection { Id = SecId("32 12 00"), CSIDivisionId = DivId("32"), Code = "32 12 00",
                Name = "Flexible Paving (Asphalt)", SortOrder = 1, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("32 13 00"), CSIDivisionId = DivId("32"), Code = "32 13 00",
                Name = "Rigid Paving (Concrete Sidewalks/Curbs)", SortOrder = 2, DefaultUnitOfMeasure = "SF" },
            new CSISection { Id = SecId("32 90 00"), CSIDivisionId = DivId("32"), Code = "32 90 00",
                Name = "Planting (Landscaping)", SortOrder = 3, DefaultUnitOfMeasure = "SF" },

            // ── DIVISION 33: UTILITIES ──
            new CSISection { Id = SecId("33 10 00"), CSIDivisionId = DivId("33"), Code = "33 10 00",
                Name = "Water Utilities", SortOrder = 1, DefaultUnitOfMeasure = "LF" },
            new CSISection { Id = SecId("33 30 00"), CSIDivisionId = DivId("33"), Code = "33 30 00",
                Name = "Sanitary Sewerage", SortOrder = 2, DefaultUnitOfMeasure = "LF" },
            new CSISection { Id = SecId("33 40 00"), CSIDivisionId = DivId("33"), Code = "33 40 00",
                Name = "Storm Drainage", SortOrder = 3, DefaultUnitOfMeasure = "LF" }
        );
    }

    // =====================================================================
    // SEED METHOD — Common Construction Trades
    // =====================================================================
    // Pre-loads the most common construction trades.
    // Users can add more, but these cover 90% of projects.
    // =====================================================================

    private static void SeedTrades(ModelBuilder modelBuilder)
    {
        // Use sequential numbers — trade codes have non-hex chars (R, P, U, etc.)
        static Guid TradeId(int num) => Guid.Parse(
            $"AA000000-0000-0000-0000-{num:D12}");

        modelBuilder.Entity<Trade>().HasData(
            // Structural trades
            new Trade { Id = TradeId(1), TradeCode = "CARP", Name = "Carpenter",
                Description = "Builds and repairs wooden structures, forms, frameworks, scaffolding" },
            new Trade { Id = TradeId(2), TradeCode = "IRON", Name = "Ironworker",
                Description = "Erects structural steel, rebar, ornamental metals" },
            new Trade { Id = TradeId(3), TradeCode = "CONC", Name = "Cement Mason",
                Description = "Places, finishes, and repairs concrete flatwork and structures" },
            new Trade { Id = TradeId(4), TradeCode = "LABR", Name = "Laborer",
                Description = "General construction labor: cleanup, demolition, material handling" },
            new Trade { Id = TradeId(5), TradeCode = "OPER", Name = "Operating Engineer",
                Description = "Operates heavy equipment: cranes, excavators, loaders, bulldozers" },

            // Finish trades
            new Trade { Id = TradeId(6), TradeCode = "DRYW", Name = "Drywall Installer",
                Description = "Hangs and finishes gypsum board (drywall/sheetrock)" },
            new Trade { Id = TradeId(7), TradeCode = "PAIN", Name = "Painter",
                Description = "Interior and exterior painting, wall coverings, coatings" },
            new Trade { Id = TradeId(8), TradeCode = "TILE", Name = "Tile Setter",
                Description = "Installs ceramic, porcelain, stone, and glass tile" },
            new Trade { Id = TradeId(9), TradeCode = "FLOR", Name = "Floor Layer",
                Description = "Installs carpet, vinyl, hardwood, laminate flooring" },

            // MEP trades (Mechanical, Electrical, Plumbing)
            new Trade { Id = TradeId(10), TradeCode = "ELEC", Name = "Electrician",
                Description = "Installs and maintains electrical systems, wiring, panels, fixtures" },
            new Trade { Id = TradeId(11), TradeCode = "PLUM", Name = "Plumber",
                Description = "Installs water supply, drainage, gas piping, fixtures" },
            new Trade { Id = TradeId(12), TradeCode = "HVAC", Name = "Sheet Metal Worker (HVAC)",
                Description = "Fabricates and installs ductwork, HVAC systems" },
            new Trade { Id = TradeId(13), TradeCode = "PIPE", Name = "Pipefitter",
                Description = "Installs high-pressure piping, steam, process piping" },
            new Trade { Id = TradeId(14), TradeCode = "SPRK", Name = "Sprinkler Fitter",
                Description = "Installs fire suppression sprinkler systems" },

            // Specialty trades
            new Trade { Id = TradeId(15), TradeCode = "ROOF", Name = "Roofer",
                Description = "Installs and repairs roofing systems: built-up, single-ply, shingle" },
            new Trade { Id = TradeId(16), TradeCode = "GLAZ", Name = "Glazier",
                Description = "Installs glass, curtain walls, storefronts, mirrors" },
            new Trade { Id = TradeId(17), TradeCode = "INSU", Name = "Insulation Worker",
                Description = "Installs thermal and acoustic insulation" },
            new Trade { Id = TradeId(18), TradeCode = "MASO", Name = "Mason/Bricklayer",
                Description = "Lays brick, block, stone, and other masonry units" }
        );
    }

    // =====================================================================
    // SaveChanges Override — Auto-Update Timestamps
    // =====================================================================
    // This method runs EVERY TIME you save changes to the database.
    // It automatically updates the UpdatedAt timestamp on any modified entity.
    //
    // JERP EQUIVALENT: Your JerpDbContext likely has this same override.
    // =====================================================================

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            // If the entity has an UpdatedAt property, set it to now
            if (entry.Entity is Project project)
                project.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is Estimate estimate)
                estimate.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is EstimateLineItem lineItem)
                lineItem.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is TakeoffItem takeoffItem)
                takeoffItem.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
