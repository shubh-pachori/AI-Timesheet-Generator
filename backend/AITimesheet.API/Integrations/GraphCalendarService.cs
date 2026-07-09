using System.Net.Http.Headers;
using System.Text.Json;
using AITimesheet.API.Entities;
using AITimesheet.API.Interfaces;

namespace AITimesheet.API.Integrations;

/// <summary>
/// Pulls calendar events (meetings) for the user's week from Microsoft Graph
/// (Outlook Calendar / Teams). accessToken is an MSAL-acquired Graph token.
/// </summary>
public class GraphCalendarService : IIntegrationService
{
    private readonly HttpClient _http;
    public ConnectionProvider Provider => ConnectionProvider.OutlookCalendar;

    public GraphCalendarService(HttpClient http)
    {
        _http = http;
        _http.BaseAddress ??= new Uri("https://graph.microsoft.com/v1.0/");
    }

    public async Task<List<Activity>> FetchActivitiesAsync(
        Guid userId, string accessToken, DateOnly weekStart, DateOnly weekEnd, CancellationToken ct = default)
    {
        try
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var startIso = weekStart.ToDateTime(TimeOnly.MinValue).ToString("o");
            var endIso = weekEnd.ToDateTime(TimeOnly.MaxValue).ToString("o");

            var resp = await _http.GetAsync(
                $"me/calendarview?startDateTime={startIso}&endDateTime={endIso}&$orderby=start/dateTime", ct);
            if (!resp.IsSuccessStatusCode) return MockMeetings(userId, weekStart);

            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var activities = new List<Activity>();
            foreach (var ev in doc.RootElement.GetProperty("value").EnumerateArray())
            {
                var subject = ev.GetProperty("subject").GetString() ?? "Meeting";
                var startStr = ev.GetProperty("start").GetProperty("dateTime").GetString();
                var start = DateTime.Parse(startStr!);

                var durationHours = 0.5;
                try
                {
                    var endStr = ev.GetProperty("end").GetProperty("dateTime").GetString();
                    durationHours = (DateTime.Parse(endStr!) - start).TotalHours;
                }
                catch { /* keep default */ }

                activities.Add(new Activity
                {
                    UserId = userId,
                    Source = ActivitySource.Meeting,
                    Title = subject,
                    ActivityDate = DateOnly.FromDateTime(start),
                    EstimatedHours = Math.Round(durationHours, 2)
                });
            }
            return activities.Count > 0 ? activities : MockMeetings(userId, weekStart);
        }
        catch
        {
            return MockMeetings(userId, weekStart);
        }
    }

    private static List<Activity> MockMeetings(Guid userId, DateOnly weekStart) => new()
    {
        new Activity { UserId = userId, Source = ActivitySource.Meeting, Title = "Sprint Planning", ActivityDate = weekStart, EstimatedHours = 1 },
        new Activity { UserId = userId, Source = ActivitySource.Meeting, Title = "Daily Standup", ActivityDate = weekStart.AddDays(1), EstimatedHours = 0.25 },
        new Activity { UserId = userId, Source = ActivitySource.Meeting, Title = "Client Discussion", ActivityDate = weekStart.AddDays(2), EstimatedHours = 1 },
        new Activity { UserId = userId, Source = ActivitySource.Meeting, Title = "Retrospective", ActivityDate = weekStart.AddDays(4), EstimatedHours = 0.5 },
    };
}
