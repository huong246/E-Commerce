namespace SaleManagementRewrite.Schemas;

public record GetItemsRequest(
    Guid? CategoryId,
    string SearchTerm,
    decimal? MinPrice,
    decimal? MaxPrice,
    int Page = 1,
    int PageSize = 10 );
public record ItemResponse (Guid Id , string? Name, decimal Price,string? Description , string? Color , string? Size, Guid CategoryId , string? CategoryName);
public record CreateCategoryRequest(string Name);
public record UpdateCategoryRequest(Guid CategoryId, string Name);
public record DeleteCategoryRequest(Guid CategoryId);