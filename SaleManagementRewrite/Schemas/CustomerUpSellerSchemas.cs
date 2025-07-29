namespace SaleManagementRewrite.Schemas;

public record ApproveRequest(Guid CustomerUpSellerId);
public record RejectRequest(Guid CustomerUpSellerId);