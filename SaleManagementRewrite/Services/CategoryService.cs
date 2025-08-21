using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;
using SaleManagementRewrite.Entities;

namespace SaleManagementRewrite.Services;

public class CategoryService(ApiDbContext dbContext, UserManager<User> userManager, IHttpContextAccessor httpContextAccessor) : ICategoryService
{
    public async Task<Result<PagedResult<ItemResponse>>> GetItemsAsync(GetItemsRequest request)
    {
        var query = dbContext.Items.Include(item => item.Category).AsQueryable();
        if (request.CategoryId.HasValue  &&request.CategoryId.Value != Guid.Empty)
        {
            query = query.Where(item => item.CategoryId == request.CategoryId.Value);
        }
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = $"\"{request.SearchTerm.Replace("\"", "\"\"")}\"*";
            var matchingItemIds = dbContext.ItemFts
                .FromSqlInterpolated($"SELECT rowid FROM ItemsFTS WHERE ItemsFTS MATCH {searchTerm}")
                .Select(fts => fts.Rowid); 
            query = query.Where(i => matchingItemIds.Contains(i.Id));
        }
        if(request.MinPrice.HasValue)
        {
            query = query.Where(item => item.Price >= request.MinPrice.Value);
        }
        if(request.MaxPrice.HasValue)
        {
            query = query.Where(item => item.Price <= request.MaxPrice.Value);
        }

        if (!string.IsNullOrEmpty(request.SearchTerm))  
        {
            query = request.SearchTerm.ToLower() switch
            {
                "price_asc" => query.OrderBy(i => i.Price),
                "price_desc" => query.OrderByDescending(i => i.Price),
                "best_selling" => query.OrderByDescending(i => i.SaleCount),
                _ => query.OrderByDescending(i => i.Name)
            };
        }
        else
        {
            query = query.OrderByDescending(i => i.Name);
        }

        var totalCount = await query.CountAsync();
        var items = await query.Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize).Select(item => new ItemResponse 
            (
                item.Id,
                item.Name,
                item.Price,
                item.Description,
                item.Color,
                item.Size,
                item.CategoryId,
                item.Category.Name 
            ))
            .ToListAsync();
        var pagedResult = new PagedResult<ItemResponse>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
        return Result<PagedResult<ItemResponse>>.Success(pagedResult);
    }

    public async Task<Result<Category>> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var adminIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(adminIdString, out _))
        {
            return Result<Category>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var admin = await userManager.FindByIdAsync(adminIdString);
        if (admin == null)
        {
            return Result<Category>.Failure("User not found", ErrorType.NotFound);
        }

        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Name == request.Name);
        if (category != null)
        {
            return Result<Category>.Failure("Category exists", ErrorType.Conflict);
        }

        category = new Category()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Items = new List<Item>(),
        };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();
        return Result<Category>.Success(category);
    }

    public async Task<Result<Category>> UpdateCategoryAsync(UpdateCategoryRequest request)
    {
        var adminIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(adminIdString, out _))
        {
            return Result<Category>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var admin = await userManager.FindByIdAsync(adminIdString);
        if (admin == null)
        {
            return  Result<Category>.Failure("User not found", ErrorType.NotFound);
        }
        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId);
        if (category == null)
        {
            return Result<Category>.Failure("Category not found", ErrorType.NotFound);
        }

        if (category.Name == request.Name)
        {
            return Result<Category>.Failure("Duplicate name", ErrorType.Conflict);
        }
        category.Name = request.Name;
        dbContext.Categories.Update(category);
        await dbContext.SaveChangesAsync();
        return Result<Category>.Success(category);
    }

    public async Task<Result<bool>> DeleteCategoryAsync(DeleteCategoryRequest request)
    {
        var adminIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(adminIdString, out _))
        {
            return Result<bool>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var admin = await userManager.FindByIdAsync(adminIdString);
        if (admin == null)
        {
            return Result<bool>.Failure("User not found", ErrorType.NotFound);
        }
        var category = await dbContext.Categories.Include(category => category.Items).FirstOrDefaultAsync(c => c.Id == request.CategoryId);
        if (category == null)
        {
            return Result<bool>.Failure("Category not found", ErrorType.NotFound);
        }

        if (category.Items.Count > 0)
        {
            return Result<bool>.Failure("Not permitted delete", ErrorType.Conflict);
        }
        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync();
        return Result<bool>.Success(true);
    }
}