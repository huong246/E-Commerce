using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Results;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; } 
    public string? Error { get; } 
    public ErrorType ErrorType { get; } 
    private Result(bool isSuccess, T? value, string? error, ErrorType errorType)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorType = errorType;
    }
    public static Result<T> Success(T value) => new Result<T>(true, value, null, ErrorType.None);
    public static Result<T> Failure(string error, ErrorType errorType = ErrorType.BadRequest) 
        => new Result<T>(false, default, error, errorType);
}
public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool HasNextPage => (Page * PageSize) < TotalCount;
    public bool HasPreviousPage => Page > 1;
}
public enum ErrorType
{
    None,
    Validation, 
    NotFound,    
    Conflict,    
    Failure,    
    Unauthorized,
    BadRequest,
    Forbidden,
}