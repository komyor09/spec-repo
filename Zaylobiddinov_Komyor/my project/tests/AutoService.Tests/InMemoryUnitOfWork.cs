using System.Reflection;
using AutoService.Domain.Common;
using AutoService.Domain.Entities;
using AutoService.Domain.Enums;
using AutoService.Domain.Interfaces;

namespace AutoService.Tests;

/// <summary>
/// Хранилище в памяти для тестов сервисов без базы данных.
/// Показывает пользу интерфейсов: сервисы работают с ним так же, как с EF Core.
/// </summary>
internal sealed class InMemoryUnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly InMemoryStore _store = new();
    public IUnitOfWork Create() => new InMemoryUnitOfWork(_store);
}

internal sealed class InMemoryStore
{
    public List<Client> Clients { get; } = new();
    public List<RepairRequest> Requests { get; } = new();
    public List<Employee> Employees { get; } = new();
    public List<Service> Services { get; } = new();
    public List<SparePart> SpareParts { get; } = new();
    public int LastId;
}

internal sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private static readonly PropertyInfo IdProperty = typeof(Entity).GetProperty(nameof(Entity.Id))!;
    private readonly InMemoryStore _store;

    public InMemoryUnitOfWork(InMemoryStore store)
    {
        _store = store;
        Clients = new ClientRepo(store.Clients);
        Requests = new RequestRepo(store.Requests);
        Employees = new Repo<Employee>(store.Employees);
        Services = new Repo<Service>(store.Services);
        SpareParts = new Repo<SparePart>(store.SpareParts);
    }

    public IClientRepository Clients { get; }
    public IRepairRequestRepository Requests { get; }
    public IRepository<Employee> Employees { get; }
    public IRepository<Service> Services { get; }
    public IRepository<SparePart> SpareParts { get; }

    /// <summary>Назначает идентификаторы новым объектам — как это сделала бы база данных.</summary>
    public void SaveChanges()
    {
        IEnumerable<Entity> all = _store.Clients
            .Concat<Entity>(_store.Clients.SelectMany(c => c.Cars))
            .Concat(_store.Employees).Concat(_store.Services).Concat(_store.SpareParts)
            .Concat(_store.Requests)
            .Concat(_store.Requests.SelectMany(r => r.Works))
            .Concat(_store.Requests.SelectMany(r => r.Parts))
            .Concat(_store.Requests.Where(r => r.Payment != null).Select(r => r.Payment!));

        foreach (var e in all.Where(e => e.Id == 0))
            IdProperty.SetValue(e, ++_store.LastId);
    }

    public void Dispose() { }

    private class Repo<T> : IRepository<T> where T : Entity
    {
        protected readonly List<T> Items;
        public Repo(List<T> items) => Items = items;
        public T? GetById(int id) => Items.FirstOrDefault(x => x.Id == id);
        public IReadOnlyList<T> GetAll() => Items.ToList();
        public void Add(T entity) => Items.Add(entity);
        public void Remove(T entity) => Items.Remove(entity);
    }

    private sealed class ClientRepo(List<Client> items) : Repo<Client>(items), IClientRepository
    {
        public IReadOnlyList<Client> Search(string text) =>
            Items.Where(c => c.FullName.Contains(text, StringComparison.OrdinalIgnoreCase) || c.Phone.Contains(text)).ToList();

        public bool PlateExists(string plateNumber) => Items.SelectMany(c => c.Cars).Any(c => c.PlateNumber == plateNumber);
    }

    private sealed class RequestRepo(List<RepairRequest> items) : Repo<RepairRequest>(items), IRepairRequestRepository
    {
        public IReadOnlyList<RepairRequest> Search(RequestFilter f) =>
            Items.Where(r => f.Status == null || r.Status == f.Status)
                 .Where(r => !f.HideClosed || !r.IsClosed)
                 .ToList();

        public int CountCreatedOn(DateTime day) => Items.Count(r => r.CreatedAt.Date == day.Date);

        public int CountIssuedForClient(int clientId) =>
            Items.Count(r => r.ClientId == clientId && r.Status == RequestStatus.Issued);

        public IReadOnlyList<RepairRequest> GetIssuedBetween(DateTime from, DateTime to) =>
            Items.Where(r => r.IssuedAt >= from && r.IssuedAt < to).ToList();

        public IReadOnlyList<RepairRequest> GetByCar(int carId) => Items.Where(r => r.CarId == carId).ToList();
    }
}
