using SaleManagementRewrite.Entities;

namespace SaleManagementRewrite.Schemas;

public record CreateItemRequest(string Name, int Stock, decimal Price, string Description, string? Color, string? Size, Guid CategoryId);

public record CreateItemResponse(Item Item);
public record UpdateItemRequest(Guid ItemId,string? Name, int? Stock, decimal? Price, string? Description, string? Color, string? Size, Guid? CategoryId);
public record DeleteItemRequest(Guid ItemId);