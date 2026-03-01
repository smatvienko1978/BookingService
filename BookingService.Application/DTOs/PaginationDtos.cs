namespace BookingService.Application.DTOs;

/// <summary>
/// Request parameters for paginated queries.
/// Provides consistent pagination across all list endpoints.
/// </summary>
/// <param name="Page">Page number (1-based). Defaults to 1.</param>
/// <param name="PageSize">Number of items per page. Defaults to 10, max 100.</param>
public record PaginationRequest(int Page = 1, int PageSize = 10)
{
    /// <summary>
    /// Maximum allowed page size to prevent excessive data retrieval.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Gets the validated page number (minimum 1).
    /// </summary>
    public int ValidatedPage => Math.Max(1, Page);

    /// <summary>
    /// Gets the validated page size (between 1 and MaxPageSize).
    /// </summary>
    public int ValidatedPageSize => Math.Clamp(PageSize, 1, MaxPageSize);

    /// <summary>
    /// Calculates the number of items to skip for the current page.
    /// </summary>
    public int Skip => (ValidatedPage - 1) * ValidatedPageSize;
}

/// <summary>
/// Wrapper for paginated response data.
/// Contains the items for the current page plus metadata for navigation.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
/// <param name="Items">The items for the current page.</param>
/// <param name="Page">Current page number.</param>
/// <param name="PageSize">Number of items per page.</param>
/// <param name="TotalCount">Total number of items across all pages.</param>
/// <param name="TotalPages">Total number of pages.</param>
public record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
)
{
    /// <summary>
    /// Whether there is a next page available.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there is a previous page available.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Creates a paginated response from a collection and pagination request.
    /// </summary>
    public static PaginatedResponse<T> Create(IReadOnlyList<T> items, int totalCount, PaginationRequest pagination)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pagination.ValidatedPageSize);
        return new PaginatedResponse<T>(
            items,
            pagination.ValidatedPage,
            pagination.ValidatedPageSize,
            totalCount,
            totalPages
        );
    }
}
