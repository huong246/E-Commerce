namespace SaleManagementRewrite.Schemas;

public record UploadItemImageRequest(Guid ItemId, IFormFile File, bool IsAvatar);
public record ItemImageResponse(Guid ItemImageId, Guid ItemId, string? ImageUrl, bool IsAvatar);
public record SetIsAvatarRequest(Guid ItemImageId,bool IsAvatar);
public record DeleteItemImageRequest(Guid ItemImageId);