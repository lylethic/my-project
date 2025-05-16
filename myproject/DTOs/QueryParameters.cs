using System;

namespace myproject.DTOs;

public class QueryParameters
{
  public bool? IsActive { get; set; } = true;
  public int Page { get; set; } = 1;
  public int PageSize { get; set; } = 10;
  public string SearchTerm { get; set; } = string.Empty;
  public string SortBy { get; set; } = "Id";
  public bool SortDescending { get; set; } = false;
}