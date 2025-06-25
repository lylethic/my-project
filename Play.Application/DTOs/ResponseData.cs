using Play.Application.Enums;

namespace Play.Application.DTOs
{
  public class ResponseData<T>
  {
    public string Message { get; set; } = "No content";
    public int StatusCode { get; set; } = 204;
    public T? Data { get; set; } = default;
    public List<T>? ListData { get; set; } = [];

    // Constructors
    public ResponseData() { }

    public ResponseData(AuthStatus status, string message)
    {
      StatusCode = (int)status;
      Message = message;
    }

    public ResponseData(AuthStatus status, string message, T data) : this(status, message)
    {
      Data = data;
    }

    public ResponseData(AuthStatus status, string message, List<T> listData) : this(status, message)
    {
      ListData = listData;
    }

    // Static Helpers (optional)
    public static ResponseData<T> Success(T data, string message = "Success")
        => new(AuthStatus.Success, message, data);

    public static ResponseData<T> Success(List<T> listData, string message = "Success")
        => new(AuthStatus.Success, message, listData);

    public static ResponseData<T> Success(AuthStatus statusCode = AuthStatus.Success, string message = "Success")
            => new(statusCode, message);

    public static ResponseData<T> Fail(string message, AuthStatus statusCode = AuthStatus.BadRequest)
        => new(statusCode, message);
  }
}
