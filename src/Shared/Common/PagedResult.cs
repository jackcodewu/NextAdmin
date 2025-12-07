namespace NextAdmin.Shared.Common;

/// <summary>
/// 分页响应结果
/// </summary>
public class PagedResult<T>
{
    /// <summary>
    /// 数据项
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// 起始记录索引
    /// </summary>
    public int StartIndex => (PageNumber - 1) * PageSize + 1;

    /// <summary>
    /// 结束记录索引
    /// </summary>
    public int EndIndex => Math.Min(StartIndex + PageSize - 1, TotalCount);
} 
