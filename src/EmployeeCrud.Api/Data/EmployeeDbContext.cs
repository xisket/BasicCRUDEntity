using EmployeeCrud.Api.Employees;
using Microsoft.EntityFrameworkCore;

namespace EmployeeCrud.Api.Data;

public sealed class EmployeeDbContext(DbContextOptions<EmployeeDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var employee = modelBuilder.Entity<Employee>();
        employee.Property(e => e.Name).HasMaxLength(100).IsRequired();
        employee.Property(e => e.Email).HasMaxLength(254).IsRequired();
        employee.Property(e => e.Department).HasMaxLength(100).IsRequired();
        employee.HasIndex(e => e.Email).IsUnique();
    }
}
