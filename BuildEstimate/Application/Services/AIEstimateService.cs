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
// JERP CONNECTION:
//   This uses the EXACT same pattern as your ClaudeApiService.cs!
//   You already built this for JERP — now it's applied to construction.
//
// ============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BuildEstimate.Application.Services;

public class AIEstimateService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIEstimateService> _logger;

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

    public async Task<AIAnalysisResult> ValidateEstimate(EstimateForAI estimate)
    {
        var prompt = BuildValidationPrompt(estimate);
        var response = await CallClaude(prompt);
        return ParseAnalysis(response);
    }

    // =====================================================================
    // CHECK PRICING — Compare against market rates
    // =====================================================================

    public async Task<AIAnalysisResult> CheckPricing(EstimateForAI estimate)
    {
        var prompt = BuildPricingPrompt(estimate);
        var response = await CallClaude(prompt);
        return ParseAnalysis(response);
    }

    // =====================================================================
    // SUGGEST ASSEMBLIES — Recommend what's missing
    // =====================================================================

    public async Task<AIAnalysisResult> SuggestAssemblies(EstimateForAI estimate)
    {
        var prompt = BuildSuggestionPrompt(estimate);
        var response = await CallClaude(prompt);
        return ParseAnalysis(response);
    }

    // =====================================================================
    // RISK ANALYSIS — Identify concentration risks
    // =====================================================================

    public async Task<AIAnalysisResult> AnalyzeRisk(EstimateForAI estimate)
    {
        var prompt = BuildRiskPrompt(estimate);
        var response = await CallClaude(prompt);
        return ParseAnalysis(response);
    }

    // =====================================================================
    // PROMPT BUILDERS
    // =====================================================================

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

    private static string FormatLineItems(List<LineItemForAI> items)
    {
        return string.Join("\n", items.Select(li =>
            $"  {li.CSICode} | {li.Description} | {li.Quantity:N1} {li.UOM} | " +
            $"Mat: ${li.MaterialTotal:N2} | Labor: ${li.LaborTotal:N2} | " +
            $"Total: ${li.LineTotal:N2}"));
    }

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

public class EstimateForAI
{
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public decimal SquareFootage { get; set; }
    public string County { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public bool IsPrevailingWage { get; set; }
    public decimal MaterialTotal { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal EquipmentTotal { get; set; }
    public decimal SubcontractorTotal { get; set; }
    public decimal DirectCost { get; set; }
    public decimal TotalBidPrice { get; set; }
    public decimal CostPerSF { get; set; }
    public decimal OverheadPercent { get; set; }
    public decimal ProfitPercent { get; set; }
    public decimal ContingencyPercent { get; set; }
    public List<LineItemForAI> LineItems { get; set; } = new();
}

public class LineItemForAI
{
    public string CSICode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UOM { get; set; } = string.Empty;
    public decimal MaterialTotal { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal LineTotal { get; set; }
}

public class AIAnalysisResult
{
    public int OverallScore { get; set; }
    public List<AIFinding> Findings { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class AIFinding
{
    public string Severity { get; set; } = "info";
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string? EstimatedImpact { get; set; }
}
