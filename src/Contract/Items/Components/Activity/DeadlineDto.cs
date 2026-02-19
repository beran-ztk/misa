namespace Misa.Contract.Features.Common.Deadlines;

public record DeadlineDto(DateTimeOffset DueAt, DateTimeOffset CreatedAt);