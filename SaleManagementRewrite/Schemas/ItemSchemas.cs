namespace SaleManagementRewrite.Schemas;

public record CreateItemRequest(string Name, int Stock, decimal Price, string Description, string? Color, string? Size);
public record UpdateItemRequest(Guid ItemId,string? Name, int? Stock, decimal? Price, string? Description, string? Color, string? Size);
public record DeleteItemRequest(Guid ItemId);