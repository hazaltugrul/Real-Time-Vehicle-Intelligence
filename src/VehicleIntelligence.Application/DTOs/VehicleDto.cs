namespace VehicleIntelligence.Application.DTOs;

public sealed record VehicleDto(
    Guid Id,
    string VehicleExternalId,
    string Status,
    DateTime CreatedAt,
    DateTime LastSeenAt
);

public sealed record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
