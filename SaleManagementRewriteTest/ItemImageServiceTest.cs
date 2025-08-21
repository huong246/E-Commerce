using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;
using SaleManagementRewrite.Services;

namespace SaleManagementRewriteTest;

public class ItemImageServiceTest
{
    [Fact]
    public async Task UploadItemImage_WhenRequestValid_ReturnsSuccess()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789", 
            FullName = "John Doe",
            UserRole = UserRoles.Seller,
            PhoneNumber = "0888888888",
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "1232344"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 20,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            CategoryId = category.Id,
            Category = category,
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();
        
        var temUploadPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(temUploadPath);
        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        mockWebHostEnvironment.Setup(m=>m.WebRootPath).Returns(temUploadPath);
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var mockFormFile = new Mock<IFormFile>();
        mockFormFile.Setup(f => f.Length).Returns(1024);
        mockFormFile.Setup(f => f.FileName).Returns("test-image.png");
        mockFormFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var request = new UploadItemImageRequest(item.Id, mockFormFile.Object, true);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemImageService = new ItemImageService(mockHttpContextAccessor.Object, dbContext, mockWebHostEnvironment.Object, mockUserManager.Object);
        var result = await itemImageService.UploadItemImage(request);
        Assert.True(result.IsSuccess);
        var itemImage = await dbContext.ItemImages.FirstOrDefaultAsync(i => i.ItemId == item.Id);
        Assert.NotNull(itemImage);
    }

    [Fact]
    public async Task UploadItemImage_WhenTokenInvalid_ReturnsTokenInvalid()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789", 
            FullName = "John Doe",
            UserRole = UserRoles.Seller,
            PhoneNumber = "0888888888",
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "1232344"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 20,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            CategoryId = category.Id,
            Category = category,
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();
        
        var temUploadPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(temUploadPath);
        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        mockWebHostEnvironment.Setup(m=>m.WebRootPath).Returns(temUploadPath);
        
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, "null")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var mockFormFile = new Mock<IFormFile>();
        mockFormFile.Setup(f => f.Length).Returns(1024);
        mockFormFile.Setup(f => f.FileName).Returns("test-image.png");
        mockFormFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var request = new UploadItemImageRequest(item.Id, mockFormFile.Object, true);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemImageService = new ItemImageService(mockHttpContextAccessor.Object, dbContext, mockWebHostEnvironment.Object, mockUserManager.Object);
        var result = await itemImageService.UploadItemImage(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task UploadItemImage_WhenUserNotFound_ReturnsUserNotFound()
    {
         var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.SaveChangesAsync();
        
        var temUploadPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(temUploadPath);
        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        mockWebHostEnvironment.Setup(m=>m.WebRootPath).Returns(temUploadPath);
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var mockFormFile = new Mock<IFormFile>();
        mockFormFile.Setup(f => f.Length).Returns(1024);
        mockFormFile.Setup(f => f.FileName).Returns("test-image.png");
        mockFormFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var request = new UploadItemImageRequest(Guid.NewGuid(), mockFormFile.Object, true);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(Guid.NewGuid().ToString())).ReturnsAsync((User)null!);
        var itemImageService = new ItemImageService(mockHttpContextAccessor.Object, dbContext, mockWebHostEnvironment.Object, mockUserManager.Object);
        var result = await itemImageService.UploadItemImage(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
    [Fact]
    public async Task UploadItemImage_WhenShopNotFound_ReturnsShopNotFound()
    {
         var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789", 
            FullName = "John Doe",
            UserRole = UserRoles.Seller,
            PhoneNumber = "0888888888",
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        
        var temUploadPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(temUploadPath);
        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        mockWebHostEnvironment.Setup(m=>m.WebRootPath).Returns(temUploadPath);
        
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var mockFormFile = new Mock<IFormFile>();
        mockFormFile.Setup(f => f.Length).Returns(1024);
        mockFormFile.Setup(f => f.FileName).Returns("test-image.png");
        mockFormFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var request = new UploadItemImageRequest(Guid.NewGuid(), mockFormFile.Object, true);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemImageService = new ItemImageService(mockHttpContextAccessor.Object, dbContext, mockWebHostEnvironment.Object, mockUserManager.Object);
        var result = await itemImageService.UploadItemImage(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UploadItemImage_WhenItemNotFound_ReturnsItemNotFound()
    {
         var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789", 
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller,
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.SaveChangesAsync();
        
        var temUploadPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(temUploadPath);
        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        mockWebHostEnvironment.Setup(m=>m.WebRootPath).Returns(temUploadPath);
        
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var mockFormFile = new Mock<IFormFile>();
        mockFormFile.Setup(f => f.Length).Returns(1024);
        mockFormFile.Setup(f => f.FileName).Returns("test-image.png");
        mockFormFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var request = new UploadItemImageRequest(Guid.NewGuid(),mockFormFile.Object, true);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemImageService = new ItemImageService(mockHttpContextAccessor.Object, dbContext, mockWebHostEnvironment.Object, mockUserManager.Object);
        var result = await itemImageService.UploadItemImage(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UploadItemImage_WhenFileInvalid_ReturnsFileInvalid()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789", 
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller,
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "1232344"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 20,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            CategoryId = category.Id,
            Category = category,
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();
        
        var temUploadPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(temUploadPath);
        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        mockWebHostEnvironment.Setup(m=>m.WebRootPath).Returns(temUploadPath);
        
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var mockFormFile = new Mock<IFormFile>();
        mockFormFile.Setup(f => f.Length).Returns(0);
        mockFormFile.Setup(f => f.FileName).Returns("test-image.png");
        mockFormFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var request = new UploadItemImageRequest(item.Id, mockFormFile.Object, true);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemImageService = new ItemImageService(mockHttpContextAccessor.Object, dbContext, mockWebHostEnvironment.Object, mockUserManager.Object);
        var result = await itemImageService.UploadItemImage(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task UploadItemImage_WhenDatabaseError_ReturnsDatabaseError()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var dbContext = new Mock<ApiDbContext>(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789", 
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller,
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "1232344"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 20,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            CategoryId = category.Id,
            Category = category,
        };
        dbContext.Setup(x => x.Users).ReturnsDbSet(new List<User> { user });
        dbContext.Setup(x => x.Shops).ReturnsDbSet(new List<Shop> { shop });
        dbContext.Setup(x=>x.Categories).ReturnsDbSet(new List<Category> { category });
        dbContext.Setup(x => x.Items).ReturnsDbSet(new List<Item> { item });
        dbContext.Setup(x => x.ItemImages).ReturnsDbSet(new List<ItemImage>());
        
        var temUploadPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(temUploadPath);
        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        mockWebHostEnvironment.Setup(m=>m.WebRootPath).Returns(temUploadPath);
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        
        dbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Simulated database error"));
        var mockFormFile = new Mock<IFormFile>();
        mockFormFile.Setup(f => f.Length).Returns(1024);
        mockFormFile.Setup(f => f.FileName).Returns("test-image.png");
        mockFormFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var request = new UploadItemImageRequest(item.Id, mockFormFile.Object, true);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemImageService = new ItemImageService(mockHttpContextAccessor.Object, dbContext.Object, mockWebHostEnvironment.Object, mockUserManager.Object);
        var result = await itemImageService.UploadItemImage(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}