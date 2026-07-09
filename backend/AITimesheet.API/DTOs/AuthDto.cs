namespace AITimesheet.API.DTOs;

public record LoginRequest(string Email, string FullName, string? AzureAdObjectId);
public record UserDto(Guid Id, string FullName, string Email, string Role);
