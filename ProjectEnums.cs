/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// PHASE 6: AI ESTIMATE ENGINE — Claude-Powered Intelligence
// ============================================================================
//
// WHAT IT DOES:
//   1. VALIDATES estimates — catches missing items, wrong quantities
//   2. SUGGESTS assemblies — "This looks like a medical office, you need..."
//   3. CHECKS pricing — "Your drywall at $0.35/SF is below market ($0.52)"
//   4. ANALYZES risk — "40% of your cost is in one subcontractor"
//
// HOW IT WORKS:
//   Takes your estimate data → converts to a prompt → sends to Claude
//   Claude responds with analysis → system parses and returns structured data
//
// ARCHITECTURE — "Service Layer":
//   This class doesn't handle HTTP requests — that's the controller's job.
//   The service contains the BUSINESS LOGIC:
//     - How to format an estimate as a prompt
//     - How to call the Claude API
//     - How to parse Claude's response back into structured data
//   This separation makes the AI logic testable and reusable.
//
// ============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BuildEstimate.Application.Services;

/// <summary>
/// Service that uses the Claude AI API (Anthropic) to analyze construction estimates.
/// Provides four specialized analysis types: validation, pricing, assembly suggestions, and risk analysis.
///
/// Follows the "Service Layer" pattern — business logic lives here, not in the controller.
/// The controller just calls this service and returns the result to the HTTP client.
/// </summary>
public class AIEstimateService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIEstimateService> _logger;

    /// <summary>
    /// Constructs the AI service with dependencies injected by the DI container.
    /// </summary>
    /// <param name="httpClient">HTTP client for making requests to the Claude API.</param>
    /// <param name="configuration">App configuration for reading the Anthropic API key.</param>
    /// <param name="logger">Structured logger for tracking AI calls and errors.</param>
    public AIEstimateService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AIEstimateService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    // =====================================================================
    // VALIDATE ESTIMATE — Catch missing items, wrong quantities
    // =====================================================================

    /// <summary>
    /// Validates an estimate for completeness and quantity accuracy.
    /// Sends the estimate data to Claude with a prompt asking it to identify:
    /// missing CSI divisions, unrealistic quantities, and non-compliant rates.
    /// </summary>
    /// <param name="estimate">The estimate data in AI-friendly flat format.</param>
    /// <returns>Structured analysis with severity-tagged findings and an overall score.</returns>
    public async Task<AIAnalysisResult> ValidateEstimate(EstimateForAI estimate)
    {
        var prompt = BuildValidationPrompt(estimate);
        var response = await CallClaude(prompt);
        return ParseAnalysis(response);
    }

    // =====================================================================
    // CHECK PRICING — Compare against market rates
    // =====================================================================

    /// <summary>
    /// Checks each line item's pricing against current market rates for the estimate's location.
    /// Returns findings for items that are below, at, or above market expectations.
    /// </summary>
    /// <param name="estimate">The estimate to price-check.</param>
    /// <returns>Structured analysis with per-item pricing assessments.</returns>
    public async Task<AIAnalysisResult> CheckPricing(EstimateForAI estimate)
    {
        var prompt = BuildPricingPrompt(estimate);
        var response = await CallClaude(prompt);
        return ParseAnalysis(response);
    }

    // =====================================================================
    // SUGGEST ASSEMBLIES — Recommend what's missing
    // =====================================================================

    /// <summary>
    /// Suggests additional assemblies or work items that are likely missing from the estimate.
    /// Claude compares the current CSI sections against what a typical project of this type needs.
    /// </summary>
    /// <param name="estimate">The estimate to analyze for completeness.</param>
    /// <returns>Structured analysis with specific assembly recommendations.</returns>
    public async Task<AIAnalysisResult> SuggestAssemblies(EstimateForAI estimate)
    {
        var prompt = BuildSuggestionPrompt(estimate);
        var response = await CallClaude(prompt);
        return ParseAnalysis(response);
    }

    // =====================================================================
    // RISK ANALYSIS — Identify concentration risks
    // =====================================================================

    /// <summary>
    /// Analyzes the estimate for financial and execution risks.
    /// Looks for: cost concentration, thin margins, inadequate contingency, and risky scope items.
    /// </summary>
    /// <param name="estimate">The estimate to risk-analyze.</param>
    /// <returns>Structured analysis with risk findings and mitigation recommendations.</returns>
    public async Task<AIAnalysisResult> AnalyzeRisk(EstimateForAI estimate)
    {
        var prompt = BuildRiskPrompt(estimate);
        var response = await CallClaude(prompt);
        return ParseAnalysis(response);
    }

    // =====================================================================
    // PROMPT BUILDERS
    // =====================================================================

    /// <summary>
    /// Builds a Claude prompt asking for estimate validation.
    /// Includes project details and all line items as formatted text.
    /// The prompt specifies the exact JSON response format Claude should use.
    /// </summary>
    private string BuildValidationPrompt(EstimateForAI estimate)
    {
        return $@"You are an expert construction estimator reviewing a bid estimate.

PROJECT: {estimate.ProjectName}
TYPE: {estimate.ProjectType}
SIZE: {estimate.SquareFootage:N0} SF
LOCATION: {estimate.County}, {estimate.State}
PREVAILING WAGE: {(estimate.IsPrevailingWage ? "Yes" : "No")}

CURRENT ESTIMATE ({estimate.LineItems.Count} line items):
{FormatLineItems(estimate.LineItems)}

TOTALS:
  Material: ${estimate.MaterialTotal:N2}
  Labor: ${estimate.LaborTotal:N2}
  Equipment: ${estimate.EquipmentTotal:N2}
  Subcontractor: ${estimate.SubcontractorTotal:N2}
  Direct Cost: ${estimate.DirectCost:N2}
  Bid Price: ${estimate.TotalBidPrice:N2}
  Cost/SF: ${estimate.CostPerSF:N2}/SF

Please analyze this estimate and respond with JSON:
{{
  ""overallScore"": 1-100,
  ""findings"": [
    {{
      ""severity"": ""critical"" | ""warning"" | ""info"",
      ""category"": ""missing_item"" | ""wrong_quantity"" | ""pricing"" | ""compliance"",
      ""message"": ""Description of the finding"",
      ""recommendation"": ""What to do about it"",
      ""estimatedImpact"": ""$X,XXX""
    }}
  ],
  ""summary"": ""One paragraph overall assessment""
}}

Focus on:
1. Missing CSI divisions that a {estimate.ProjectType} typically needs
2. Quantities that seem too high or too low for {estimate.SquareFootage:N0} SF
3. Labor rates that don't match {estimate.County} County {(estimate.IsPrevailingWage ? "prevailing wage" : "market")} rates
4. Cost/SF compared to typical {estimate.ProjectType} projects";
    }

    /// <summary>
    /// Builds a Claude prompt asking for market rate price comparison.
    /// Formats the line items as a table and asks Claude to assess each one.
    /// </summary>
    private string BuildPricingPrompt(EstimateForAI estimate)
    {
        return $@"You are a construction cost consultant. Compare this estimate's pricing against current market rates for {estimate.County}, {estimate.State}.

PROJECT: {estimate.ProjectName} ({estimate.ProjectType})
SIZE: {estimate.SquareFootage:N0} SF

LINE ITEMS:
{FormatLineItems(estimate.LineItems)}

For each line item, assess if the unit cost is:
- BELOW MARKET (potential quality risk or error)
- AT MARKET (reasonable)
- ABOVE MARKET (opportunity to negotiate or rebid)

Respond with JSON:
{{
  ""findings"": [
    {{
      ""severity"": ""critical"" | ""warning"" | ""info"",
      ""category"": ""pricing"",
      ""message"": ""Item description — current $X.XX vs market $X.XX"",
      ""recommendation"": ""Specific action"",
      ""estimatedImpact"": ""$X,XXX""
    }}
  ],
  ""summary"": ""Overall pricing assessment""
}}";
    }

    /// <summary>
    /// Builds a Claude prompt asking what CSI sections are likely missing.
    /// Provides the existing sections as a list and asks Claude to identify gaps.
    /// </summary>
    private string BuildSuggestionPrompt(EstimateForAI estimate)
    {
        return $@"You are a senior construction estimator. Based on this project type and existing line items, what CSI sections are likely MISSING?

PROJECT: {estimate.ProjectName}
TYPE: {estimate.ProjectType}
SIZE: {estimate.SquareFootage:N0} SF

EXISTING CSI SECTIONS:
{string.Join("\n", estimate.LineItems.Select(li => $"  {li.CSICode} - {li.Description}"))}

What assemblies or line items should be added? Consider:
- Typical {estimate.ProjectType} requirements
- Code compliance (fire protection, accessibility, MEP)
- Commonly forgotten items (cleanup, permits, temporary facilities)

Respond with JSON:
{{
  ""findings"": [
    {{
      ""severity"": ""critical"" | ""warning"" | ""info"",
      ""category"": ""missing_item"",
      ""message"": ""CSI XX XX XX - What's missing and why"",
      ""recommendation"": ""Add assembly or line item for..."",
      ""estimatedImpact"": ""$X,XXX""
    }}
  ],
  ""summary"": ""Coverage assessment""
}}";
    }

    /// <summary>
    /// Builds a Claude prompt asking for a risk profile assessment.
    /// Provides the cost breakdown percentages and markup structure for Claude to evaluate.
    /// </summary>
    private string BuildRiskPrompt(EstimateForAI estimate)
    {
        return $@"You are a construction risk analyst. Analyze this estimate for financial and execution risks.

PROJECT: {estimate.ProjectName} ({estimate.ProjectType})
BID PRICE: ${estimate.TotalBidPrice:N2}
COST/SF: ${estimate.CostPerSF:N2}

COST BREAKDOWN:
  Material: ${estimate.MaterialTotal:N2} ({(estimate.DirectCost > 0 ? estimate.MaterialTotal / estimate.DirectCost * 100 : 0):N1}%)
  Labor: ${estimate.LaborTotal:N2} ({(estimate.DirectCost > 0 ? estimate.LaborTotal / estimate.DirectCost * 100 : 0):N1}%)
  Equipment: ${estimate.EquipmentTotal:N2}
  Subcontractor: ${estimate.SubcontractorTotal:N2}

MARKUP:
  Overhead: {estimate.OverheadPercent}%
  Profit: {estimate.ProfitPercent}%
  Contingency: {estimate.ContingencyPercent}%

Respond with JSON:
{{
  ""overallScore"": 1-100,
  ""findings"": [
    {{
      ""severity"": ""critical"" | ""warning"" | ""info"",
      ""category"": ""risk"",
      ""message"": ""Risk description"",
      ""recommendation"": ""Mitigation strategy""
    }}
  ],
  ""summary"": ""Risk profile assessment""
}}";
    }

    // =====================================================================
    // CLAUDE API CALL — Same pattern as your JERP ClaudeApiService
    // =====================================================================

    /// <summary>
    /// Sends a prompt to the Claude API and returns the raw text response.
    /// Handles authentication headers, error responses, and JSON parsing.
    /// Returns a fallback JSON string if the API call fails, so callers always get a valid response.
    /// </summary>
    /// <param name="prompt">The complete prompt text to send to Claude.</param>
    /// <returns>The text content of Claude's response, or a fallback error JSON string.</returns>
    private async Task<string> CallClaude(string prompt)
    {
        var apiKey = _configuration["Anthropic:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Anthropic API key not configured");
            return "{ \"findings\": [], \"summary\": \"AI service not configured. Add Anthropic:ApiKey to appsettings.json\" }";
        }

        try
        {
            var request = new
            {
                model = "claude-sonnet-4-20250514",
                max_tokens = 2000,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var response = await _httpClient.PostAsync(
                "https://api.anthropic.com/v1/messages", content);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Claude API error: {Status} {Body}",
                    response.StatusCode, responseBody);
                return "{ \"findings\": [], \"summary\": \"AI analysis temporarily unavailable\" }";
            }

            // Extract text from Claude's response
            using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
            var text = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "{}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Claude API");
            return "{ \"findings\": [], \"summary\": \"Error connecting to AI service\" }";
        }
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    /// <summary>
    /// Formats a list of line items as a compact text table for inclusion in Claude prompts.
    /// Each row shows: CSI code | description | quantity | material total | labor total | line total.
    /// </summary>
    private static string FormatLineItems(List<LineItemForAI> items)
    {
        return string.Join("\n", items.Select(li =>
            $"  {li.CSICode} | {li.Description} | {li.Quantity:N1} {li.UOM} | " +
            $"Mat: ${li.MaterialTotal:N2} | Labor: ${li.LaborTotal:N2} | " +
            $"Total: ${li.LineTotal:N2}"));
    }

    /// <summary>
    /// Parses Claude's JSON response into a structured <see cref="AIAnalysisResult"/>.
    /// Handles the case where Claude wraps JSON in markdown code fences (```json ... ```).
    /// If JSON parsing fails, returns the raw text in the Summary field so nothing is lost.
    /// </summary>
    /// <param name="json">The raw text response from Claude.</param>
    private static AIAnalysisResult ParseAnalysis(string json)
    {
        try
        {
            // Clean up potential markdown code fences
            json = json.Trim();
            if (json.StartsWith("```"))
            {
                var firstNewline = json.IndexOf('\n');
                var lastFence = json.LastIndexOf("```");
                if (firstNewline > 0 && lastFence > firstNewline)
                    json = json.Substring(firstNewline + 1, lastFence - firstNewline - 1);
            }

            return System.Text.Json.JsonSerializer.Deserialize<AIAnalysisResult>(json,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new AIAnalysisResult();
        }
        catch
        {
            return new AIAnalysisResult
            {
                Summary = json // Return raw text if JSON parsing fails
            };
        }
    }
}

// =====================================================================
// DTOs for AI Service
// =====================================================================

/// <summary>
/// A simplified, flat representation of an estimate for inclusion in AI prompts.
/// Contains only the data Claude needs to analyze — no database IDs or navigation properties.
/// This prevents the prompt from becoming unnecessarily long.
/// </summary>
public class EstimateForAI
{
    /// <summary>Name of the project being estimated.</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>Type of project, e.g., "Commercial", "Medical", "Industrial". Used by Claude to set expectations.</summary>
    public string ProjectType { get; set; } = string.Empty;

    /// <summary>Gross square footage. Used for cost-per-SF comparisons and scale validation.</summary>
    public decimal SquareFootage { get; set; }

    /// <summary>County where the project is located. Affects expected labor rates.</summary>
    public string County { get; set; } = string.Empty;

    /// <summary>State abbreviation. Used with county for regional cost benchmarking.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Whether the project requires prevailing wages. Affects expected labor cost levels.</summary>
    public bool IsPrevailingWage { get; set; }

    /// <summary>Total material cost from all line items.</summary>
    public decimal MaterialTotal { get; set; }

    /// <summary>Total labor cost from all line items.</summary>
    public decimal LaborTotal { get; set; }

    /// <summary>Total equipment cost from all line items.</summary>
    public decimal EquipmentTotal { get; set; }

    /// <summary>Total subcontractor cost from all line items.</summary>
    public decimal SubcontractorTotal { get; set; }

    /// <summary>Sum of all four cost types before markups.</summary>
    public decimal DirectCost { get; set; }

    /// <summary>Final bid price after all markups are applied.</summary>
    public decimal TotalBidPrice { get; set; }

    /// <summary>Cost per square foot: TotalBidPrice / SquareFootage.</summary>
    public decimal CostPerSF { get; set; }

    /// <summary>Overhead markup percentage, e.g., 10.0 for 10%.</summary>
    public decimal OverheadPercent { get; set; }

    /// <summary>Profit markup percentage.</summary>
    public decimal ProfitPercent { get; set; }

    /// <summary>Contingency percentage — buffer for unknown costs.</summary>
    public decimal ContingencyPercent { get; set; }

    /// <summary>All line items in a flat, AI-readable format.</summary>
    public List<LineItemForAI> LineItems { get; set; } = new();
}

/// <summary>
/// A single estimate line item in AI-friendly flat format.
/// Only includes the fields useful for AI analysis — no database IDs.
/// </summary>
public class LineItemForAI
{
    /// <summary>The CSI code for this work item, e.g., "09 29 00".</summary>
    public string CSICode { get; set; } = string.Empty;

    /// <summary>Description of the specific work being performed.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The quantity of work (e.g., 9400 SF of drywall).</summary>
    public decimal Quantity { get; set; }

    /// <summary>Unit of measure abbreviation (e.g., "SF", "LF", "CY").</summary>
    public string UOM { get; set; } = string.Empty;

    /// <summary>Total material cost for this line item.</summary>
    public decimal MaterialTotal { get; set; }

    /// <summary>Total labor cost for this line item.</summary>
    public decimal LaborTotal { get; set; }

    /// <summary>Combined line total: Material + Labor + Equipment + Subcontractor.</summary>
    public decimal LineTotal { get; set; }
}

/// <summary>
/// The complete result of an AI analysis operation.
/// Contains zero or more findings (each with a severity level) plus a summary paragraph.
/// </summary>
public class AIAnalysisResult
{
    /// <summary>
    /// Overall quality score from 1 to 100.
    /// 90+ = excellent estimate, 70-89 = good with minor issues,
    /// 50-69 = needs attention, below 50 = significant problems.
    /// </summary>
    public int OverallScore { get; set; }

    /// <summary>
    /// Individual findings from the analysis.
    /// Each finding has a severity (critical/warning/info), a category,
    /// a message explaining the issue, and a recommendation for what to do.
    /// </summary>
    public List<AIFinding> Findings { get; set; } = new();

    /// <summary>A one-paragraph plain-English summary of the overall analysis.</summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// A single finding from an AI analysis — one specific issue or recommendation.
/// Findings are severity-tagged so the UI can prioritize what the estimator sees first.
/// </summary>
public class AIFinding
{
    /// <summary>
    /// How serious this finding is:
    /// "critical" = must fix before submitting bid (e.g., missing fire suppression),
    /// "warning" = should review (e.g., labor rate seems low),
    /// "info" = FYI (e.g., consider adding a contingency note).
    /// </summary>
    public string Severity { get; set; } = "info";

    /// <summary>
    /// The type of issue: "missing_item", "wrong_quantity", "pricing", "compliance", or "risk".
    /// Used by the UI to show appropriate icons and filter views.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Plain-English description of the finding, e.g., "Drywall at $0.35/SF is below market ($0.52/SF)".</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Specific action the estimator should take to address this finding.</summary>
    public string Recommendation { get; set; } = string.Empty;

    /// <summary>
    /// Approximate dollar impact of this finding, e.g., "$12,500" or "$3,000–$8,000".
    /// Null if the impact cannot be estimated. Helps the estimator prioritize.
    /// </summary>
    public string? EstimatedImpact { get; set; }
}
