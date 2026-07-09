namespace AITimesheet.API.DTOs;

public record ConnectAccountRequest(Guid UserId, string Provider, string AccessToken, string? RefreshToken, string? ExternalAccountId);

public record ConnectionStatusDto(string Provider, bool IsConnected, DateTime? ConnectedAt);
