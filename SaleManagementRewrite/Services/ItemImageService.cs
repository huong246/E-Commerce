using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Services;

public class ItemImageService(
    IHttpContextAccessor httpContextAccessor,
    ApiDbContext dbContext,
    IWebHostEnvironment webHostEnvironment)
    : IItemImageService
{
    public async Task<UploadItemImageResult> UploadItemImage(UploadItemImageRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return UploadItemImageResult.TokenInvalid;
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return UploadItemImageResult.UserNotFound;
        }
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        if (shop == null)
        {
            return UploadItemImageResult.ShopNotFound;
        }
        var item =  await dbContext.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId && i.ShopId == shop.Id);
        if (item == null)
        {
            return UploadItemImageResult.ItemNotFound;
        }

        if (request.File.Length == 0)
        {
            return UploadItemImageResult.FileInvalid;
        } 
        var fileExtension = Path.GetExtension(request.File.FileName); 
        var newFileName = $"{Guid.NewGuid()}{fileExtension}";
        var uploadDirectory = Path.Combine(webHostEnvironment.WebRootPath, "uploads", request.ItemId.ToString());
        Directory.CreateDirectory(uploadDirectory);
        var filePath = Path.Combine(uploadDirectory, newFileName);
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await request.File.CopyToAsync(stream);
        }
        var imageUrl = $"/uploads/{request.ItemId}/{newFileName}";
        if (request.IsAvatar)
        {
            var primaryImages = await dbContext.ItemImages.Where(i => i.ItemId == request.ItemId && i.IsAvatar)
                .ToListAsync();
            foreach (var image in primaryImages)
            {
                image.IsAvatar = false;
            }
        }

        var itemImage = new ItemImage()
        {
            Id = Guid.NewGuid(),
            Item = item,
            ItemId = item.Id,
            IsAvatar = request.IsAvatar,
            ImageUrl = imageUrl,
        };
        try
        {
            dbContext.ItemImages.Add(itemImage);
            await dbContext.SaveChangesAsync();
            return UploadItemImageResult.Success;
        }
        catch (DbUpdateException)
        {
            return UploadItemImageResult.DatabaseError;
        }
    }

    public async Task<DeleteItemImageResult> DeleteItemImage(DeleteItemImageRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return DeleteItemImageResult.TokenInvalid;
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return DeleteItemImageResult.UserNotFound;
        }
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        if (shop == null)
        {
            return DeleteItemImageResult.ShopNotFound;
        }
        var item =  await dbContext.Items.FirstOrDefaultAsync(i => i.ShopId == shop.Id && i.ItemImages.FirstOrDefault(it => it.Id == request.ItemImageId) != null);
        if (item == null)
        {
            return DeleteItemImageResult.ItemNotFound;
        }

        var itemImage = await dbContext.ItemImages.FirstOrDefaultAsync(i => i.Id == request.ItemImageId && i.ItemId == item.Id);
        if (itemImage == null)
        {
            return DeleteItemImageResult.ItemImageItemNotFound;
        }
        try
        { 
            var relativePath = itemImage.ImageUrl.TrimStart('/');
            var filePath =  Path.Combine(webHostEnvironment.WebRootPath, relativePath); 
            if (File.Exists(filePath)) 
            { 
                File.Delete(filePath); 
            }
            dbContext.ItemImages.Remove(itemImage);
            await dbContext.SaveChangesAsync();
            return DeleteItemImageResult.Success;
        }
        catch (DbUpdateException)
        {
            return DeleteItemImageResult.DatabaseError;
        }
    }

    public async Task<SetIsAvatarResult> SetIsAvatar(SetIsAvatarRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return SetIsAvatarResult.TokenInvalid;
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return  SetIsAvatarResult.UserNotFound;
        }
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        if (shop == null)
        {
            return  SetIsAvatarResult.ShopNotFound;
        }
        var item =  await dbContext.Items.FirstOrDefaultAsync(i => i.ShopId == shop.Id &&  i.ItemImages.FirstOrDefault(it => it.Id == request.ItemImageId) != null);
        if (item == null)
        {
            return SetIsAvatarResult.ItemNotFound;
        }

        var itemImage = await dbContext.ItemImages.FirstOrDefaultAsync(i => i.Id == request.ItemImageId && i.ItemId == item.Id);
        if (itemImage == null)
        {
            return SetIsAvatarResult.ItemImageItemNotFound;
        }
        var itemImages = await dbContext.ItemImages.Where(i=>i.ItemId == item.Id && i.IsAvatar).ToListAsync();
        foreach (var image in itemImages)
        {
            image.IsAvatar = false;
        }
        itemImage.IsAvatar = true;
        try
        {
            dbContext.ItemImages.Update(itemImage);
            await dbContext.SaveChangesAsync();
            return SetIsAvatarResult.Success;
        }
        catch (DbUpdateException)
        {
            return SetIsAvatarResult.DatabaseError;
        }
    }
}