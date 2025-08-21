using System.Security.Claims;
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

public class ItemServiceTest
{
    [Fact]
    public void Debug_EFCore_Model_Configuration()
    {
        // 1. Sắp đặt môi trường test y hệt như các test khác
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        using var dbContext = new ApiDbContext(options);

        // 2. Lấy ra "bản thiết kế" chi tiết của model mà EF Core đang sử dụng
        var debugView = dbContext.Model.ToDebugString();

        // 3. In "bản thiết kế" này ra màn hình console của test runner
        Console.WriteLine(debugView);

        // 4. Kiểm tra xem "bản thiết kế" này có thực sự chứa thông tin về "Category" không
        Assert.Contains("Category", debugView);
    }
    [Fact]
    public async Task CreateItemAsync_WhenRequestValid_ReturnsSuccess()
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
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "test",
        };
        user.Addresses = [address];
        address.User = user;
        shop.User = user;
        shop.Address = address;

        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new CreateItemRequest("testItem", 10, 10, "Clothes", "blue", "100", category.Id);
        var result = await itemService.CreateItemAsync(request);
        Assert.True(result.IsSuccess);
        var item = await dbContext.Items.FirstOrDefaultAsync(i => i.ShopId == shop.Id);
        Assert.NotNull(item);
        Assert.Equal("testItem", item.Name);
    }
    [Fact]
    public async Task CreateItemAsync_WhenTokenInvalid_ReturnTokenInvalid()
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
            User = user,
            UserId = userId,
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
            Name = "test",
        };

        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.SaveChangesAsync();


        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "null") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new CreateItemRequest("testItem", 10, 10, "Clothes", "blue", "100", category.Id);
        var result = await itemService.CreateItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task CreateItemAsync_WhenUserNotFound_ReturnsUserNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "test",
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(Guid.NewGuid().ToString())).ReturnsAsync((User)null!);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new CreateItemRequest("testItem", 10, 10, "Clothes", "blue", "100", category.Id);
        var result = await itemService.CreateItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CreateItemAsync_WhenShopNotFound_ReturnsShopNotFound()
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
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "test",
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new CreateItemRequest("testItem", 10, 10, "Clothes", "blue", "100", category.Id);
        var result = await itemService.CreateItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CreateItemAsync_WhenInvalidValue_ReturnInvalidValue()
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
            User = user,
            UserId = userId,
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
        }; var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "test",
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.SaveChangesAsync();


        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new CreateItemRequest("testItem", -10, -10, "Clothes", "blue", "100", category.Id);
        var result = await itemService.CreateItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CreateItemAsync_WhenDatabaseError_ReturnsDatabaseError()
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        var mockDbContext = new Mock<ApiDbContext>(options) { CallBase = true };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Email = "12345678",
            UserRole = UserRoles.Seller,
            Birthday = new DateTime(1999, 1, 1),
            Gender = "male",
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            User = user,
            UserId = userId,
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
            Name = "test",
        };
        mockDbContext.Object.Users.Add(user);
        mockDbContext.Object.Shops.Add(shop);
        mockDbContext.Object.Addresses.Add(address);
        mockDbContext.Object.Categories.Add(category);
        await mockDbContext.Object.SaveChangesAsync();
        var request = new CreateItemRequest("testItem", 10, 100, "Clothes", "blue", "100", category.Id);
        mockDbContext.Setup(db => db.Users).ReturnsDbSet(new List<User> { user });
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, mockDbContext.Object, mockUserManager.Object);
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Simulated database error"));
        var result = await itemService.CreateItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenRequestValid_ReturnsSuccess()
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
            UserRole = UserRoles.Seller
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
            Name = "test",
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
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Items.AddAsync(item);
        await dbContext.Categories.AddAsync(category);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new UpdateItemRequest(item.Id, "New Update", 50, 20, null, null, null, null);
        var result = await itemService.UpdateItemAsync(request);
        Assert.True(result.IsSuccess);
        Assert.Equal("New Update", item.Name);
        Assert.Equal(20, item.Price);
        Assert.Equal(50, item.Stock);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenTokenInvalid_ReturnsTokenInvalid()
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
            UserRole = UserRoles.Seller
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
            Name = "test",
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
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "null") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new UpdateItemRequest(item.Id, "New Update", 50, 20, null, null, null,null);
        var result = await itemService.UpdateItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenUserNotFound_ReturnsUserNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(Guid.NewGuid().ToString())).ReturnsAsync((User)null!);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new UpdateItemRequest(Guid.NewGuid(), "New Update", 50, 20, null, null, null, null);
        var result = await itemService.UpdateItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenShopNotFound_ReturnsShopNotFound()
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
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new UpdateItemRequest(Guid.NewGuid(), "New Update", 50, 20, null, null, null,null);
        var result = await itemService.UpdateItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenItemNotFound_ReturnsItemNotFound()
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
            UserRole = UserRoles.Seller 
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

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new UpdateItemRequest(Guid.NewGuid(), "New Update", 50, 20, null, null, null,null);
        var result = await itemService.UpdateItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenDuplicateValue_ReturnsDuplicateValue()
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
            Name = "test",
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
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new UpdateItemRequest(item.Id, "item", 20, 100, null, null, null,null);
        var result = await itemService.UpdateItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenInvalidValue_ReturnsInvalidValue()
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
            UserRole = UserRoles.Seller
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
            Name = "test",
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
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new UpdateItemRequest(item.Id, "New Update", -50, -20, null, null, null,null);
        var result = await itemService.UpdateItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenDatabaseError_ReturnsDatabaseError()
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        var mockDbContext = new Mock<ApiDbContext>(options) { CallBase = true };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Email = "12345678",
            Birthday = new DateTime(1999, 1, 1),
            Gender = "male",
            UserRole = UserRoles.Seller,
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            User = user,
            UserId = userId,
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
            Name = "test",
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
            Category = category,
            CategoryId = category.Id,
        };
        mockDbContext.Object.Users.Add(user);
        mockDbContext.Object.Shops.Add(shop);
        mockDbContext.Object.Addresses.Add(address);
        mockDbContext.Object.Items.Add(item);
        mockDbContext.Object.Categories.Add(category);
        await mockDbContext.Object.SaveChangesAsync();
        var request = new UpdateItemRequest(item.Id, "New Update", 50, 20, null, null, null,null);
        mockDbContext.Setup(db => db.Users).ReturnsDbSet(new List<User> { user });
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, mockDbContext.Object, mockUserManager.Object);
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Simulated database error"));
        var result = await itemService.UpdateItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task DeleteItemAsync_WhenRequestValid_ReturnsSuccess()
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
            Name = "quan ao",
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
            Category = category,
            CategoryId = category.Id,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new DeleteItemRequest(item.Id);
        var result = await itemService.DeleteItemAsync(request);
        Assert.True(result.IsSuccess);
        var item1 = await dbContext.Items.FirstOrDefaultAsync(i => i.Id == item.Id);
        Assert.Null(item1);
    }

    [Fact]
    public async Task DeleteItemAsync_WhenTokenInvalid_ReturnsTokenInvalid()
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
            Name = "quan ao",
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
            Category = category,
            CategoryId = category.Id,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "null") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new DeleteItemRequest(item.Id);
        var result = await itemService.DeleteItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task DeleteItemAsync_WhenUserNotFound_ReturnsUserNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(Guid.NewGuid().ToString())).ReturnsAsync((User)null!);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new DeleteItemRequest(Guid.NewGuid());
        var result = await itemService.DeleteItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DeleteItemAsync_WhenShopNotFound_ReturnsShopNotFound()
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
            PasswordHash ="123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new DeleteItemRequest(Guid.NewGuid());
        var result = await itemService.DeleteItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DeleteItemAsync_WhenItemNotFound_ReturnsItemNotFound()
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
            UserRole = UserRoles.Seller
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

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new DeleteItemRequest(Guid.NewGuid());
        var result = await itemService.DeleteItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DeleteItemAsync_WhenDatabaseError_ReturnsDatabaseError()
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        var mockDbContext = new Mock<ApiDbContext>(options) { CallBase = true };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Email = "12345678",
            Birthday = new DateTime(1999, 1, 1),
            Gender = "male",
            UserRole = UserRoles.Seller,
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            User = user,
            UserId = userId,
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
        };
        mockDbContext.Object.Users.Add(user);
        mockDbContext.Object.Shops.Add(shop);
        mockDbContext.Object.Addresses.Add(address);
        mockDbContext.Object.Items.Add(item);
        await mockDbContext.Object.SaveChangesAsync();


        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var itemService = new ItemService(mockHttpContextAccessor.Object, mockDbContext.Object, mockUserManager.Object);
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Simulated database error"));
        var request = new DeleteItemRequest(item.Id);
        var result = await itemService.DeleteItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
