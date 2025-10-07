using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Services;

public class ItemImageService(
    IHttpContextAccessor httpContextAccessor,
    ApiDbContext dbContext,
    IWebHostEnvironment webHostEnvironment, UserManager<User> userManager)
    : IItemImageService
{
    public async Task<Result<ItemImageResponse>> UploadItemImage(UploadItemImageRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<ItemImageResponse>.Failure("Token invalid", ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<ItemImageResponse>.Failure("User not found", ErrorType.NotFound);
        }
        var isSeller = await userManager.IsInRoleAsync(user, UserRoles.Seller);
        var isAdmin = await userManager.IsInRoleAsync(user, UserRoles.Admin);
        if (!isSeller && !isAdmin)
        {
            return Result<ItemImageResponse>.Failure("User not permitted", ErrorType.Conflict);
        }
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        if (shop == null)
        {
            return Result<ItemImageResponse>.Failure("Shop not found", ErrorType.NotFound);
        }
        var item =  await dbContext.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId && i.ShopId == shop.Id);
        if (item == null)
        {
            return Result<ItemImageResponse>.Failure("Item not found", ErrorType.NotFound);
        }

        if (request.File.Length == 0)
        {
            return Result<ItemImageResponse>.Failure("File invalid", ErrorType.Conflict);
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
        if (request.IsAvatar)
        {
            item.ImageAvatarUrl = itemImage.ImageUrl;
        }
        try
        {
            dbContext.ItemImages.Add(itemImage);
            dbContext.Items.Update(item);
            await dbContext.SaveChangesAsync();
            var response = new ItemImageResponse(itemImage.Id, item.Id, itemImage.ImageUrl, itemImage.IsAvatar);
            return Result<ItemImageResponse>.Success(response);
        }
        catch (DbUpdateException)
        {
            return Result<ItemImageResponse>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<bool>> DeleteItemImage(DeleteItemImageRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<bool>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<bool>.Failure("User not found", ErrorType.NotFound);
        }
        var isSeller = await userManager.IsInRoleAsync(user, UserRoles.Seller);
        var isAdmin = await userManager.IsInRoleAsync(user, UserRoles.Admin);
        if (!isSeller && !isAdmin)
        {
            return Result<bool>.Failure("User not permitted", ErrorType.Conflict);
        }
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        if (shop == null)
        {
            return Result<bool>.Failure("Shop not found", ErrorType.NotFound);
        }
        var item =  await dbContext.Items.FirstOrDefaultAsync(i => i.ShopId == shop.Id && i.ItemImages.FirstOrDefault(it => it.Id == request.ItemImageId) != null);
        if (item == null)
        {
            return Result<bool>.Failure("Item not found", ErrorType.NotFound);
        }

        var itemImage = await dbContext.ItemImages.FirstOrDefaultAsync(i => i.Id == request.ItemImageId && i.ItemId == item.Id);
        if (itemImage == null)
        {
            return Result<bool>.Failure("ItemImage not found", ErrorType.NotFound);
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
            return Result<bool>.Success(true);
        }
        catch (DbUpdateException)
        {
            return Result<bool>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<ItemImage>> SetIsAvatar(SetIsAvatarRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<ItemImage>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<ItemImage>.Failure("User not found", ErrorType.NotFound);
        }
        var isSeller = await userManager.IsInRoleAsync(user, UserRoles.Seller);
        if (!isSeller)
        {
            return Result<ItemImage>.Failure("User not permitted", ErrorType.Conflict);
        }
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        if (shop == null)
        {
            return Result<ItemImage>.Failure("Shop not found", ErrorType.NotFound);
        }
        var item =  await dbContext.Items.FirstOrDefaultAsync(i => i.ShopId == shop.Id &&  i.ItemImages.FirstOrDefault(it => it.Id == request.ItemImageId) != null);
        if (item == null)
        {
            return Result<ItemImage>.Failure("Item not found", ErrorType.NotFound);
        }

        var itemImage = await dbContext.ItemImages.FirstOrDefaultAsync(i => i.Id == request.ItemImageId && i.ItemId == item.Id);
        if (itemImage == null)
        {
            return Result<ItemImage>.Failure("ItemImage not found", ErrorType.NotFound);
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
            return Result<ItemImage>.Success(itemImage);
        }
        catch (DbUpdateException)
        {
            return Result<ItemImage>.Failure("Database error", ErrorType.Conflict);
        }
    }
}