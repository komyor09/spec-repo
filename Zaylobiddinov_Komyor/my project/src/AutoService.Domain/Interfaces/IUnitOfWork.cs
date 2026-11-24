using AutoService.Domain.Entities;

namespace AutoService.Domain.Interfaces;

/// <summary>
/// Единица работы: набор репозиториев с общим сохранением.
/// Одна бизнес-операция = один IUnitOfWork = одна транзакция.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IClientRepository Clients { get; }
    IRepairRequestRepository Requests { get; }
    IRepository<Employee> Employees { get; }
    IRepository<Service> Services { get; }
    IRepository<SparePart> SpareParts { get; }

    void SaveChanges();
}

/// <summary>Создаёт новую единицу работы на каждую операцию.</summary>
public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();
}
