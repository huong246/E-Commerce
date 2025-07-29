using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.IServices;
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var result = await cartItemService.AddItemToCart(request);
        Assert.Equal(AddItemToCartResult.Success, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Items.AddAsync(item);
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var result = await cartItemService.AddItemToCart(request);
        Assert.Equal(AddItemToCartResult.TokenInvalid, result);
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var result = await cartItemService.AddItemToCart(request);
        Assert.Equal(AddItemToCartResult.UserNotFound, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
        var item1 = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 20,
            ShopId = shop.Id,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Shop = shop,
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Items.AddAsync(item1);
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
        var request = new AddItemToCartRequest(item1.Id, 10);
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var result = await cartItemService.AddItemToCart(request);
        Assert.Equal(AddItemToCartResult.NotAddItemOwner, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
    
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var result = await cartItemService.AddItemToCart(request);
        Assert.Equal(AddItemToCartResult.ItemNotFound, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var result = await cartItemService.AddItemToCart(request);
        Assert.Equal(AddItemToCartResult.QuantityInvalid, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 0,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        var user2Id = Guid.NewGuid();
        var user2 = new User()
        {
            Id = user2Id,
            Username = "12323456789",
            Password = BCrypt.Net.BCrypt.HashPassword("1234567890"),
            FullName = "John Doe1",
            PhoneNumber = "08888888818",
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(user2);
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var result = await cartItemService.AddItemToCart(request);
        Assert.Equal(AddItemToCartResult.OutOfStock, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var result = await cartItemService.AddItemToCart(request);
        Assert.Equal(AddItemToCartResult.InsufficientStock, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext.Object);
        var request = new AddItemToCartRequest(item.Id, 10);
        var result = await cartItemService.AddItemToCart(request);
        Assert.Equal(AddItemToCartResult.DatabaseError, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
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
            UserId = userId,
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var request = new UpdateQuantityItemInCartRequest(item.Id, 1);
        var result = await cartItemService.UpdateQuantityItem(request);
        Assert.Equal(UpdateQuantityItemInCartResult.Success, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var request = new UpdateQuantityItemInCartRequest(item.Id, 1);
        var result = await cartItemService.UpdateQuantityItem(request);
        Assert.Equal(UpdateQuantityItemInCartResult.TokenInvalid, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var request = new UpdateQuantityItemInCartRequest(item.Id, 1);
        var result = await cartItemService.UpdateQuantityItem(request);
        Assert.Equal(UpdateQuantityItemInCartResult.UserNotFound, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var request = new UpdateQuantityItemInCartRequest(item.Id, 1);
        var result = await cartItemService.UpdateQuantityItem(request);
        Assert.Equal(UpdateQuantityItemInCartResult.CartItemNotFound, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var request = new UpdateQuantityItemInCartRequest(Guid.NewGuid(), 1);
        var result = await cartItemService.UpdateQuantityItem(request);
        Assert.Equal(UpdateQuantityItemInCartResult.ItemNotFound, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var request = new UpdateQuantityItemInCartRequest(item.Id, -1);
        var result = await cartItemService.UpdateQuantityItem(request);
        Assert.Equal(UpdateQuantityItemInCartResult.QuantityInvalid, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 0,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var request = new UpdateQuantityItemInCartRequest(item.Id, 1);
        var result =  await cartItemService.UpdateQuantityItem(request);
        Assert.Equal(UpdateQuantityItemInCartResult.OutOfStock, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var request = new UpdateQuantityItemInCartRequest(item.Id, 100);
        var result =  await cartItemService.UpdateQuantityItem(request);
        Assert.Equal(UpdateQuantityItemInCartResult.InsufficientStock, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
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
        await mockDbContext.Object.Database.EnsureCreatedAsync();
        mockDbContext.Object.Users.Add(user);
        mockDbContext.Object.Shops.Add(shop);
        mockDbContext.Object.Addresses.Add(address);
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, mockDbContext.Object);
        
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Simulated database error"));
        var request = new UpdateQuantityItemInCartRequest(item.Id, 1);
        var result =  await cartItemService.UpdateQuantityItem(request);
        Assert.Equal(UpdateQuantityItemInCartResult.DatabaseError, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var request = new DeleteItemFromCartRequest(item.Id);
        var result =  await cartItemService.DeleteItemFromCart(request);
        Assert.Equal(DeleteItemFromCartResult.Success, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var request = new DeleteItemFromCartRequest(item.Id);
        var result = await cartItemService.DeleteItemFromCart(request);
        Assert.Equal(DeleteItemFromCartResult.TokenInvalid, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var request =  new DeleteItemFromCartRequest(item.Id);
        var result = await cartItemService.DeleteItemFromCart(request);
        Assert.Equal(DeleteItemFromCartResult.UserNotFound, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var request = new DeleteItemFromCartRequest(Guid.NewGuid());
        var result =  await cartItemService.DeleteItemFromCart(request);
        Assert.Equal(DeleteItemFromCartResult.ItemNotFound, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, dbContext);
        var request = new DeleteItemFromCartRequest(item.Id);
        var result =  await cartItemService.DeleteItemFromCart(request);
        Assert.Equal(DeleteItemFromCartResult.CartItemNotFound, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
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
        await mockDbContext.Object.Database.EnsureCreatedAsync();
        mockDbContext.Object.Users.Add(user);
        mockDbContext.Object.Shops.Add(shop);
        mockDbContext.Object.Addresses.Add(address);
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
        var cartItemService = new CartItemService(mockHttpContextAccessor.Object, mockDbContext.Object);
        var request = new DeleteItemFromCartRequest(item.Id);
        var result =  await cartItemService.DeleteItemFromCart(request);
        Assert.Equal(DeleteItemFromCartResult.DatabaseError, result);
    }
}