using Microsoft.Data.Sqlite;
using PrintShop.Core.Models;

namespace PrintShop.Core.Data;

/// <summary>Обобщённый интерфейс репозитория: операции CRUD.</summary>
public interface IRepository<T> where T : EntityBase
{
    IReadOnlyList<T> GetAll();
    T? GetById(int id);
    void Add(T entity);
    void Update(T entity);
    void Delete(int id);
}

/// <summary>
/// Абстрактный базовый репозиторий (шаблонный метод): общие GetAll/GetById/Delete,
/// а чтение строки (Map) и запись (Insert/UpdateRow) реализуют наследники.
/// </summary>
public abstract class RepositoryBase<T> : IRepository<T> where T : EntityBase
{
    protected RepositoryBase(DatabaseContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    protected DatabaseContext Context { get; }

    /// <summary>Имя таблицы в БД.</summary>
    protected abstract string TableName { get; }

    /// <summary>Порядок сортировки по умолчанию.</summary>
    protected virtual string OrderBy => "Id";

    /// <summary>Создание объекта из строки результата запроса.</summary>
    protected abstract T Map(SqliteDataReader r);

    protected abstract void Insert(T entity, DbSession s);
    protected abstract void UpdateRow(T entity, DbSession s);

    public virtual IReadOnlyList<T> GetAll()
    {
        using var s = Context.Open();
        return Query(s, $"SELECT * FROM {TableName} ORDER BY {OrderBy}");
    }

    public virtual T? GetById(int id)
    {
        using var s = Context.Open();
        return Query(s, $"SELECT * FROM {TableName} WHERE Id = $id", ("$id", id)).FirstOrDefault();
    }

    public void Add(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Context.InTransaction(s => Add(entity, s));
    }

    public void Add(T entity, DbSession s)
    {
        Insert(entity, s);
        if (entity.Id == 0) entity.Id = (int)s.LastInsertId();
    }

    public void Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity.IsNew) throw new OrderValidationException("Нельзя обновить запись, которая ещё не сохранена.");
        Context.InTransaction(s => UpdateRow(entity, s));
    }

    public void Update(T entity, DbSession s) => UpdateRow(entity, s);

    public virtual void Delete(int id)
    {
        try
        {
            Context.InTransaction(s => s.Execute($"DELETE FROM {TableName} WHERE Id = $id", ("$id", id)));
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // FOREIGN KEY constraint failed
        {
            throw new OrderValidationException("Запись нельзя удалить: она используется в других данных (например, в заказах).", ex);
        }
    }

    protected List<T> Query(DbSession s, string sql, params (string, object?)[] parameters)
    {
        using var cmd = s.Command(sql, parameters);
        using var r = cmd.ExecuteReader();
        var list = new List<T>();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    // Удобные методы чтения
    protected static string Str(SqliteDataReader r, string col) =>
        r[col] is DBNull ? string.Empty : Convert.ToString(r[col])!;
    protected static int Int(SqliteDataReader r, string col) => Convert.ToInt32(r[col]);
    protected static decimal Dec(SqliteDataReader r, string col) => DatabaseContext.Dec(r[col]);
    protected static DateTime Date(SqliteDataReader r, string col) => DatabaseContext.FromDb(Str(r, col));
}
