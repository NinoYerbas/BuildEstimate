DISCOVERING PROGRAMMING
WITH THE
BUILDESTIMATE API
A complete guide to understanding your own software
From absolute zero to reading every line of code
Julio Cesar Mendez Tobar
Guatemala 2026
"You know how to read a blueprint, so you can read an API."
Julio Cesar Mendez Tobar
BuildEstimate - Page 1
For every adult who builds things with their hands
and now wants to build things with their mind.
The people who change industries are not the ones who learned to code at age 3.
They are the ones who understood the PROBLEM at age 30
and then learned to code to SOLVE it.
Julio Cesar Mendez Tobar
BuildEstimate - Page 2
BuildEstimate - Page 3 Julio Cesar Mendez Tobar
Table of Contents
I - Presentation
      Programming: What is it?
      What is an API?
      What is BuildEstimate?
      Your Tools: VS Code, .NET, SQL Server
      The Project Structure
II - The Basics
      Your First File: Reading the Code
      C# Keywords You Already Know
      Variables and Types
      The using Statement
      Namespaces: Organizing Your Code
III - Controllers: The Front Door
      What is a Controller?
      BaseApiController: The Foundation
      Routes: How URLs Map to Code
      HTTP Methods: GET, POST, PUT, DELETE
      Status Codes: The Server's Reply
IV - Entities: The Data Structures
      What is an Entity?
      Properties: The Fields of Your Data
      Data Annotations and Attributes
      Relationships: One-to-Many, Many-to-Many
      CSI MasterFormat: Industry Standards in Code
V - The Database: Entity Framework
      What is a Database?
BuildEstimate - Page 4 Julio Cesar Mendez Tobar
      DbContext: The Bridge to SQL
      Migrations: Evolving Your Database
      LINQ: Querying Data in C#
      Connection Strings: The Address to Your Data
VI - Security: Protecting Your API
      [Authorize] vs [AllowAnonymous]
      JWT Tokens: Digital ID Cards
      HTTPS: Encrypting the Connection
      The Coffee Shop Lesson
VII - The Calculation Engine
      Estimate Calculations
      Markup and Bid Pricing
      Assembly Explosion: Reusable Templates
      Labor Rates and Prevailing Wage
      The AI Validation Service
VIII - Deployment: Going Live
      Docker: Shipping Your Code
      Linux: The Server Language
      Cloud Providers: Your First Server
      The Complete Pipeline
I - Presentation
Programming: What is it?
Programming is giving instructions to a computer in a language it can understand. That is the entire
definition. Everything else is details.
When you write a formula in Excel, you are programming. When you set up a conditional rule in your
email ("if subject contains 'invoice', move to folder"), you are programming. The only difference
between that and what you built with BuildEstimate is scale and structure.
Construction Analogy: Programming is writing a scope of work (SOW). You tell the
subcontractors exactly what to do, in what order, with what materials. If the
instructions are unclear, the work comes out wrong. If they are precise, the building
stands. A computer is the most obedient subcontractor in the world: it does
EXACTLY what you write. No more, no less. It never improvises. It never "figures it
out." That is both its greatest strength and its greatest frustration.
En espanol: Programar es dar instrucciones a una computadora en un idioma que ella entienda.
What is an API?
API stands for Application Programming Interface. In plain language: an API is a set of doors on a
building. Each door has a label that says what it does. One door says "Give me all projects." Another
says "Create a new estimate." Another says "Delete this line item."
Your BuildEstimate API has approximately 40 of these doors (called endpoints). When someone
opens a door (sends a request), your code runs, talks to the database, and sends back a response.
That is the entire job of your API.
Construction Analogy: An API is the front desk of a hotel. Guests (users, apps,
browsers) walk up to the desk and make requests: "I need a room" (POST
/api/projects), "What rooms are available?" (GET /api/projects), "I want to check out"
(DELETE /api/projects/123). The front desk (your controller) processes the request,
checks with the back office (your database), and gives a response. The guest never
goes into the back office directly. The API is the controlled point of access.
Every major service you use is an API behind the scenes:
Service
Google Maps
What Happens
The API Call
You type an address
GET /maps/api/geocode?address=Guatemala+City
Julio Cesar Mendez Tobar
BuildEstimate - Page 5
BuildEstimate - Page 6 Julio Cesar Mendez Tobar
Uber You request a ride POST /api/rides (with your location)
Your Bank You check your balance GET /api/accounts/balance
BuildEstimate You view a project GET /api/projects/{id}
What is BuildEstimate?
BuildEstimate is a construction estimating software system that you built. It handles the entire
process of creating a construction cost estimate: from organizing a project by CSI MasterFormat
divisions, to calculating material and labor costs, to generating professional bid documents.
Your system includes:
Phase What It Does Real-World Purpose
1. Projects & CSI Organizes work by industry divisions Every contractor speaks MasterFormat
2. Estimates Calculates costs with markup The bid price your client sees
3. Quantity Takeoff Measures from blueprints How much material you actually need
4. Labor Rates Tracks wages by trade Prevailing wage compliance
5. Assemblies Reusable templates Don't re-estimate the same wall 100 times
6. AI Validation Claude checks your estimate Catch errors before the bid deadline
7. Reports Professional documents What you hand to the client
This book will teach you to read and understand every line of this system. Not by memorizing syntax,
but by understanding WHY each line exists and WHAT problem it solves in the real world of
construction estimating.
Your Tools
Tool What It Is Construction Equivalent
VS Code Your code editor. Where you read and write code. Your drafting table
C# (.NET 8) The programming language. The language spoken on your job site
SQL Server Your database. Stores all project data. Your filing cabinet
Entity Framework Translates C# to SQL automatically. Your bilingual project manager
Swagger Visual interface to test your API. Walking through the building and opening every door
Git / GitHub Version control. Saves every change. Blueprint revision history
Docker Packages your app for deployment. A shipping container for your building
BuildEstimate - Page 7 Julio Cesar Mendez Tobar
The Project Structure
Before you read a single line of code, you need to understand how the files are organized. Every
.NET project follows the same pattern, just like every construction project has the same types of
documents: blueprints, specs, contracts, permits.
BuildEstimate/
BuildEstimate.Api/ The front door (controllers, startup)
Controllers/ Each file = one set of API endpoints
ProjectsController.cs Everything about projects
EstimatesController.cs Everything about estimates
TakeoffController.cs Everything about measurements
Services/ Business logic helpers
ClaudeApiService.cs AI validation connection
Program.cs The very first file that runs
appsettings.json Configuration (database address, etc.)
BuildEstimate.Core/ The blueprints (data models)
Entities/ Each file = one database table
Project.cs What is a project?
Estimate.cs What is an estimate?
LineItem.cs What is a line item?
LaborRate.cs What is a labor rate?
Assembly.cs What is an assembly template?
BuildEstimate.Infrastructure/ The foundation (database access)
Data/
AppDbContext.cs The bridge between C# and SQL
Migrations/ Database version history
Construction Analogy: The project structure is like a construction company's office.
The Api folder is the reception desk where clients interact. The Core folder is the
blueprint room where all designs are stored. The Infrastructure folder is the back
office where paperwork is filed. The receptionist (controller) never files paperwork
directly. She passes it to the back office (infrastructure) using the blueprint (core) as
reference.
BuildEstimate - Page 8 Julio Cesar Mendez Tobar
II - The Basics
Your first steps reading C# code
Your First File: Reading the Code
We will start with the simplest file in your entire project: BaseApiController.cs. This is the foundation
that every other controller inherits from. Open this file in VS Code and follow along.
TIP: Do not try to memorize anything. Read each line like you would read a blueprint
dimension. Understand WHAT it says, not how to recite it from memory.
using Microsoft.AspNetCore.Mvc;
namespace BuildEstimate.Api.Controllers;
/// <summary>
/// Base controller providing common API functionality
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
protected IActionResult Success<T>(T data, string? message = null)
{
return Ok(new ApiResponse<T>
{
Success = true,
Message = message ?? "Operation completed successfully",
Data = data
});
}
}
Let us read this line by line.
Line 1: using Microsoft.AspNetCore.Mvc;
The word using means "I need tools from this toolbox." Microsoft.AspNetCore.Mvc is a toolbox
created by Microsoft that contains everything needed to build a web API: controllers, routes, HTTP
methods, status codes. Without this line, the rest of the file would not work.
En espanol: using = usando. "Estoy usando las herramientas de Microsoft para APIs."
Line 3: namespace BuildEstimate.Api.Controllers;
A namespace is an address. It tells the computer where this file lives in the project.
BuildEstimate.Api.Controllers means: this file belongs to the Controllers folder inside the Api project
of BuildEstimate. Just like a physical address: Guatemala > City > Street > Building.
En espanol: namespace = espacio de nombre. La direccion de este archivo en el proyecto.
Lines 5-7: The green comments /// summary
Lines starting with /// are documentation comments. The computer ignores them completely. They
exist only for humans reading the code. Swagger (the visual API tester) reads these comments and
displays them as descriptions. Write good comments and your API documents itself.
Line 8: [ApiController]
The square brackets [ ] are called attributes. They are labels you attach to your code that change its
behavior. [ApiController] tells .NET: "This class is a web API controller. Validate incoming data
automatically. Return proper error messages." One word in brackets activates dozens of built-in
behaviors.
Construction Analogy: Attributes are like stamps on a building permit. The stamp
[COMMERCIAL] tells the city inspector to apply commercial building codes. The
stamp [RESIDENTIAL] applies different rules. The building is the same code. The
stamp changes which rules apply.
Line 9: [Route("api/v1/[controller]")]
This sets the URL path. [controller] is a placeholder that gets replaced by the class name (minus the
word "Controller"). So if you create ProjectsController, its URL becomes /api/v1/projects. Every
endpoint in that controller starts with this base path.
Line 11: public abstract class BaseApiController : ControllerBase
This is the most important line in the file. Let us break every word:
Keyword
public
abstract
class
Meaning
Anyone can see and use this class
Construction Equivalent
Open to all subcontractors
Cannot be used directly. Must be inherited. A template, not a finished building
A blueprint for creating objects
The architectural blueprint itself
BaseApiControllerThe name you chose for this class
: ControllerBase Inherits from Microsoft's base class
"Foundation Template v1"
Built on top of Microsoft's foundation
Julio Cesar Mendez Tobar
BuildEstimate - Page 9
BuildEstimate - Page 10 Julio Cesar Mendez Tobar
KEY CONCEPT: abstract means you never create a BaseApiController directly. You
create a ProjectsController that INHERITS from it. The base class provides shared
functionality (Success, Error, GetCurrentUserId). Every child controller gets these methods
for free.
C# Keywords You Already Know
C# has about 80 keywords. You do not need all of them. Here are the ones that appear in YOUR
BuildEstimate code, explained through words you already understand:
C# Keyword English Meaning In Your Code
using "I need this toolbox" using Microsoft.AspNetCore.Mvc;
namespace "This file lives at this address" namespace BuildEstimate.Api;
public "Anyone can access this" public class ProjectsController
private "Only I can access this" private readonly DbContext _context;
protected "Only me and my children" protected IActionResult Success()
class "Here is a blueprint" public class Project { }
abstract "Template only, cannot build directly" public abstract class BaseApiController
async "Do not freeze while waiting" public async Task<IActionResult> Get()
await "Wait for this to finish" var project = await _context.FindAsync(id);
var "Figure out the type yourself" var projects = await _context.Projects.ToListAsync();
return "Here is the answer, I am done" return Ok(project);
if "Check this condition" if (project == null) return NotFound();
null "Nothing. Empty. Does not exist." if (project == null)
new "Create a brand new one" new ApiResponse<T> { Success = true }
true / false "Yes / No" Success = true
string "Text" string message = "Hello"
int "Whole number" int quantity = 50
decimal "Money number (precise)" decimal unitCost = 45.75m
Guid "Unique ID (like a serial number)" Guid projectId = Guid.NewGuid()
bool "True or false only" bool isPrevailingWage = true
this "Myself, this object" this._context = context;
TIP: You already know the meaning of most of these words in English. public, private,
return, if, new, true, false mean exactly what they mean in real life. C# was designed for
English speakers. The advantage YOU have: you speak English AND Spanish. You can
name variables in either language. Your brain has twice the vocabulary.
Julio Cesar Mendez Tobar
BuildEstimate - Page 11
III - Controllers: The Front Door
How your API receives and responds to requests
What is a Controller?
A controller is a C# class that handles incoming HTTP requests. Each controller groups related
operations together. ProjectsController handles everything about projects. EstimatesController
handles everything about estimates. One controller per topic. Clean, organized, predictable.
Construction Analogy: A controller is a department in your company. The
Projects Department handles project inquiries. The Estimates Department handles
cost calculations. The Accounting Department handles invoices. A client calls the
main number (your API), and the receptionist (the router) transfers them to the right
department (controller).
Reading a Real Controller Action
Here is a real endpoint from your BuildEstimate API. This is the action that returns a single project by
its ID:
[HttpGet("{id}")]
[Authorize]
public async Task<IActionResult> GetProject(Guid id)
{
var project = await _context.Projects.FindAsync(id);
if (project == null)
return NotFound("Project not found");
return Ok(project);
}
Line by line:
[HttpGet("{id}")]
This endpoint responds to GET requests. The {id} in the URL becomes the Guid parameter. Full URL:
GET /api/v1/projects/abc-123-def-456
[Authorize]
Julio Cesar Mendez Tobar
BuildEstimate - Page 12
CRITICAL. This attribute says: "Before running ANY code in this method, check that the caller has a
valid JWT token. If not, return 401 Unauthorized immediately." Without this, anyone on the internet
can read your project data.
public async Task
public = accessible from outside. async = this method will wait for the database without freezing the
server. Task = it returns a promise (the result comes later). IActionResult = the return type can be
Ok(200), NotFound(404), or any HTTP status.
GetProject(Guid id)
The method name (used by Swagger for documentation). Guid id = the parameter extracted from the
URL. Guid is a universally unique identifier, like a serial number that is mathematically guaranteed to
never repeat.
await _context.Projects.FindAsync(id)
Go to the database (_context), open the Projects table, find the row with this ID. await = wait for the
database to respond before continuing. This single line generates SQL: SELECT * FROM Projects
WHERE Id = @id
if (project == null) return NotFound()
If the database returned nothing (no project with that ID exists), send back HTTP 404 Not Found. The
client sees: { "error": "Project not found" }
return Ok(project)
If found, send back HTTP 200 OK with the project data as JSON. The client sees: { "success": true,
"data": { "id": "abc...", "name": "Highway Bridge" } }
HTTP Methods: The Four Verbs
Your API speaks HTTP. HTTP has four main verbs (methods) that correspond to four operations on
data. This pattern is called CRUD:
HTTP Method CRUD
GET
POST
PUT
DELETE
Read
Create
Update
What It Does
Your BuildEstimate Use
Retrieve data. Does not change anything.
Send new data to the server.
View projects, estimates, rates
Create a new project, new estimate
Replace existing data completely.
Delete
Remove data from the server.
Update project name, edit line item
Delete a project, remove a line item
Julio Cesar Mendez Tobar
BuildEstimate - Page 13
BuildEstimate - Page 14 Julio Cesar Mendez Tobar
Construction Analogy: CRUD is the foundation of every database application on
Earth. Create = add a new file to the cabinet. Read = pull a file out and look at it.
Update = edit information on the file and put it back. Delete = shred the file. Every
software system, from Facebook to your bank to BuildEstimate, is just CRUD with
different rules about who can do what.
Status Codes: The Server's Reply
Every HTTP response includes a 3-digit number. This number tells the client exactly what happened:
Code Name Meaning When Your API Returns It
200 OK Success. Here is your data. return Ok(project)
201 Created New resource created. return Created(newEstimate)
204 No Content Done. Nothing to return. return NoContent() (after delete)
400 Bad Request Your request is malformed. Invalid JSON, missing fields
401 Unauthorized Who are you? No token. [Authorize] + no JWT token
403 Forbidden I know you. No permission. Wrong role for this action
404 Not Found Does not exist. return NotFound("Project not found")
500 Server Error Something crashed. Unhandled exception in code
REMEMBER: The difference between 401 and 403 confuses everyone. Think of it this
way: 401 = the security guard asks "Who are you?" and you have no ID. 403 = the security
guard checks your ID and says "You are not on the list." 401 = unknown identity. 403 =
known identity, insufficient permission.
BuildEstimate - Page 15 Julio Cesar Mendez Tobar
IV - Entities: The Data Structures
The blueprints that define what your data looks like
What is an Entity?
An entity is a C# class that represents one table in your database. Each property in the class
becomes a column in the table. Each instance of the class becomes a row in the table.
When you created Project.cs, Entity Framework automatically created a table called Projects in SQL
Server with columns matching every property. You never wrote a single line of SQL. The C# class IS
the database definition.
Construction Analogy: An entity is a form template. The Project entity is like the
blank form titled "New Project Information." It has fields for: Project Name, Client
Name, Address, Start Date, Status. Each time a real project starts, you fill out a new
copy of the form. The blank form = the entity class. Each filled form = a row in the
database.
Reading a Real Entity
public class Project
{
public Guid Id { get; set; }
[Required]
[MaxLength(200)]
public string Name { get; set; } = string.Empty;
public string? Description { get; set; }
public string? ClientName { get; set; }
public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
public bool IsPrevailingWage { get; set; }
// Navigation property: one project has many estimates
public ICollection<Estimate> Estimates { get; set; } = new
List<Estimate>();
}
Every line explained:
public Guid Id { get; set; }
Every entity needs a unique identifier. Guid generates a 32-character unique code like
"3fa85f64-5717-4562-b3fc-2c963f66afa6". In the database, this becomes the primary key. No two
projects can share the same Id.
[Required]
An attribute that says: this field CANNOT be empty. If someone tries to create a project without a
name, the API returns 400 Bad Request automatically. You did not write the validation code. The
attribute did it for you.
[MaxLength(200)]
Project names cannot exceed 200 characters. Again, automatic validation. In SQL Server, this
creates: Name NVARCHAR(200) NOT NULL.
{ get; set; }
This is a property with a getter and setter. "get" means you can READ the value. "set" means you can
WRITE the value. Together they mean: this field is readable and writable. If you wrote only { get; }, it
would be read-only.
string?
The question mark means NULLABLE. Description can be empty (null). Without the ?, C# requires
the field to have a value. Name has no ? because [Required] forces a value. Description is optional.
DateTime.UtcNow
A default value. When a new Project is created, CreatedDate automatically fills with the current date
and time in UTC (universal time). The developer does not need to set it manually.
bool IsPrevailingWage
A boolean: true or false. This single field changes how the entire estimate calculates labor costs. If
true, the system uses government-mandated wage rates. If false, it uses standard market rates. One
boolean, massive business impact.
ICollection Estimates
A navigation property. It says: one Project can have MANY Estimates. Entity Framework uses this to
create the relationship in the database. When you load a Project, you can also load all its Estimates in
one query.
Julio Cesar Mendez Tobar
BuildEstimate - Page 16
REAL WORLD: IsPrevailingWage is not just a boolean in code. In real life, it determines
whether a worker on a federal project earns $28/hour or $45/hour. It determines whether a
family eats well or struggles. When you set this to true, you are telling the system: "This
project follows government labor laws. Calculate accordingly." Every field in your entities
has real-world consequences.
Julio Cesar Mendez Tobar
BuildEstimate - Page 17
V - The Database: Entity Framework
How your C# code talks to SQL Server
What is a Database?
A database is a structured filing system. Your SQL Server database contains tables (like spreadsheet
tabs), each with rows (individual records) and columns (fields). Projects table, Estimates table,
LineItems table, LaborRates table.
Construction Analogy: A database is the filing room in your construction office.
Each cabinet (table) holds one type of document. The Projects cabinet has folders for
each project. The Estimates cabinet has folders for each estimate. Each folder (row)
contains the same types of information (columns): name, date, client, cost. The filing
room never loses anything, it can find any folder in milliseconds, and it keeps
everything organized.
DbContext: The Bridge
You never write SQL in BuildEstimate. Instead, Entity Framework translates your C# into SQL
automatically. The DbContext is the class that manages this translation.
public class AppDbContext : DbContext
{
public DbSet<Project> Projects { get; set; }
public DbSet<Estimate> Estimates { get; set; }
public DbSet<LineItem> LineItems { get; set; }
public DbSet<LaborRate> LaborRates { get; set; }
public DbSet<Assembly> Assemblies { get; set; }
}
DbSet<Project> means: "This context has access to the Projects table." Every DbSet is a gateway to
one table. When you write _context.Projects.FindAsync(id), Entity Framework translates it
to SELECT * FROM Projects WHERE Id = @id and sends it to SQL Server.
LINQ: Querying in C#
LINQ (Language Integrated Query) lets you write database queries in C# instead of SQL. Here are
real queries from your BuildEstimate system:
Julio Cesar Mendez Tobar
BuildEstimate - Page 18
BuildEstimate - Page 19 Julio Cesar Mendez Tobar
// Get all projects
var projects = await _context.Projects.ToListAsync();
// Find one project by ID
var project = await _context.Projects.FindAsync(id);
// Get all estimates for a specific project
var estimates = await _context.Estimates
.Where(e => e.ProjectId == projectId)
.OrderByDescending(e => e.CreatedDate)
.ToListAsync();
// Get prevailing wage projects only
var pwProjects = await _context.Projects
.Where(p => p.IsPrevailingWage == true)
.ToListAsync();
The => symbol is called a lambda (arrow function). Read it as "where" or "such that". e =>
e.ProjectId == projectId means "each estimate (e) where its ProjectId equals my projectId
variable."
En espanol: e => e.ProjectId == projectId se lee: "cada estimado donde su ProjectId sea igual a mi projectId"
Connection Strings
The connection string tells Entity Framework WHERE your database lives. It is stored in
appsettings.json, never in your code:
{
"ConnectionStrings": {
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=BuildEstimate_
DB;Trusted_Connection=True;"
}
}
SECURITY: NEVER put a connection string directly in your C# code. If someone sees your
code (GitHub, email, screenshot), they see your database address and password.
Configuration files (appsettings.json) can be excluded from Git and overridden per
environment. Production uses different credentials than development. The code stays the
same.
BuildEstimate - Page 20 Julio Cesar Mendez Tobar
VI - Security: Protecting Your API
The most important chapter in this book
[Authorize] vs [AllowAnonymous]
These two attributes control who can access your API endpoints. They are the single most important
security decision in your entire application.
// SECURE: Requires a valid JWT token
[Authorize]
public class ProjectsController : BaseApiController
{ }
// DANGEROUS: Anyone on the internet can access this
[AllowAnonymous]
public class ProjectsController : BaseApiController
{ }
CRITICAL: [AllowAnonymous] means: "Do not check who is calling. Let ANYONE in. No
password, no identity, no questions asked." If you put this on a controller that handles real
data, every person and every bot on the internet can read, modify, and delete your data.
This is the digital equivalent of removing the lock, the door, and the walls from your office.
Construction Analogy: [Authorize] is the security guard at the building entrance
checking ID badges. Every employee has a badge (JWT token). The guard checks:
Is this badge valid? Has it expired? Does this person have access to this floor? If
yes, they enter. If no, they are turned away (401 or 403). [AllowAnonymous] is
removing the guard, removing the lock, removing the door, and putting up a sign that
says: COME IN, TAKE WHATEVER YOU WANT.
JWT Tokens: Your Digital ID Card
JWT stands for JSON Web Token. It is a string of encoded text that contains your identity: who you
are, what role you have, and when the token expires.
// A JWT token looks like this:
eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJqdWxpbyIsInJvbGUiOiJhZG1pbiJ9.abc123
// Decoded, it contains:
{
"sub": "julio", // Who you are
"role": "admin", // What you can do
"exp": 1735689600 // When this token expires
}
The flow: (1) User logs in with username and password. (2) Server validates credentials. (3) Server
creates a JWT token and sends it back. (4) Client stores the token. (5) Every future request includes
the token in the Authorization header. (6) Server checks the token on every request. No token or
invalid token = rejected.
The Coffee Shop Lesson
A decade ago, you connected to a coffee shop WiFi without protection. You were likely the victim of a
man-in-the-middle attack. The attacker sat between you and the WiFi router, reading everything you
sent: passwords, emails, banking sessions. On an unprotected public network with no firewall, no
VPN, no HTTPS, you were completely transparent.
Today, you understand protocols (TCP, HTTP, TLS), ports, firewalls, and encryption. You found
MySQL ports 3306 and 33060 exposed on your own machine during a firewall audit. You are no
longer the person who walked into that coffee shop. You are becoming the person who builds the
locks.
"Knowledge IS the defense. An antivirus helps. A VPN helps. A firewall helps. But
understanding WHY those tools exist and HOW attacks work - that is the real
protection. Tools can be bypassed. Understanding cannot."
Julio Cesar Mendez Tobar
BuildEstimate - Page 21
VII - The Calculation Engine
Where construction knowledge becomes code
Estimate Calculations
This is where your industry expertise lives in code. Every estimator knows the formula: Material Cost
+ Labor Cost + Equipment = Direct Cost. Direct Cost + Overhead + Profit = Bid Price. Your
BuildEstimate calculation engine automates this entire chain.
// The core estimate calculation
decimal materialCost = quantity * unitPrice;
decimal laborCost = laborHours * laborRate;
decimal directCost = materialCost + laborCost + equipmentCost;
// Apply markup
decimal overhead = directCost * (overheadPercent / 100m);
decimal profit = directCost * (profitPercent / 100m);
decimal bidPrice = directCost + overhead + profit;
Notice the m after 100 in 100m. In C#, the letter m means "this is a decimal number, not an integer."
When you divide money, you MUST use decimal, not int. If you divide 10 / 3 as integers, C# returns 3
(it drops the remainder). If you divide 10m / 3m as decimals, C# returns 3.3333... which is correct for
money.
REAL WORLD: Construction estimating errors cost real money. If your markup calculation
uses integer division instead of decimal division, a $1,000,000 project could lose $33,333 in
a single rounding error. The letter m is the difference between a profitable project and
working for free.
Assembly Explosion
An assembly is a reusable template. Instead of estimating every component of a "standard interior
wall" separately (drywall, studs, tape, mud, paint, labor), you create an assembly that contains all
components. When you add the assembly to an estimate, it "explodes" into individual line items
automatically.
Julio Cesar Mendez Tobar
BuildEstimate - Page 22
Construction Analogy: Assembly explosion is like a prefab kit. Instead of ordering
lumber, nails, shingles, felt paper, and flashing separately for every roof, you order
"Standard Roof Kit" and everything arrives together. Your assembly template is the
kit specification. The explosion is the moment the kit gets unpacked into individual
items on the estimate. Build the kit once, use it on 100 projects.
The AI Validation Service
Your BuildEstimate system includes an AI validation endpoint powered by Claude. It takes your
completed estimate and checks it for common errors: missing items, unusual quantities, cost outliers,
rate discrepancies.
// From your ClaudeApiService.cs
var requestBody = new
{
model = "claude-sonnet-4-20250514",
max_tokens = 1024,
messages = new[]
{
new { role = "user", content = estimateData }
}
};
var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
request.Headers.Add("x-api-key", apiKey);
request.Headers.Add("anthropic-version", "2023-06-01");
This code sends your estimate data to Claude's API over HTTPS. The same API this very book was
created with. Claude analyzes the estimate and returns feedback: "Line item 47 shows $12/SF for
ceramic tile but the regional average is $8-$10/SF. Verify this price."
Julio Cesar Mendez Tobar
BuildEstimate - Page 23
VIII - Deployment: Going Live
From your PC to the world
The Deployment Pipeline
Your Code -> Git Push -> Docker Build -> Cloud Server -> HTTPS -> Users
Step 1: git push origin main Send code to GitHub
Step 2: docker build -t buildestimate . Package into container
Step 3: docker push Upload container to registry
Step 4: ssh into server Connect to cloud server
Step 5: docker-compose up -d Start API + Database
Step 6: certbot Install free SSL certificate
Step 7: https://buildestimate.com Your API is live
Right now, BuildEstimate runs on your PC at http://localhost:52817. That is like having a store with no
address, no road, and no sign. Only you can visit it. Deployment gives it an address, a road, and a
sign.
Step
What
1. Domain Buy buildestimate.com
2. Server
DigitalOcean droplet
3. Docker Package API + Database
4. Deploy Upload and run
5. HTTPS Let's Encrypt certificate
6. DNS
Cost
$12/year
Time
5 minutes
$6-24/month
$0 (free tool)
$0
$0 (free)
Point domain to server
$0
10 minutes
30 minutes
20 minutes
5 minutes
10 minutes
Total cost to go live: approximately $18/month. That is less than your Netflix subscription. For that
price, your BuildEstimate API is accessible from any browser, any phone, anywhere in the world,
protected by HTTPS encryption, running on a Linux server with 99.9% uptime.
You built this. Every controller, every entity, every calculation, every endpoint. This book does not
teach you how to build it. This book teaches you how to READ it. How to open any file, on any line,
and explain what it does and why it matters.
Julio Cesar Mendez Tobar
BuildEstimate - Page 24
The Linotte book says: "You know how to read a book, so you can write a program."
This book says: "You know how to read a blueprint, so you can read an API."
The saws and the dust built who you are.
The keyboard and the code build where you are going.
Keep changing those lines of code.
Keep breaking things.
Keep studying.
Julio Cesar Mendez Tobar
Guatemala, 2026
Julio Cesar Mendez Tobar
BuildEstimate - Page 25