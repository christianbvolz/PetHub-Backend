namespace PetHub.API.DTOs.Common;

public class PagedResult<T>
{
    public required List<T> Items { get; set; }
    public required int Page { get; set; }
    public required int PageSize { get; set; }
    public required int TotalCount { get; set; }
    public required int TotalPages { get; set; }
    public required bool HasPreviousPage { get; set; }
    public required bool HasNextPage { get; set; }
}
