namespace myproject.DTOs
{
  public class ResponseData<T>
  {
    public string Message { get; set; } = "No content";
    public int StatusCode { get; set; } = 204;
    public string? Token { get; set; }
    public DateTime? TokenExpiredTime { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiredTime { get; set; }
    public T? Data { get; set; } = default;
    public List<T>? ListData { get; set; } = new();

    // Constructors
    public ResponseData() { }

    public ResponseData(int status, string message)
    {
      StatusCode = status;
      Message = message;
    }

    public ResponseData(int status, string message, T data) : this(status, message)
    {
      Data = data;
    }

    public ResponseData(int status, string message, List<T> listData) : this(status, message)
    {
      ListData = listData;
    }

    public ResponseData(int status, string message, string token, DateTime? tokenExpiredTime) : this(status, message)
    {
      Token = token;
      TokenExpiredTime = tokenExpiredTime;
    }

    public ResponseData(int status, string message, string token, DateTime? tokenExpiredTime, string refreshToken, DateTime? refreshTokenExpiredTime) : this(status, message)
    {
      Token = token;
      TokenExpiredTime = tokenExpiredTime;
      RefreshToken = refreshToken;
      RefreshTokenExpiredTime = refreshTokenExpiredTime;
    }

    // Static Helpers (optional)
    public static ResponseData<T> Success(T data, string message = "Success")
        => new(200, message, data);

    public static ResponseData<T> Success(string token, DateTime? tokenExpiredTime, string refreshToken, DateTime? refreshTokenExpiredTime, string message = "Success")
      => new(200, message, token, tokenExpiredTime, refreshToken, refreshTokenExpiredTime);

    public static ResponseData<T> Success(List<T> listData, string message = "Success")
        => new(200, message, listData);

    public static ResponseData<T> Success(int statusCode = 200, string message = "Success")
            => new(statusCode, message);

    public static ResponseData<T> Fail(string message, int statusCode = 400)
        => new(statusCode, message);
  }
}
