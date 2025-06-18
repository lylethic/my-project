using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Play.Application.DTOs;

public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = [];
    public int PageSize { get; set; }
    public int Records { get; set; }
    public DateTime? NextCursor { get; set; } //`created_at` of the last record
}

public class PaginationRequest
{
    private int _page = 1;
    private int _pageSize = 20;
    [FromQuery(Name = "page")]
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }
    [FromQuery(Name = "last_created_at")]
    public DateTime? LastCreatedAt { get; set; }
    [FromQuery(Name = "pageSize")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is >= 1 and < 100 ? value : 10;
    }
    [FromQuery(Name = "isActive")]
    public bool? IsActive { get; set; } = true;
}