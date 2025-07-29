using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SaleManagement.Services;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Services;

var builder = WebApplication.CreateBuilder(args); //trình xây dựng ứng dụng web
var configuration = builder.Configuration; //đối tượng cấu hinh cho phep truy cap vao trong file
//tao duong noi den co so du lieu
//var dbPath = Path.Combine(builder.Environment.ContentRootPath, "SaleManagementRewrite.db");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//dang ky ApiDbContext tu duong noi den co so du lieu duoc tao o tren
builder.Services.AddDbContext<ApiDbContext>(options => options.UseSqlite(connectionString)); 

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

//tao nut authorization tren moi header cua request
builder.Services.AddSwaggerGen(options =>
{
    //dinh nghia cau hinh cua nut authorization
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    //dinh nghia chi can xac thuc token 1 lan o bat ky request nao thi tat ca cac request deu duoc xac thuc
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            [] //o vua tao la o trong
        }
    });
});

//Phan xac thuc nguoi dung bang bearer Token tuong ung
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidAudience = configuration["JWT:ValidAudience"],
        ValidIssuer = configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? string.Empty)),
        RoleClaimType = ClaimTypes.Role,
    };

});
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IItemImageService, ItemImageService>();
builder.Services.AddScoped<ICartItemService, CartItemService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICustomerUpSellerService, CustomerUpSellerService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddHostedService<CompleteOrderService>();
builder.Services.AddHostedService<VoucherUpdateStatusService>();
var app = builder.Build();

// ham tu dong cap nhat migration de khong can chay lenh dotnet ef database update thu cong
//nghia la khi them moi vao entity thi van can chay ham dotnet ef migrations add... truoc

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApiDbContext>();
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception e)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(e.Message);
    }
}
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); //dung cho itemImage
app.UseAuthentication();
app.UseAuthorization(); //Phan quyen
app.UseMiddleware<JwtBlacklistMiddleware>();
app.MapControllers(); // dua yeu cau toi controller tuong ung
app.MapHub<SaleManagementRewrite.Hubs.NotificationHubs>("/NotificationHubs");
app.MapHub<SaleManagementRewrite.Hubs.ChatHubs>("/ChatHubs");
app.Run();