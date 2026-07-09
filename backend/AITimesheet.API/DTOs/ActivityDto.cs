namespace AITimesheet.API.DTOs;

public record ActivityDto(Guid Id, string Source, string Title, string? Status, DateOnly ActivityDate, double? EstimatedHours);
