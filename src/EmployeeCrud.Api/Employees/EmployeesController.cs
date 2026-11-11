using System.ComponentModel.DataAnnotations;
using EmployeeCrud.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EmployeeCrud.Api.Employees;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController(EmployeeDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<EmployeePage>> List(
        [Range(1, 1_000_000)] int page = 1,
        [Range(1, 100)] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = db.Employees.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(e => e.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new EmployeeResponse(e.Id, e.Name, e.Email, e.Department))
            .ToListAsync(cancellationToken);
        return Ok(new EmployeePage(items, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var employee = await db.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        return employee is null ? NotFound() : Ok(ToResponse(employee));
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeResponse>> Create(EmployeeRequest request, CancellationToken cancellationToken)
    {
        var employee = new Employee
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Department = request.Department.Trim()
        };
        db.Employees.Add(employee);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateEmail(exception))
        {
            return Problem(statusCode: 409, title: "An employee with this email already exists.");
        }
        return CreatedAtAction(nameof(Get), new { id = employee.Id }, ToResponse(employee));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EmployeeRequest request, CancellationToken cancellationToken)
    {
        var employee = await db.Employees.FindAsync([id], cancellationToken);
        if (employee is null) return NotFound();
        employee.Name = request.Name.Trim();
        employee.Email = request.Email.Trim().ToLowerInvariant();
        employee.Department = request.Department.Trim();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateEmail(exception))
        {
            return Problem(statusCode: 409, title: "An employee with this email already exists.");
        }
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var employee = await db.Employees.FindAsync([id], cancellationToken);
        if (employee is null) return NotFound();
        db.Employees.Remove(employee);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static EmployeeResponse ToResponse(Employee employee) =>
        new(employee.Id, employee.Name, employee.Email, employee.Department);

    // The database constraint also handles two concurrent requests with the same email.
    private static bool IsDuplicateEmail(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 };
}
