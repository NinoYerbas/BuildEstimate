/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// PROGRAM.CS — THE IGNITION KEY
// ============================================================================
//
// This file runs FIRST when your application starts.
// It configures EVERYTHING:
//   1. Database connection (where is SQL Server?)
//   2. Authentication (who can access what?)
//   3. Logging (what gets recorded?)
//   4. Swagger (API documentation)
//   5. Dependency Injection (what classes does each controller need?)
//   6. Middleware pipeline (what happens to every request?)
//
// JERP EQUIVALENT: Your JERP Program.cs does the exact same things.
//
// HOW .NET 8 MINIMAL API WORKS:
//   Old way (.NET 5):  Startup.cs with ConfigureServices() and Configure()
//   New way (.NET 8):  Everything in one file, top-to-bottom
//
//   The code runs in order:
//   1. Create the builder          → var builder = WebApplication.CreateBuilder()
//   2. Register services           → builder.Services.Add...()
//   3. Build the app               → var app = builder.Build()
//   4. Configure the pipeline      → app.Use...(), app.Map...()
//   5. Run                         → app.Run()
//
// ============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using BuildEstimate.Infrastructure.Data;

// =====================================================================
// STEP 1: CREATE THE BUILDER
// =====================================================================
// The builder is the setup phase. Nothing is running yet.
// You're just telling .NET what you want.

var builder = WebApplication.CreateBuilder(args);

// =====================================================================
// STEP 2: CONFIGURE LOGGING (Serilog)
// =====================================================================
// Serilog writes structured logs — much better than Console.WriteLine.
// Logs go to: Console (for development) and File (for production).
//
// JERP uses the exact same Serilog setup.

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/buildestimate-.log", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

// =====================================================================
// STEP 3: REGISTER THE DATABASE (Entity Framework)
// =====================================================================
//
// This is where you tell EF Core:
//   "Here's my DbContext class, and here's the database connection string."
//
// builder.Configuration.GetConnectionString("DefaultConnection") reads from
// appsettings.json:
//   {
//     "ConnectionStrings": {
//       "DefaultConnection": "Server=localhost;Database=BuildEstimate;..."
//     }
//   }
//
// DATABASE CONCEPT — "Connection Pooling":
//   AddDbContext doesn't create a connection for every request.
//   It uses a POOL — a set of reusable connections.
//   Request comes in → grab a connection from the pool → use it → return it.
//   This is MUCH faster than creating new connections every time.
//
// JERP EQUIVALENT: Your JERP Program.cs has:
//   builder.Services.AddDbContext<JerpDbContext>(options => ...);

builder.Services.AddDbContext<BuildEstimateDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.MigrationsAssembly("BuildEstimate.Infrastructure");
            // Tells EF: "Put migration files in the Infrastructure project"
            
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
            // If the database is temporarily unavailable (network hiccup),
            // retry up to 3 times instead of immediately failing.
        }
    ));

// =====================================================================
// STEP 4: CONFIGURE AUTHENTICATION (JWT)
// =====================================================================
//
// JWT (JSON Web Token) = a secure token that proves who you are.
// 
// How it works:
//   1. User logs in with username/password → server returns a JWT
//   2. Every subsequent request includes: Authorization: Bearer eyJhbG...
//   3. Server validates the token → knows who's making the request
//
// This is IDENTICAL to your JERP authentication setup.

var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatShouldBeInAppSettings123!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BuildEstimate";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// =====================================================================
// STEP 5: ADD CONTROLLERS + SWAGGER
// =====================================================================

builder.Services.AddControllers();
// Scans all projects for classes that inherit from ControllerBase
// and registers them as HTTP endpoint handlers.

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BuildEstimate API",
        Version = "v1",
        Description = "Construction cost estimating system with CSI MasterFormat integration",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Julio Cesar Mendez Tobar",
            Email = "ichbincesartobar@yahoo.com"
        }
    });
    
    // Include XML comments in Swagger (the /// comments above each method)
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// =====================================================================
// STEP 6: ADD CORS (Cross-Origin Resource Sharing)
// =====================================================================
//
// CORS allows your frontend (running on localhost:3000) to call your
// API (running on localhost:5000). Without CORS, the browser blocks it.
//
// In production, you'd restrict this to your actual frontend domain.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// =====================================================================
// STEP 6B: REGISTER AI SERVICE
// =====================================================================
// The AI service calls Claude's API for estimate validation.
// HttpClient is injected by .NET's HttpClientFactory (manages connections).
// Same pattern as your JERP ClaudeApiService registration.

builder.Services.AddHttpClient<BuildEstimate.Application.Services.AIEstimateService>();

// =====================================================================
// STEP 7: BUILD THE APP
// =====================================================================
// Everything above was CONFIGURATION (telling .NET what you want).
// Build() compiles all that configuration into a runnable application.

var app = builder.Build();

// =====================================================================
// STEP 8: CONFIGURE THE MIDDLEWARE PIPELINE
// =====================================================================
//
// Middleware is code that runs on EVERY request, in order.
// Think of it like an assembly line:
//   Request → [Swagger] → [CORS] → [Auth] → [Controller] → Response
//
// The ORDER MATTERS. Authentication must come before Authorization.
// Swagger should only be available in Development.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BuildEstimate API v1");
        c.RoutePrefix = string.Empty;  // Swagger at root URL (localhost:5000/)
    });
}

app.UseCors("AllowAll");
app.UseAuthentication();  // "Who are you?" (validates JWT)
app.UseAuthorization();   // "Are you allowed?" (checks [Authorize] attributes)
app.MapControllers();     // "Route this request to the correct controller method"

// =====================================================================
// STEP 9: AUTO-MIGRATE DATABASE ON STARTUP
// =====================================================================
//
// This creates the database and applies all migrations automatically
// when the app starts. Great for development — in production you'd
// run migrations manually.
//
// DATABASE CONCEPT — "Migration":
//   A migration is a set of SQL commands that evolve your database schema.
//   First migration: CREATE TABLE Projects, CREATE TABLE CSIDivisions, ...
//   Later migration: ALTER TABLE Estimates ADD COLUMN BondPercent DECIMAL(5,2)
//   
//   EF tracks which migrations have been applied in a special table
//   called __EFMigrationsHistory.

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BuildEstimateDbContext>();
    
    try
    {
        db.Database.Migrate();
        // Applies any pending migrations. If database doesn't exist, creates it.
        // Seed data (CSI divisions) is included in the initial migration.
        
        Log.Information("Database migrated successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database migration failed. Is SQL Server running?");
        // Don't crash — let the health check endpoint report the issue
    }
}

// =====================================================================
// STEP 10: RUN THE APPLICATION
// =====================================================================
Log.Information("BuildEstimate API starting on {Environment}", app.Environment.EnvironmentName);
app.Run();
// This blocks forever, listening for HTTP requests until you press Ctrl+C.
