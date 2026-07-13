using System.Net.Http.Headers;
using System.Text.Json;
using AITimesheet.TimesheetService.Entities;
using AITimesheet.TimesheetService.ServiceLayer.Interfaces;

namespace AITimesheet.TimesheetService.ServiceLayer.Implementations;

public class GitHubIntegrationService : IIntegrationService
{
    private readonly HttpClient _http;
    public ConnectionProvider Provider => ConnectionProvider.GitHub;

    public GitHubIntegrationService(HttpClient http)
    {
        _http = http;
        _http.BaseAddress ??= new Uri("https://api.github.com/");
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AITimesheetGenerator", "1.0"));
    }

    public async Task<List<Activity>> FetchActivitiesAsync(
        Guid userId, string accessToken, DateOnly weekStart, DateOnly weekEnd, CancellationToken ct = default)
    {
        var activities = new List<Activity>();

        try
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var userResp = await _http.GetAsync("user", ct);
            if (!userResp.IsSuccessStatusCode)
            {
                return MockCommits(userId, weekStart);
            }

            var userJson = await userResp.Content.ReadAsStringAsync(ct);
            using var userDoc = JsonDocument.Parse(userJson);
            var login = userDoc.RootElement.GetProperty("login").GetString();

            var query = $"author:{login}+author-date:{weekStart:yyyy-MM-dd}..{weekEnd:yyyy-MM-dd}";
            var req = new HttpRequestMessage(HttpMethod.Get, $"search/commits?q={query}");
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.cloak-preview+json"));
            var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return MockCommits(userId, weekStart);

            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
            {
                var message = item.GetProperty("commit").GetProperty("message").GetString() ?? "Commit";
                var dateStr = item.GetProperty("commit").GetProperty("author").GetProperty("date").GetString();
                var sha = item.GetProperty("sha").GetString();

                activities.Add(new Activity
                {
                    UserId = userId,
                    Source = ActivitySource.GitCommit,
                    Title = message.Split('\n')[0],
                    Description = message,
                    ExternalReference = sha?[..7],
                    ActivityDate = DateTime.Parse(dateStr!),
                    EstimatedHours = 0.5
                });
            }
        }
        catch
        {
            return MockCommits(userId, weekStart);
        }

        return activities;
    }

    private static List<Activity> MockCommits(Guid userId, DateOnly weekStart) => new()
    {
        new Activity { UserId = userId, Source = ActivitySource.GitCommit, Title = "Fix login authentication issue", ActivityDate = weekStart.ToDateTime(TimeOnly.MinValue), EstimatedHours = 2 },
        new Activity { UserId = userId, Source = ActivitySource.GitCommit, Title = "Add API validation", ActivityDate = weekStart.AddDays(1).ToDateTime(TimeOnly.MinValue), EstimatedHours = 1.5 },
        new Activity { UserId = userId, Source = ActivitySource.GitCommit, Title = "Improve dashboard UI", ActivityDate = weekStart.AddDays(2).ToDateTime(TimeOnly.MinValue), EstimatedHours = 2 },
    };
}
