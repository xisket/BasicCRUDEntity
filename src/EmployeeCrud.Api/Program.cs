using EmployeeCrud.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<EmployeeDbContext>(options => options.UseSqlite(
    builder.Configuration.GetConnectionString("Employees")
    ?? $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "employees.db")}"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "Employee CRUD v1"));
}

app.UseExceptionHandler();
app.UseStatusCodePages();
 
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapControllers();

app.Run();

public partial class Program;
