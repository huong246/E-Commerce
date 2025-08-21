using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;
using SaleManagementRewrite.Services;

namespace SaleManagementRewriteTest;

public class CartItemServiceTest
{
    [Fact]
    public async Task AddItemToCart_WhenRequestValid_ReturnsSuccess()
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
            Name = "category",
            Items = new List<Item>()
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
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            UserRole = UserRoles.Customer,
            PhoneNumber = "08818888888",
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var request = new AddItemToCartRequest(item.Id, 10);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await cartItemService.AddItemToCart(request);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AddItemToCart_WhenTokenInvalid_ReturnTokenInvalid()
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
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.SaveChangesAsync();


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
        var request = new AddItemToCartRequest(item.Id, 10);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await cartItemService.AddItemToCart(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task AddItemToCart_WhenUserNotFound_ReturnsUserNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.SaveChangesAsync();


        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
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
        var request = new AddItemToCartRequest(Guid.NewGuid(), 10);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(Guid.NewGuid().ToString())).ReturnsAsync((User)null!);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await cartItemService.AddItemToCart(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task AddItemToCart_WhenNotAddItemOwner_ReturnsNotAddItemOwner()
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
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();


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
        var request = new AddItemToCartRequest(item.Id, 10);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await cartItemService.AddItemToCart(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task AddItemToCart_WhenItemNotFound_ReturnsItemNotFound()
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
            Name = "category",
            Items = new List<Item>()
        };
        var user1Id = Guid.NewGuid();
        var user1 = new User()
        {
            Id = user1Id,
            UserName = "1232234567",
            PasswordHash = "1234256789",
            FullName = "John Doe",
            UserRole = UserRoles.Customer,
            PhoneNumber = "08828888888",
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, user1);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.SaveChangesAsync();


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
        var request = new AddItemToCartRequest(Guid.NewGuid(), 10);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(user1Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user1, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user1, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await cartItemService.AddItemToCart(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task AddItemToCart_WhenQuantityInvalid_ReturnsQuantityInvalid()
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
            Name = "category",
            Items = new List<Item>()
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
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();


        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
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
        var request = new AddItemToCartRequest(item.Id, -10);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await cartItemService.AddItemToCart(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task AddItemToCart_WhenOutOfStock_ReturnsOutOfStock()
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
        var user2Id = Guid.NewGuid();
        var user2 = new User()
        {
            Id = user2Id,
            UserName = "12323456789",
            PasswordHash = "1234567890",
            UserRole = UserRoles.Customer,
            FullName = "John Doe1",
            PhoneNumber = "08888888818",
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 0,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, user2);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();


        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, user2Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var request = new AddItemToCartRequest(item.Id, 10);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(user2.Id.ToString())).ReturnsAsync(user2);
        mockUserManager.Setup(x => x.AddToRoleAsync(user2, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user2, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await cartItemService.AddItemToCart(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task AddItemToCart_WhenInsufficientStock_ReturnsInsufficientStock()
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
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);

        var request = new AddItemToCartRequest(item.Id, 50);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await cartItemService.AddItemToCart(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task AddItemToCart_WhenDatabaseError_ReturnsDatabaseError()
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
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
        };
        var userList = new List<User> { user, customer };
        dbContext.Setup(x => x.Users).ReturnsDbSet(userList);
        dbContext.Setup(x => x.Shops).ReturnsDbSet(new List<Shop> { shop });
        dbContext.Setup(x => x.Items).ReturnsDbSet(new List<Item> { item });
        dbContext.Setup(x => x.ItemImages).ReturnsDbSet(new List<ItemImage>());
        dbContext.Setup(x => x.CartItems).ReturnsDbSet(new List<CartItem>());
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        dbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Simulated database error"));
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService =
            new CartItemService(mockHttpContextAccessor.Object, dbContext.Object, mockUserManager.Object);
        var request = new AddItemToCartRequest(item.Id, 10);
        var result = await cartItemService.AddItemToCart(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task UpdateCartItem_WhenRequestValid_ReturnsSuccess()
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
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            UserRole = UserRoles.Customer,
            PhoneNumber = "08818888888",
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 1,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new UpdateQuantityItemInCartRequest(item.Id, 1);
        var result = await cartItemService.UpdateQuantityItem(request);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, cartItem.Quantity);
    }

    [Fact]
    public async Task UpdateCartItem_WhenTokenInvalid_ReturnsTokenInvalid()
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
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            UserRole = UserRoles.Customer,
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 1,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = customer.Id,
            ShopId = shop.Id,
            Shop = shop,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, "null")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new UpdateQuantityItemInCartRequest(item.Id, 1);
        var result = await cartItemService.UpdateQuantityItem(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task UpdateCartItem_WhenUserNotFound_ReturnsUserNotFound()
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
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
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
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new UpdateQuantityItemInCartRequest(item.Id, 1);
        var result = await cartItemService.UpdateQuantityItem(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UpdateCartItem_WhenCartItemNotFound_ReturnsCartItemNotFound()
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
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new UpdateQuantityItemInCartRequest(item.Id, 1);
        var result = await cartItemService.UpdateQuantityItem(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UpdateCartItem_WhenItemNotFound_ReturnsItemNotFound()
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
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer,
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 1,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = customer.Id,
            ShopId = shop.Id,
            Shop = shop,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new UpdateQuantityItemInCartRequest(Guid.NewGuid(), 1);
        var result = await cartItemService.UpdateQuantityItem(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UpdateCartItem_WhenQuantityInvalid_ReturnsQuantityInvalid()
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
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer,
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 1,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = customer.Id,
            ShopId = shop.Id,
            Shop = shop,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new UpdateQuantityItemInCartRequest(item.Id, -10);
        var result = await cartItemService.UpdateQuantityItem(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task UpdateCartItem_WhenOutOfStock_ReturnsOutOfStock()
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
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 0,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer,
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 1,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = customer.Id,
            ShopId = shop.Id,
            Shop = shop,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new UpdateQuantityItemInCartRequest(item.Id, 1);
        var result = await cartItemService.UpdateQuantityItem(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task UpdateCartItem_WhenInsufficientStock_ReturnsInsufficientStock()
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
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer,
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 1,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = customer.Id,
            ShopId = shop.Id,
            Shop = shop,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Categories.AddAsync(category);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new UpdateQuantityItemInCartRequest(item.Id, 100);
        var result = await cartItemService.UpdateQuantityItem(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task UpdateCartItem_WhenDatabaseError_ReturnsDatabaseError()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var mockDbContext = new Mock<ApiDbContext>(options) { CallBase = true };
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
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer,
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 1,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = customer.Id,
            ShopId = shop.Id,
            Shop = shop,
        };
        await mockDbContext.Object.Database.EnsureCreatedAsync();
        mockDbContext.Object.Users.Add(user);
        mockDbContext.Object.Shops.Add(shop);
        mockDbContext.Object.Addresses.Add(address);
        mockDbContext.Object.Items.Add(item);
        mockDbContext.Object.Users.Add(customer);
        mockDbContext.Object.CartItems.Add(cartItem);
        mockDbContext.Object.Categories.Add(category);
        await mockDbContext.Object.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService =
            new CartItemService(mockHttpContextAccessor.Object, mockDbContext.Object, mockUserManager.Object);

        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Simulated database error"));
        var request = new UpdateQuantityItemInCartRequest(item.Id, 1);
        var result = await cartItemService.UpdateQuantityItem(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task DeleteItemFromCart_WhenRequestValid_ReturnsSuccess()
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
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer,
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 1,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = customer.Id,
            ShopId = shop.Id,
            Shop = shop,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new DeleteItemFromCartRequest(item.Id);
        var result = await cartItemService.DeleteItemFromCart(request);
        Assert.True(result.IsSuccess);
        var cartItem1 = await dbContext.CartItems.FirstOrDefaultAsync(c => c.Id == cartItem.Id);
        Assert.Null(cartItem1);
    }

    [Fact]
    public async Task DeleteItemFromCart_WhenTokenInvalid_ReturnsTokenInvalid()
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
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer,
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 1,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = customer.Id,
            ShopId = shop.Id,
            Shop = shop,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, "null")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new DeleteItemFromCartRequest(item.Id);
        var result = await cartItemService.DeleteItemFromCart(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task DeleteItemFromCart_WhenUserNotFound_ReturnsUserNotFound()
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
            Name = "category",
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
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
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new DeleteItemFromCartRequest(item.Id);
        var result = await cartItemService.DeleteItemFromCart(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DeleteItemFromCart_WhenItemNotFound_ReturnsItemNotFound()
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
            Name = "category",
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer,
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 1,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = customer.Id,
            ShopId = shop.Id,
            Shop = shop,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Items.AddAsync(item);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new DeleteItemFromCartRequest(Guid.NewGuid());
        var result = await cartItemService.DeleteItemFromCart(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DeleteItemFromCart_WhenCartItemNotFound_ReturnsCartItemNotFound()
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
            Name = "category",
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new DeleteItemFromCartRequest(item.Id);
        var result = await cartItemService.DeleteItemFromCart(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DeleteItemFromCart_WhenDatabaseError_ReturnsDatabaseError()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var mockDbContext = new Mock<ApiDbContext>(options) { CallBase = true };
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
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer,
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 1,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = customer.Id,
            ShopId = shop.Id,
            Shop = shop,
        };
        await mockDbContext.Object.Database.EnsureCreatedAsync();
        mockDbContext.Object.Users.Add(user);
        mockDbContext.Object.Shops.Add(shop);
        mockDbContext.Object.Addresses.Add(address);
        mockDbContext.Object.Categories.Add(category);
        mockDbContext.Object.Items.Add(item);
        mockDbContext.Object.Users.Add(customer);
        mockDbContext.Object.CartItems.Add(cartItem);
        await mockDbContext.Object.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Simulated database error"));
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var cartItemService =
            new CartItemService(mockHttpContextAccessor.Object, mockDbContext.Object, mockUserManager.Object);
        var request = new DeleteItemFromCartRequest(item.Id);
        var result = await cartItemService.DeleteItemFromCart(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
