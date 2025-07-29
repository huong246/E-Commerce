using SaleManagementRewrite.Entities.Enum;

namespace SaleManagementRewrite.Schemas;

public record CreateVoucherRequest(Guid? ShopId, Guid? ItemId, int LengthCode, int Quantity, decimal Value, Method Method, Target Target, decimal? MaxDiscountAmount, DateTime StartDate, DateTime EndDate,decimal? MinSpend, bool IsActive);
public record DeleteVoucherRequest(Guid VoucherId);
public record UpdateVoucherRequest(Guid VoucherId, Guid? ItemId, int? Quantity, decimal? Value, Method? Method, Target? Target, decimal? MaxDiscountAmount, DateTime? StartDate, DateTime? EndDate, decimal? MinSpend ,bool? IsActive, byte[] RowVersion);