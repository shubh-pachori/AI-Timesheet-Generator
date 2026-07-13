using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace AITimesheet.TimesheetService.ServiceLayer.Clients;

public record UserDto(Guid Id, string FullName, string Email, string Role, Guid? ManagerId);

public class IdentityServiceClient
{
    private readonly HttpClient _http;

    public IdentityServiceClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        var baseUrl = config["Services:IdentityServiceUrl"] ?? "http://localhost:5081/";
        _http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<UserDto>($"api/auth/internal/users/{userId}", ct);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<UserDto>> GetEmployeesByManagerIdAsync(Guid managerId, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<UserDto>>($"api/auth/internal/users/manager/{managerId}", ct) ?? new List<UserDto>();
        }
        catch
        {
            return new List<UserDto>();
        }
    }
}
