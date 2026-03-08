# CSI MasterFormat — Educational Guide

## What is CSI MasterFormat?

**CSI MasterFormat** is the standard numbering and title classification system used throughout the North American construction industry to organize project specifications and costs. It was created by the **Construction Specifications Institute (CSI)** and is published jointly with Construction Specifications Canada (CSC).

Every construction professional — architects, contractors, estimators, owners — uses the same code numbers to refer to the same types of work. This universal language eliminates ambiguity and makes communication between project teams consistent.

---

## Why Does It Matter?

Without a standard coding system, a bid might list costs as:

> "Concrete work — $450,000  
> Steel — $380,000  
> Drywall and finishing — $210,000"

With CSI MasterFormat, the same bid lists:

> "03 00 00 — Concrete — $450,000  
> 05 00 00 — Metals — $380,000  
> 09 00 00 — Finishes — $210,000"

Every project stakeholder immediately knows what those numbers mean, how to compare them to other projects, and where to find the specifications.

---

## How the Number System Works

CSI MasterFormat uses a hierarchical numbering system with three levels:

```
XX  00  00
│   │   └── Sub-section (specific work type)
│   └────── Section (major category within the division)
└────────── Division (broad category of work)
```

**Examples:**

| Code | Name | Description |
|------|------|-------------|
| `03` | Concrete | Top-level division |
| `03 30 00` | Cast-in-Place Concrete | Major section within Concrete |
| `03 30 53` | Miscellaneous Cast-in-Place Concrete | Specific sub-type |
| `09` | Finishes | Top-level division |
| `09 21 16` | Gypsum Board Assemblies | Drywall section |
| `09 91 00` | Painting | Paint section |

---

## The 34 Divisions

| Division | Name | Common Work Types |
|----------|------|-------------------|
| **01** | General Requirements | Temporary facilities, submittals, project closeout |
| **02** | Existing Conditions | Demolition, soil investigation, hazmat removal |
| **03** | Concrete | Formwork, rebar, cast-in-place, precast, post-tension |
| **04** | Masonry | Brick, CMU block, stone, mortar |
| **05** | Metals | Structural steel, joists, decking, misc. metals, stairs |
| **06** | Wood, Plastics, and Composites | Rough framing, finish carpentry, millwork |
| **07** | Thermal and Moisture Protection | Roofing, insulation, waterproofing, caulking |
| **08** | Openings | Doors, frames, hardware, windows, glazing, curtain walls |
| **09** | Finishes | Drywall, plaster, tile, flooring, painting, acoustical |
| **10** | Specialties | Toilet accessories, fire extinguishers, signage, lockers |
| **11** | Equipment | Kitchen equipment, loading dock, laundry, lab equipment |
| **12** | Furnishings | Window treatments, furniture, artwork |
| **13** | Special Construction | Pre-engineered buildings, aquatic facilities, vaults |
| **14** | Conveying Equipment | Elevators, escalators, lifts |
| **21** | Fire Suppression | Sprinkler systems, fire pumps |
| **22** | Plumbing | Pipes, fixtures, water heaters, medical gas |
| **23** | HVAC | Ductwork, air handling units, boilers, controls |
| **25** | Integrated Automation | Building automation systems (BAS/BMS) |
| **26** | Electrical | Power distribution, lighting, branch circuits |
| **27** | Communications | Data cabling, audio-visual, public address |
| **28** | Electronic Safety & Security | Fire alarm, security cameras, access control |
| **31** | Earthwork | Excavation, grading, fill, compaction, dewatering |
| **32** | Exterior Improvements | Paving, fencing, landscaping, irrigation, striping |
| **33** | Utilities | Underground piping, storm drains, manholes |
| **34** | Transportation | Railways, highways, bridges, tunnels (heavy civil) |
| **35** | Waterways & Marine | Docks, seawalls, dredging |
| **40** | Process Interconnections | Industrial piping systems |
| **41** | Material Processing | Cranes, conveyors, industrial equipment |
| **42** | Process Heating / Cooling | Industrial heat exchangers, boilers |
| **43** | Gas / Liquid Handling | Industrial pumps, compressors |
| **44** | Pollution Control | Scrubbers, filters |
| **45** | Industry-Specific Manufacturing | Specialized plant equipment |
| **48** | Electrical Power Generation | Generators, solar, wind turbines |

---

## How BuildEstimate Uses CSI MasterFormat

### 1. Code Organization
Every estimate line item is tagged with a CSI section code. This allows the system to:
- Group line items by division for the bid summary report
- Sort costs in a consistent, industry-standard order
- Enable cost comparison across projects (e.g., "Our concrete costs are always Division 03")

### 2. Seeded Reference Data
All 34 divisions and the most common sections are **automatically seeded** into the database when you run the first migration. You never need to enter them manually.

```csharp
// From BuildEstimateDbContext.OnModelCreating():
modelBuilder.Entity<CSIDivision>().HasData(
    new CSIDivision { Code = "03", Name = "Concrete", SortOrder = 3 },
    new CSIDivision { Code = "05", Name = "Metals", SortOrder = 5 },
    // ... all 34 divisions
);
```

### 3. Assembly Linkage
Every component in an Assembly template is linked to a CSI section. When you apply an assembly to an estimate, each created line item automatically inherits the correct CSI code. This means the bid summary instantly shows accurate division breakdowns without any manual categorization.

### 4. Production Rate Lookup
Production rates (hours per unit of work) are stored by CSI section + trade combination. When you use the labor lookup endpoint, the system finds the appropriate rate for:

```
CSI Section: 09 21 16 — Gypsum Board
Trade: Drywall Finisher
County: Los Angeles
→ Returns: 0.017 hours per SF, $65.42/hr prevailing wage
→ Labor cost: 0.017 × $65.42 = $1.11/SF
```

---

## Example: Reading a Bid Summary by Division

A typical commercial project bid summary might look like this:

```
Division  Name                        Cost        %
────────  ──────────────────────────  ──────────  ──────
03        Concrete                    $485,000    18.2%
05        Metals                      $310,000    11.6%
06        Wood & Plastics             $145,000     5.4%
07        Moisture Protection          $98,000     3.7%
08        Openings                    $210,000     7.9%
09        Finishes                    $380,000    14.2%
22        Plumbing                    $175,000     6.6%
23        HVAC                        $290,000    10.9%
26        Electrical                  $310,000    11.6%
                                     ──────────
          Direct Cost               $2,403,000   90.1%
          Overhead (10%)              $240,300    9.0%
          Profit (10%)                $240,300    9.0%
          Contingency (5%)            $120,150    4.5%
                                     ──────────
          TOTAL BID PRICE           $3,003,750
```

Every number in the "By Division" section comes from the line items tagged with that CSI code.

---

## Resources

- [CSI MasterFormat Official Page](https://www.csiresources.org/practice/standards/masterformat)
- [2020 MasterFormat Overview (PDF)](https://www.csiresources.org) — the current edition used in this application
- [RSMeans Cost Data](https://www.rsmeans.com) — industry-standard cost database organized by CSI code
