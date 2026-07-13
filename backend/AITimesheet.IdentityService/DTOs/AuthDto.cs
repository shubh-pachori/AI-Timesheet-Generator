namespace AITimesheet.IdentityService.DTOs;

public record LoginRequestDto(string Email, string FullName, string? AzureAdObjectId);
public record UserDto(Guid Id, string FullName, string Email, string Role, Guid? ManagerId);
public record AuthResponseDto(UserDto User, string Token);
