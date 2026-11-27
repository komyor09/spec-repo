using System.Globalization;
using Microsoft.Data.Sqlite;

namespace PrintShop.Core.Data;

/// <summary>
/// Сеанс работы с БД: открытое подключение и (необязательно) транзакция.
/// Позволяет нескольким репозиториям работать в одной транзакции.
/// </summary>
public sealed class DbSession : IDisposable
{
    internal DbSession(SqliteConnection connection, bool withTransaction)
    {
        Connection = connection;
        Transaction = withTransaction ? connection.BeginTransaction() : null;
    }

    public SqliteConnection Connection { get; }
    public SqliteTransaction? Transaction { get; }

    public SqliteCommand Command(string sql, params (string Name, object? Value)[] parameters)
    {
        var cmd = Connection.CreateCommand();
        cmd.Transaction = Transaction;
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }

    public int Execute(string sql, params (string, object?)[] parameters)
    {
        using var cmd = Command(sql, parameters);
        return cmd.ExecuteNonQuery();
    }

    public object? Scalar(string sql, params (string, object?)[] parameters)
    {
        using var cmd = Command(sql, parameters);
        return cmd.ExecuteScalar();
    }

    public long LastInsertId() => (long)Scalar("SELECT last_insert_rowid();")!;

    public void Commit() => Transaction?.Commit();

    public void Dispose()
    {
        Transaction?.Dispose();
        Connection.Dispose();
    }
}

/// <summary>
/// Подключение к базе данных SQLite и создание таблиц при первом запуске.
/// </summary>
public class DatabaseContext
{
    public const string DateFormat = "yyyy-MM-dd HH:mm:ss";

    public DatabaseContext(string databasePath)
    {
        DatabasePath = Path.GetFullPath(databasePath);
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            ForeignKeys = true,
            Pooling = false // чтобы файл БД освобождался (нужно для резервного копирования)
        }.ToString();
    }

    public string DatabasePath { get; }
    public string ConnectionString { get; }

    /// <summary>true, если при инициализации база была создана заново.</summary>
    public bool WasCreated { get; private set; }

    /// <summary>Открывает новый сеанс (подключение) с транзакцией или без.</summary>
    public DbSession Open(bool withTransaction = false)
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        return new DbSession(conn, withTransaction);
    }

    /// <summary>Выполняет действие в транзакции: при исключении изменения откатываются.</summary>
    public void InTransaction(Action<DbSession> action)
    {
        using var s = Open(withTransaction: true);
        action(s);
        s.Commit();
    }

    public T InTransaction<T>(Func<DbSession, T> func)
    {
        using var s = Open(withTransaction: true);
        var result = func(s);
        s.Commit();
        return result;
    }

    /// <summary>Создаёт файл БД и таблицы, если их ещё нет.</summary>
    public void Initialize()
    {
        var dir = Path.GetDirectoryName(DatabasePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        WasCreated = !File.Exists(DatabasePath);

        using var s = Open(withTransaction: true);
        s.Execute(Schema);
        s.Commit();
    }

    // ---- Вспомогательные преобразования ----
    public static string ToDb(DateTime d) => d.ToString(DateFormat, CultureInfo.InvariantCulture);
    public static DateTime FromDb(string s) => DateTime.ParseExact(s, DateFormat, CultureInfo.InvariantCulture);
    public static decimal Dec(object v) => Convert.ToDecimal(v, CultureInfo.InvariantCulture);

    private const string Schema = """
        CREATE TABLE IF NOT EXISTS Users (
            Id           INTEGER PRIMARY KEY AUTOINCREMENT,
            Login        TEXT NOT NULL UNIQUE,
            PasswordHash TEXT NOT NULL,
            FullName     TEXT NOT NULL,
            Role         INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Customers (
            Id        INTEGER PRIMARY KEY AUTOINCREMENT,
            Type      INTEGER NOT NULL,
            Name      TEXT NOT NULL,
            Phone     TEXT NOT NULL,
            Email     TEXT,
            Address   TEXT,
            CreatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS ProductTypes (
            Id        INTEGER PRIMARY KEY AUTOINCREMENT,
            Name      TEXT NOT NULL,
            Kind      INTEGER NOT NULL,
            BasePrice REAL NOT NULL CHECK (BasePrice >= 0),
            Unit      TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Materials (
            Id          INTEGER PRIMARY KEY AUTOINCREMENT,
            Name        TEXT NOT NULL,
            Density     INTEGER NOT NULL,
            Format      TEXT NOT NULL,
            Price       REAL NOT NULL CHECK (Price >= 0),
            Quantity    REAL NOT NULL CHECK (Quantity >= 0),
            MinQuantity REAL NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS Services (
            Id         INTEGER PRIMARY KEY AUTOINCREMENT,
            Name       TEXT NOT NULL,
            Price      REAL NOT NULL CHECK (Price >= 0),
            ChargeType INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS Orders (
            Id         INTEGER PRIMARY KEY AUTOINCREMENT,
            Number     TEXT NOT NULL UNIQUE,
            CustomerId INTEGER NOT NULL REFERENCES Customers(Id),
            ManagerId  INTEGER REFERENCES Users(Id) ON DELETE SET NULL,
            CreatedAt  TEXT NOT NULL,
            DueDate    TEXT NOT NULL,
            Status     INTEGER NOT NULL,
            TotalPrice REAL NOT NULL DEFAULT 0,
            Discount   REAL NOT NULL DEFAULT 0,
            LayoutPath TEXT,
            Comment    TEXT
        );

        CREATE TABLE IF NOT EXISTS OrderItems (
            Id            INTEGER PRIMARY KEY AUTOINCREMENT,
            OrderId       INTEGER NOT NULL REFERENCES Orders(Id) ON DELETE CASCADE,
            ProductTypeId INTEGER NOT NULL REFERENCES ProductTypes(Id),
            MaterialId    INTEGER NOT NULL REFERENCES Materials(Id),
            Quantity      INTEGER NOT NULL CHECK (Quantity >= 1),
            Format        TEXT NOT NULL,
            Color         INTEGER NOT NULL,
            Params        TEXT,
            Price         REAL NOT NULL DEFAULT 0,
            Discount      REAL NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS OrderItemServices (
            OrderItemId INTEGER NOT NULL REFERENCES OrderItems(Id) ON DELETE CASCADE,
            ServiceId   INTEGER NOT NULL REFERENCES Services(Id),
            PRIMARY KEY (OrderItemId, ServiceId)
        );

        CREATE TABLE IF NOT EXISTS Payments (
            Id          INTEGER PRIMARY KEY AUTOINCREMENT,
            OrderId     INTEGER NOT NULL REFERENCES Orders(Id) ON DELETE CASCADE,
            Amount      REAL NOT NULL CHECK (Amount > 0),
            PaymentDate TEXT NOT NULL,
            PaymentType INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS StatusHistory (
            Id        INTEGER PRIMARY KEY AUTOINCREMENT,
            OrderId   INTEGER NOT NULL REFERENCES Orders(Id) ON DELETE CASCADE,
            Status    INTEGER NOT NULL,
            ChangedAt TEXT NOT NULL,
            UserId    INTEGER REFERENCES Users(Id) ON DELETE SET NULL
        );

        CREATE INDEX IF NOT EXISTS IX_Orders_Customer ON Orders(CustomerId);
        CREATE INDEX IF NOT EXISTS IX_OrderItems_Order ON OrderItems(OrderId);
        CREATE INDEX IF NOT EXISTS IX_Payments_Order ON Payments(OrderId);
        CREATE INDEX IF NOT EXISTS IX_StatusHistory_Order ON StatusHistory(OrderId);
        """;
}
