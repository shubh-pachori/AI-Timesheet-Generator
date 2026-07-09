using System.Text;
using System.Text.Json;
using AITimesheet.API.Entities;
using AITimesheet.API.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AITimesheet.API.AI;

/// <summary>
/// Calls Azure OpenAI (or plain OpenAI) chat completions to turn raw activity data
/// into a professional daily timesheet + weekly summary, and to answer chat queries
/// like "What did I work on last Thursday?".
/// </summary>
public class OpenAiTimesheetService : IAiTimesheetService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<OpenAiTimesheetService> _logger;

    public OpenAiTimesheetService(HttpClient http, IConfiguration config, ILogger<OpenAiTimesheetService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<AiTimesheetResult> GenerateTimesheetAsync(
        List<Activity> weekActivities, DateOnly weekStart, DateOnly weekEnd, CancellationToken ct = default)
    {
        var byDay = weekActivities.GroupBy(a => a.ActivityDate).OrderBy(g => g.Key);

        var prompt = BuildPrompt(weekActivities, weekStart, weekEnd);

        var aiJson = await CallModelAsync(prompt, ct);

        if (aiJson is null)
        {
            // Deterministic fallback so the demo never breaks without an API key
            return BuildFallbackResult(byDay);
        }

        try
        {
            using var doc = JsonDocument.Parse(aiJson);
            var entries = new List<AiGeneratedEntry>();
            foreach (var e in doc.RootElement.GetProperty("entries").EnumerateArray())
            {
                entries.Add(new AiGeneratedEntry(
                    DateOnly.Parse(e.GetProperty("date").GetString()!),
                    e.GetProperty("description").GetString() ?? "",
                    e.GetProperty("hours").GetDouble(),
                    e.TryGetProperty("devHours", out var dh) ? dh.GetDouble() : 0,
                    e.TryGetProperty("meetingHours", out var mh) ? mh.GetDouble() : 0,
                    e.TryGetProperty("reviewHours", out var rh) ? rh.GetDouble() : 0));
            }
            var summary = doc.RootElement.GetProperty("weeklySummary").GetString() ?? "";
            var missing = doc.RootElement.TryGetProperty("missingHourPrompts", out var mp)
                ? mp.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                : new List<string>();

            return new AiTimesheetResult(entries, summary, missing);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response, using fallback generator.");
            return BuildFallbackResult(byDay);
        }
    }

    public async Task<string> AnswerChatQueryAsync(
        Guid userId, string question, List<Activity> relevantActivities, CancellationToken ct = default)
    {
        var context = string.Join("\n", relevantActivities.Select(a =>
            $"- [{a.ActivityDate:yyyy-MM-dd}] ({a.Source}) {a.Title} {(a.Status is null ? "" : $"[{a.Status}]")}"));

        var prompt = $@"You are an assistant that answers questions about an employee's work activity log.
Activity log:
{context}

Question: {question}

Answer concisely and specifically, referencing ticket IDs, commit counts, or meeting names where relevant.";

        var response = await CallModelAsync(prompt, ct, expectJson: false);
        return response ?? FallbackChatAnswer(question, relevantActivities);
    }

    private string BuildPrompt(List<Activity> activities, DateOnly weekStart, DateOnly weekEnd)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Based on these commits, Jira tickets, pull requests and meetings, generate a professional employee timesheet.");
        sb.AppendLine($"Week: {weekStart:yyyy-MM-dd} to {weekEnd:yyyy-MM-dd}");
        sb.AppendLine("Raw activity:");
        foreach (var a in activities.OrderBy(a => a.ActivityDate))
        {
            sb.AppendLine($"- [{a.ActivityDate:yyyy-MM-dd}] ({a.Source}) {a.Title} {(a.EstimatedHours.HasValue ? $"~{a.EstimatedHours}h" : "")}");
        }
        sb.AppendLine();
        sb.AppendLine(@"Respond ONLY with strict JSON, no markdown, in this shape:
{
  ""entries"": [
    { ""date"": ""YYYY-MM-DD"", ""description"": ""..."", ""hours"": 8, ""devHours"": 5, ""meetingHours"": 2, ""reviewHours"": 1 }
  ],
  ""weeklySummary"": ""..."",
  ""missingHourPrompts"": [""...""]
}
Combine related raw items into fluent professional sentences (e.g. 'Implemented authentication middleware and resolved JWT token validation issues. Participated in Sprint Planning.'). If a day has less than 6 hours of detected activity, add a missingHourPrompts entry asking about documentation/research/learning for that day.");
        return sb.ToString();
    }

    private async Task<string?> CallModelAsync(string prompt, CancellationToken ct, bool expectJson = true)
    {
        var endpoint = _config["AzureOpenAI:Endpoint"];
        var apiKey = _config["AzureOpenAI:ApiKey"];
        var deployment = _config["AzureOpenAI:Deployment"] ?? "gpt-4o";

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogInformation("AzureOpenAI not configured — using fallback (non-AI) generation for demo purposes.");
            return null;
        }

        try
        {
            var url = $"{endpoint.TrimEnd('/')}/openai/deployments/{deployment}/chat/completions?api-version=2024-06-01";
            var body = new
            {
                messages = new[]
                {
                    new { role = "system", content = "You generate accurate, professional employee timesheets from raw activity data." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.3,
                response_format = expectJson ? new { type = "json_object" } : null
            };

            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("api-key", apiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Azure OpenAI call failed with status {Status}", resp.StatusCode);
                return null;
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure OpenAI call threw an exception, falling back.");
            return null;
        }
    }

    // ---- Deterministic fallbacks (used when no AI key is configured, so the hackathon demo always works) ----

    private static AiTimesheetResult BuildFallbackResult(IOrderedEnumerable<IGrouping<DateOnly, Activity>> byDay)
    {
        var entries = new List<AiGeneratedEntry>();
        var missing = new List<string>();
        var titles = new List<string>();

        foreach (var day in byDay)
        {
            var dev = day.Where(a => a.Source is ActivitySource.GitCommit or ActivitySource.JiraTicket or ActivitySource.PullRequest).ToList();
            var meetings = day.Where(a => a.Source == ActivitySource.Meeting).ToList();
            var reviews = day.Where(a => a.Source == ActivitySource.CodeReview).ToList();

            var devHours = Math.Round(dev.Sum(a => a.EstimatedHours ?? 1), 2);
            var meetingHours = Math.Round(meetings.Sum(a => a.EstimatedHours ?? 0.5), 2);
            var reviewHours = Math.Round(reviews.Sum(a => a.EstimatedHours ?? 0.5), 2);
            var totalHours = Math.Round(devHours + meetingHours + reviewHours, 2);

            var descParts = new List<string>();
            if (dev.Count > 0) descParts.Add($"Worked on {string.Join(", ", dev.Select(d => d.Title).Distinct().Take(3))}.");
            if (meetings.Count > 0) descParts.Add($"Attended {string.Join(", ", meetings.Select(m => m.Title))}.");
            if (reviews.Count > 0) descParts.Add("Reviewed pull requests.");

            titles.AddRange(dev.Select(d => d.Title));

            entries.Add(new AiGeneratedEntry(
                day.Key,
                descParts.Count > 0 ? string.Join(" ", descParts) : "No significant activity detected.",
                totalHours,
                devHours, meetingHours, reviewHours));

            if (totalHours < 6)
            {
                missing.Add($"We found only {totalHours} hours of activity on {day.Key:dddd}. Did you work on documentation, learning or research?");
            }
        }

        var summary = titles.Count > 0
            ? $"This week you worked on {titles.Distinct().Count()} key items including {string.Join(", ", titles.Distinct().Take(4))}, attended team meetings, and contributed to ongoing sprint goals."
            : "No significant activity was detected for this week.";

        return new AiTimesheetResult(entries, summary, missing);
    }

    private static string FallbackChatAnswer(string question, List<Activity> activities)
    {
        if (activities.Count == 0) return "I couldn't find any recorded activity matching that question.";
        var summary = string.Join(", ", activities.Select(a => a.Title).Distinct().Take(5));
        return $"Based on your activity log, you worked on: {summary}.";
    }
}
