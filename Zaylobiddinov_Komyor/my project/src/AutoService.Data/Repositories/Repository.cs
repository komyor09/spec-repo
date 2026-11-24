using AutoService.Domain.Common;
using AutoService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoService.Data.Repositories;

/// <summary>
/// Базовая реализация репозитория на EF Core.
/// Наследники переопределяют Query, чтобы подгрузить связанные данные (Include).
/// </summary>
public class Repository<T> : IRepository<T> where T : Entity
{
    protected readonly AppDbContext Db;

    public Repository(AppDbContext db) => Db = db;

    protected virtual IQueryable<T> Query => Db.Set<T>();

    public virtual T? GetById(int id) => Query.FirstOrDefault(e => e.Id == id);

    public virtual IReadOnlyList<T> GetAll() => Query.ToList();

    public void Add(T entity) => Db.Set<T>().Add(entity);

    public void Remove(T entity) => Db.Set<T>().Remove(entity);
}
