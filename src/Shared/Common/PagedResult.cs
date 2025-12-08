namespace NextAdmin.Shared.Common;

/// <summary>
/// Paged response result
/// </summary>
public class PagedResult<T>
{
    /// <summary>
    /// Data items
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Total record count
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether has previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether has next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Start record index
    /// </summary>
    public int StartIndex => (PageNumber - 1) * PageSize + 1;

    /// <summary>
    /// End record index
    /// </summary>
    public int EndIndex => Math.Min(StartIndex + PageSize - 1, TotalCount);
} 
