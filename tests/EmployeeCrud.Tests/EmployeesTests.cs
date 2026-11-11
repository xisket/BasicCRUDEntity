using System.Net;
using System.Net.Http.Json;
using EmployeeCrud.Api.Employees;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace EmployeeCrud.Tests;

public sealed class EmployeesTests
{
    [Fact]
    public async Task Swagger_ui_points_to_the_generated_employee_contract()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync("/swagger/index.html");
        Assert.Contains("swagger-ui", html);
        var configuration = await client.GetStringAsync("/swagger/index.js");
        Assert.Contains("/openapi/v1.json", configuration);
        var document = await client.GetStringAsync("/openapi/v1.json");
        Assert.Contains("/api/employees", document);
    }

    [Fact]
    public async Task Crud_round_trip()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        //CREATE
        var created = await client.PostAsJsonAsync("/api/employees", new EmployeeRequest("Ana", "ANA@example.com", "HR"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        //READ
        var employee = await client.GetFromJsonAsync<EmployeeResponse>(created.Headers.Location);
        Assert.Equal("ana@example.com", employee!.Email);
        //UPDATE
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(created.Headers.Location,
            new EmployeeRequest("Ana Perez", "ana@example.com", "People"))).StatusCode);
        employee = await client.GetFromJsonAsync<EmployeeResponse>(created.Headers.Location);
        Assert.Equal("Ana Perez", employee!.Name);
        var page = await client.GetFromJsonAsync<EmployeePage>("/api/employees?pageSize=1");
        Assert.Equal(1, page!.Total);
        Assert.Single(page.Items);
        //DELETE
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(created.Headers.Location)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(created.Headers.Location)).StatusCode);
    }

    [Theory]
    [InlineData("", "ana@example.com", "HR")]
    [InlineData("   ", "ana@example.com", "HR")]
    [InlineData("Ana", "invalid", "HR")]
    [InlineData("Ana", "ana@example.com", "")]
    public async Task Invalid_input_returns_problem(string name, string email, string department)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/employees", new EmployeeRequest(name, email, department));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Duplicate_email_is_rejected_on_create_and_update()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/employees", new EmployeeRequest("Ana", "ana@example.com", "HR"));
        var duplicate = await client.PostAsJsonAsync("/api/employees", new EmployeeRequest("Other", "ANA@example.com", "HR"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var second = await client.PostAsJsonAsync("/api/employees", new EmployeeRequest("Luis", "luis@example.com", "HR"));
        var update = await client.PutAsJsonAsync(second.Headers.Location, new EmployeeRequest("Luis", "ana@example.com", "HR"));
        Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
    }

    [Fact]
    public async Task Missing_records_and_invalid_pagination_return_client_errors()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/employees/999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/employees/999", new EmployeeRequest("Ana", "ana@example.com", "HR"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/employees/999")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/employees?pageSize=101")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/employees?page=0")).StatusCode);
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"employees-{Guid.NewGuid():N}.db");
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Employees"] = $"Data Source={databasePath};Pooling=False" }));
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                foreach (var suffix in new[] { "", "-wal", "-shm" }) File.Delete(databasePath + suffix);
        }
    }
}
