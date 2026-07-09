namespace AITimesheet.API.DTOs;

public record ChatRequest(Guid UserId, string Question);
public record ChatResponse(string Answer);
