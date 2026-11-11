using System.ComponentModel.DataAnnotations;

namespace EmployeeCrud.Api.Employees;

public sealed record EmployeeRequest(
    [Required, StringLength(100)] string Name,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(100)] string Department);

public sealed record EmployeeResponse(int Id, string Name, string Email, string Department);
public sealed record EmployeePage(IReadOnlyList<EmployeeResponse> Items, int Page, int PageSize, int Total);
