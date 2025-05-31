namespace Play.Application.DTOs;

public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = [];
    public int PageSize { get; set; }
    public DateTime? NextCursor { get; set; } //`created_at` of the last record
}

public class PaginationRequest
{
    private int _page = 1;
    private int _pageSize = 10;
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }
    public DateTime? LastCreatedAt { get; set; }
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is >= 1 and < 100 ? value : 10;
    }
    public bool? IsActive { get; set; } = true;
}