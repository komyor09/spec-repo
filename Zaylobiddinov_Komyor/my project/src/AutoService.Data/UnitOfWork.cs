using AutoService.Data.Repositories;
using AutoService.Domain.Entities;
using AutoService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoService.Data;

/// <summary>
/// Единица работы на EF Core: один DbContext на одну бизнес-операцию.
/// Все репозитории используют общий контекст, SaveChanges сохраняет изменения одной транзакцией.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        Clients = new ClientRepository(db);
        Requests = new RepairRequestRepository(db);
        Employees = new Repository<Employee>(db);
        Services = new Repository<Service>(db);
        SpareParts = new Repository<SparePart>(db);
    }

    public IClientRepository Clients { get; }
    public IRepairRequestRepository Requests { get; }
    public IRepository<Employee> Employees { get; }
    public IRepository<Service> Services { get; }
    public IRepository<SparePart> SpareParts { get; }

    public void SaveChanges() => _db.SaveChanges();

    public void Dispose() => _db.Dispose();
}

public sealed class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public UnitOfWorkFactory(IDbContextFactory<AppDbContext> contextFactory) => _contextFactory = contextFactory;

    public IUnitOfWork Create() => new UnitOfWork(_contextFactory.CreateDbContext());
}
