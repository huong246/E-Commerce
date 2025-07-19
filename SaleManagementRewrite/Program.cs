var builder = WebApplication.CreateBuilder(args); //trình xây dựng ứng dụng web
var configuration = builder.Configuration; //đối tượng cấu hinh cho phep truy cap vao trong file
//tao duong noi den co so du lieu
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "SaleManagementRew")

builder.Services.AddControllers();

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();