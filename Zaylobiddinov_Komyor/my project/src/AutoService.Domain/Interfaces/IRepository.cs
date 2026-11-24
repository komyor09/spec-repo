using AutoService.Domain.Common;

namespace AutoService.Domain.Interfaces;

/// <summary>Обобщённый репозиторий: хранилище объектов одного типа.</summary>
public interface IRepository<T> where T : Entity
{
    T? GetById(int id);
    IReadOnlyList<T> GetAll();
    void Add(T entity);
    void Remove(T entity);
}
