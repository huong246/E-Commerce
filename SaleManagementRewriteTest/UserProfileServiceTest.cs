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

public class UserProfileServiceTest
{
    [Fact]
    public async Task GetUserProfileAsync_WhenTokenValid_ReturnsUserProfile()
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object,
            dbContext);
        var result = await userProfileService.GetUserProfileAsync();
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal(user.Username, result.Username);
        Assert.Equal(user.FullName, result.Fullname);
    }

    [Fact]
    public async Task GetUserProfileAsync_WhenTokenInvalid_ReturnsNull()
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
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object,
            dbContext);
        var result = await userProfileService.GetUserProfileAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenTokenValid_ReturnsUserProfile()
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
            Password =BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
            PhoneNumber = "0888888888",
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
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object,
            dbContext);
        var request = new UpdateUserProfileRequest("TestFullName", "TestEmail...", "123456788", new DateTime (1999, 1, 1), "male");
        var result = await userProfileService.UpdateUserProfileAsync(request);
        Assert.Equal(UpdateUserProfileResult.Success, result);
        var userUpdate =  await dbContext.Users.FindAsync(userId);
        Assert.NotNull(userUpdate);
        Assert.Equal(request.Fullname, userUpdate.FullName);
        Assert.Equal(request.Email, userUpdate.Email);
        Assert.Equal(request.PhoneNumber, userUpdate.PhoneNumber);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenTokenInvalid_ReturnsNull()
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier,"null")};
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object,
            dbContext);
        var request = new UpdateUserProfileRequest("TestFullName", "TestEmail...", "123456788", new DateTime (1999, 1, 1), "male");
        var result = await userProfileService.UpdateUserProfileAsync(request);
        Assert.Equal(UpdateUserProfileResult.TokenInvalid, result);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenUserNotFound_ReturnsUserNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object,
            dbContext);
        var request = new UpdateUserProfileRequest("TestFullName", "TestEmail...", "123456788", new DateTime (1999, 1, 1), "male");
        var result = await userProfileService.UpdateUserProfileAsync(request);
        Assert.Equal(UpdateUserProfileResult.UserNotFound, result);
    }
    
    [Fact]
    public async Task UpdateUserProfileAsync_WhenDuplicateValue_ReturnsDuplicateValue()
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
            Email = "12345678",
            Birthday = new DateTime(1999, 1, 1),
            Gender = "male",
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
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object,
            dbContext);
        var request = new UpdateUserProfileRequest("John Doe", "12345678", "0888888888", new DateTime (1999, 1, 1), "male");
        var result = await userProfileService.UpdateUserProfileAsync(request);
        Assert.Equal(UpdateUserProfileResult.DuplicateValue, result);
    }
    
    [Fact]
    public async Task UpdateUserProfileAsync_WhenDatabaseError_ReturnsDatabaseError()
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        var mockDbContext = new Mock<ApiDbContext>(options);
        
        var userId = Guid.NewGuid();
        var user = new User()
         {
             Id = userId,
             Username = "123234567",
             Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
             FullName = "John Doe",
             PhoneNumber = "0888888888",
             Email = "12345678",
             Birthday = new DateTime(1999, 1, 1),
             Gender = "male",
         };
        var request = new UpdateUserProfileRequest("John Doe", "12345678000", "0888888888", new DateTime (1999, 1, 1), "male");
        mockDbContext.Setup(db => db.Users).ReturnsDbSet(new List<User> { user });
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
             .ThrowsAsync(new DbUpdateException("Simulated database error"));
        var userProfileService = new UserProfileService( mockHttpContextAccessor.Object, mockDbContext.Object);
        var result = await userProfileService.UpdateUserProfileAsync(request);
        Assert.Equal(UpdateUserProfileResult.DatabaseError, result);
        
    }

    [Fact]
    public async Task UpdatePasswordAsync_WhenRequestValid_ReturnsSuccess()
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
            Email = "12345678",
            Birthday = new DateTime(1999, 1, 1),
            Gender = "male",
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
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object,
            dbContext);
        var request = new UpdatePasswordRequest("123456789", "0909876462");
        var result = await userProfileService.UpdatePasswordAsync(request);
        Assert.Equal(UpdatePasswordResult.Success, result);
    }

    [Fact]
    public async Task UpdatePasswordAsync_WhenTokenInvalid_ReturnsTokenInvalid()
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
            Email = "12345678",
            Birthday = new DateTime(1999, 1, 1),
            Gender = "male",
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "null") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object,
            dbContext);
        var request = new UpdatePasswordRequest("123456789", "0909876462");
        var result = await userProfileService.UpdatePasswordAsync(request);
        Assert.Equal(UpdatePasswordResult.TokenInvalid, result);
    }

    [Fact]
    public async Task UpdatePasswordAsync_WhenUserNotFound_ReturnsNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object,
            dbContext);
        var request = new UpdatePasswordRequest("123456789", "0909876462");
        var result = await userProfileService.UpdatePasswordAsync(request);
        Assert.Equal(UpdatePasswordResult.UserNotFound, result);
    }

    [Fact]
    public async Task UpdatePasswordAsync_WhenOldPasswordWrong_returnsOldPasswordWrong()
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
            Email = "12345678",
            Birthday = new DateTime(1999, 1, 1),
            Gender = "male",
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
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object,
            dbContext);
        var request = new UpdatePasswordRequest("123456789101", "0909876462");
        var result = await userProfileService.UpdatePasswordAsync(request);
        Assert.Equal(UpdatePasswordResult.OldPasswordWrong, result);
    }

    [Fact]
    public async Task UpdatePasswordAsync_WhenDuplicateValue_returnsDuplicateValue()
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
            Email = "12345678",
            Birthday = new DateTime(1999, 1, 1),
            Gender = "male",
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
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object,
            dbContext);
        var request = new UpdatePasswordRequest("123456789", "123456789");
        var result = await userProfileService.UpdatePasswordAsync(request);
        Assert.Equal(UpdatePasswordResult.DuplicateValue, result);
    }

    [Fact]
    public async Task UpdatePasswordAsync_WhenDatabaseError_ReturnsDatabaseError()
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        var mockDbContext = new Mock<ApiDbContext>(options);
        
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Email = "12345678",
            Birthday = new DateTime(1999, 1, 1),
            Gender = "male",
        };
        
        var request = new UpdatePasswordRequest("123456789", "123456455789");
        mockDbContext.Setup(db => db.Users).ReturnsDbSet(new List<User> { user });
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Simulated database error"));
        var userProfileService = new UserProfileService( mockHttpContextAccessor.Object, mockDbContext.Object);
        var result = await userProfileService.UpdatePasswordAsync(request);
        Assert.Equal(UpdatePasswordResult.DatabaseError, result);
    }
    
}