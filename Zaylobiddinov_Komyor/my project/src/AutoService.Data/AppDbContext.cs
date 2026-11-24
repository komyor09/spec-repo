using AutoService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoService.Data;

/// <summary>Контекст базы данных SQLite. Настройки таблиц — в папке Configurations.</summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<SparePart> SpareParts => Set<SparePart>();
    public DbSet<RepairRequest> RepairRequests => Set<RepairRequest>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // подключает все классы IEntityTypeConfiguration<T> из этой сборки
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
