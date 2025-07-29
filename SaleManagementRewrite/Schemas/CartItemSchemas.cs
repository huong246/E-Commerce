namespace SaleManagementRewrite.Schemas;

public record AddItemToCartRequest(Guid ItemId, int Quantity);
public record UpdateQuantityItemInCartRequest(Guid ItemId, int Quantity);
public record DeleteItemFromCartRequest(Guid ItemId);