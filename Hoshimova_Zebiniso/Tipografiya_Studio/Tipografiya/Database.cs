using System.IO;
using Microsoft.Data.Sqlite;

namespace Tipografiya;

internal static class Database
{
    private static string DirectoryPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Tipografiya");
    private static readonly string OriginalPath = Path.Combine(DirectoryPath, "tipografiya.db");
    private static readonly string StudioPath = Path.Combine(DirectoryPath, "tipografiya-studio.db");
    private static string databasePath = OriginalPath;

    private static readonly (string Table, string[] Columns)[] RequiredColumns =
    [
        ("State", ["Name", "Version"]),
        ("Clients", ["Id", "Name", "Phone"]),
        ("Orders", ["Id", "ClientId", "Created", "Due", "Comment"]),
        ("OrderLines", ["OrderId", "Position", "TariffId", "TariffName", "TariffUnit",
            "Rate", "Quantity", "Product", "Units", "Binding"]),
        ("OrderHistory", ["OrderId", "Position", "Time", "Status", "Reason"]),
        ("Tariffs", ["Id", "Name", "Unit", "Rate", "Position"])
    ];

    internal static bool IsStorageError(Exception ex) =>
        ex is SqliteException or IOException or UnauthorizedAccessException
            or InvalidDataException or System.Text.Json.JsonException;

    private static SqliteConnection Open()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true
        }.ToString());

        try
        {
            connection.Open();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static bool HasCompatibleColumns(string path)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString());
        connection.Open();

        foreach (var (table, required) in RequiredColumns)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT type FROM sqlite_schema WHERE name = $table COLLATE NOCASE";
            command.Parameters.AddWithValue("$table", table);
            var objectType = command.ExecuteScalar();
            if (objectType == null)
                continue;
            if (objectType is not "table")
                return false;

            command.CommandText = "SELECT name FROM pragma_table_info($table)";
            using var reader = command.ExecuteReader();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
                names.Add(reader.GetString(0));

            if (!required.All(names.Contains))
                return false;
        }

        return true;
    }

    private static SqliteCommand Command(SqliteTransaction tx, string sql,
        params (string Name, object Value)[] parameters)
    {
        var command = tx.Connection!.CreateCommand();
        command.Transaction = tx;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
        return command;
    }

    private static int Execute(SqliteTransaction tx, string sql,
        params (string Name, object Value)[] parameters)
    {
        using var command = Command(tx, sql, parameters);
        return command.ExecuteNonQuery();
    }

    internal static bool Initialize()
    {
        Directory.CreateDirectory(DirectoryPath);
        bool studioExists = File.Exists(StudioPath);
        bool useStudio = studioExists || (File.Exists(OriginalPath) && !HasCompatibleColumns(OriginalPath));
        databasePath = useStudio ? StudioPath : OriginalPath;
        if (studioExists && !HasCompatibleColumns(StudioPath))
            throw new InvalidDataException($"Структура файла «{StudioPath}» не подходит для этой версии.");

        using var connection = Open();
        using var tx = connection.BeginTransaction();
        Execute(tx, """
            CREATE TABLE IF NOT EXISTS State (
                Name TEXT PRIMARY KEY, Version INTEGER NOT NULL
            );
            INSERT OR IGNORE INTO State VALUES ('journal', 0);

            CREATE TABLE IF NOT EXISTS Clients (
                Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Phone TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Orders (
                Id INTEGER PRIMARY KEY,
                ClientId INTEGER NOT NULL REFERENCES Clients(Id),
                Created TEXT NOT NULL, Due TEXT NOT NULL, Comment TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS OrderLines (
                OrderId INTEGER NOT NULL REFERENCES Orders(Id) ON DELETE CASCADE,
                Position INTEGER NOT NULL,
                TariffId TEXT NOT NULL, TariffName TEXT NOT NULL,
                TariffUnit INTEGER NOT NULL, Rate TEXT NOT NULL,
                Quantity TEXT NOT NULL, Product INTEGER NOT NULL,
                Units INTEGER NOT NULL, Binding TEXT NOT NULL,
                PRIMARY KEY (OrderId, Position)
            );
            CREATE TABLE IF NOT EXISTS OrderHistory (
                OrderId INTEGER NOT NULL REFERENCES Orders(Id) ON DELETE CASCADE,
                Position INTEGER NOT NULL,
                Time TEXT NOT NULL, Status TEXT NOT NULL, Reason TEXT NOT NULL,
                PRIMARY KEY (OrderId, Position)
            );
            CREATE TABLE IF NOT EXISTS Tariffs (
                Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Unit INTEGER NOT NULL,
                Rate TEXT NOT NULL, Position INTEGER NOT NULL UNIQUE
            );
            """);
        RemoveLegacyDemoOrders(tx);
        tx.Commit();
        return useStudio && !studioExists;
    }

    private static void RemoveLegacyDemoOrders(SqliteTransaction tx)
    {
        (int Id, string Name, string Phone)[] examples =
        [
            (104, "Студия «Наврӯз»", "+992 00 000 04 00"),
            (105, "Книжная мастерская", "+992 00 000 05 00"),
            (106, "Лицей № 1", "+992 00 000 06 00"),
            (107, "Ателье «Садаф»", "+992 00 000 07 00")
        ];
        int removed = 0;
        foreach (var (id, name, phone) in examples)
        {
            // The old example generator backdated the initial history entry to 09:00 on the creation date.
            using var match = Command(tx, """
                SELECT o.ClientId
                FROM Orders o
                JOIN Clients c ON c.Id = o.ClientId
                JOIN OrderHistory h ON h.OrderId = o.Id AND h.Position = 0
                WHERE o.Id = $id AND c.Name = $name AND c.Phone = $phone
                  AND h.Status = 'Принят' AND h.Reason = ''
                  AND datetime(h.Time) = datetime(date(o.Created) || ' 09:00:00')
                """, ("$id", id), ("$name", name), ("$phone", phone));
            if (match.ExecuteScalar() is not long clientId)
                continue;

            Execute(tx, "DELETE FROM OrderLines WHERE OrderId = $id", ("$id", id));
            Execute(tx, "DELETE FROM OrderHistory WHERE OrderId = $id", ("$id", id));
            removed += Execute(tx, "DELETE FROM Orders WHERE Id = $id", ("$id", id));
            Execute(tx, """
                DELETE FROM Clients WHERE Id = $client
                  AND NOT EXISTS (SELECT 1 FROM Orders WHERE ClientId = $client)
                """, ("$client", clientId));
        }

        if (removed > 0)
            Execute(tx, "UPDATE State SET Version = Version + 1 WHERE Name = 'journal'");
    }

    private static long? Version(SqliteTransaction tx, string name)
    {
        using var command = Command(tx,
            "SELECT Version FROM State WHERE Name = $name", ("$name", name));
        return (long?)command.ExecuteScalar();
    }

    private static long AdvanceVersion(SqliteTransaction tx, string name, long version)
    {
        int changed = Execute(tx, """
            UPDATE State SET Version = Version + 1
            WHERE Name = $name AND Version = $version
            """, ("$name", name), ("$version", version));
        if (changed != 1)
            throw new IOException("Данные изменились в другом окне. Нажмите F5 и повторите действие.");
        return version + 1;
    }

    private static void Read(SqliteTransaction tx, string sql, Action<SqliteDataReader> readRow)
    {
        using var command = Command(tx, sql);
        using var reader = command.ExecuteReader();
        while (reader.Read())
            readRow(reader);
    }

    internal static (List<Client> Clients, List<PrintOrder> Orders, long Version) LoadJournal()
    {
        using var connection = Open();
        using var tx = connection.BeginTransaction(deferred: true);
        long version = Version(tx, "journal")
            ?? throw new InvalidDataException("Отсутствует версия журнала заказов.");
        var clients = new Dictionary<int, Client>();
        var orders = new Dictionary<int, PrintOrder>();

        Read(tx, "SELECT Id, Name, Phone FROM Clients", row =>
        {
            int id = row.GetInt32(0);
            clients.Add(id, new Client { Id = id, Name = row.GetString(1), Phone = row.GetString(2) });
        });
        Read(tx, "SELECT Id, ClientId, Created, Due, Comment FROM Orders", row =>
        {
            int id = row.GetInt32(0);
            orders.Add(id, new PrintOrder
            {
                Id = id, Customer = clients[row.GetInt32(1)],
                Created = row.GetDateTime(2), Due = row.GetDateTime(3), Comment = row.GetString(4)
            });
        });
        Read(tx, """
            SELECT OrderId, TariffId, TariffName, TariffUnit, Rate, Quantity, Product, Units, Binding
            FROM OrderLines ORDER BY OrderId, Position
            """, row =>
        {
            var tariff = new Tariff(row.GetString(1), row.GetString(2),
                (PriceUnit)row.GetInt32(3), row.GetDecimal(4));
            orders[row.GetInt32(0)].Lines.Add(new OrderLine(tariff, row.GetDecimal(5),
                (PrintProduct)row.GetInt32(6), row.GetInt32(7), row.GetDecimal(8)));
        });
        Read(tx, "SELECT OrderId, Time, Status, Reason FROM OrderHistory ORDER BY OrderId, Position", row =>
        {
            orders[row.GetInt32(0)].RestoreHistory(
                new HistoryEntry(row.GetDateTime(1), row.GetString(2), row.GetString(3)));
        });

        tx.Commit();
        return (clients.Values.ToList(), orders.Values.ToList(), version);
    }

    internal static long SaveJournal(long version, IEnumerable<Client> clients,
        IEnumerable<PrintOrder> orders)
    {
        using var connection = Open();
        using var tx = connection.BeginTransaction();
        long next = AdvanceVersion(tx, "journal", version);

        foreach (var client in clients)
        {
            Execute(tx, """
                INSERT INTO Clients (Id, Name, Phone) VALUES ($id, $name, $phone)
                ON CONFLICT(Id) DO UPDATE SET Name = excluded.Name, Phone = excluded.Phone
                """, ("$id", client.Id), ("$name", client.Name), ("$phone", client.Phone));
        }

        foreach (var order in orders)
        {
            Execute(tx, """
                INSERT INTO Orders (Id, ClientId, Created, Due, Comment)
                VALUES ($id, $client, $created, $due, $comment)
                ON CONFLICT(Id) DO UPDATE SET
                    ClientId = excluded.ClientId, Due = excluded.Due, Comment = excluded.Comment
                """, ("$id", order.Id), ("$client", order.Customer.Id),
                ("$created", order.Created), ("$due", order.Due), ("$comment", order.Comment));

            Execute(tx, "DELETE FROM OrderLines WHERE OrderId = $id", ("$id", order.Id));
            Execute(tx, "DELETE FROM OrderHistory WHERE OrderId = $id", ("$id", order.Id));

            for (int i = 0; i < order.Lines.Count; i++)
            {
                var line = order.Lines[i];
                Execute(tx, """
                    INSERT INTO OrderLines (OrderId, Position, TariffId, TariffName, TariffUnit,
                        Rate, Quantity, Product, Units, Binding)
                    VALUES ($order, $position, $tariff, $name, $unit, $rate, $quantity, $product, $units, $binding)
                    """, ("$order", order.Id), ("$position", i), ("$tariff", line.Tariff.Id),
                    ("$name", line.Tariff.Name), ("$unit", (int)line.Tariff.Unit),
                    ("$rate", line.Tariff.Rate), ("$quantity", line.Quantity),
                    ("$product", (int)line.Product), ("$units", line.Units), ("$binding", line.Binding));
            }

            for (int i = 0; i < order.History.Count; i++)
            {
                var item = order.History[i];
                Execute(tx, """
                    INSERT INTO OrderHistory (OrderId, Position, Time, Status, Reason)
                    VALUES ($order, $position, $time, $status, $reason)
                    """, ("$order", order.Id), ("$position", i), ("$time", item.Time),
                    ("$status", item.Status), ("$reason", item.Reason));
            }
        }

        tx.Commit();
        return next;
    }

    internal static long DeleteOrder(long version, int id)
    {
        using var connection = Open();
        using var tx = connection.BeginTransaction();
        long next = AdvanceVersion(tx, "journal", version);
        // Explicit cleanup also supports compatible older databases without cascade rules.
        Execute(tx, "DELETE FROM OrderLines WHERE OrderId = $id", ("$id", id));
        Execute(tx, "DELETE FROM OrderHistory WHERE OrderId = $id", ("$id", id));
        if (Execute(tx, "DELETE FROM Orders WHERE Id = $id", ("$id", id)) != 1)
            throw new IOException("Заказ уже удалён. Обновите журнал и повторите действие.");
        tx.Commit();
        return next;
    }

    internal static long DeleteClient(long version, int id)
    {
        using var connection = Open();
        using var tx = connection.BeginTransaction();
        long next = AdvanceVersion(tx, "journal", version);
        using var linked = Command(tx, "SELECT COUNT(*) FROM Orders WHERE ClientId = $id", ("$id", id));
        if (Convert.ToInt64(linked.ExecuteScalar()) > 0)
            throw new InvalidDataException("У клиента есть заказы. Сначала удалите их в журнале заказов.");
        if (Execute(tx, "DELETE FROM Clients WHERE Id = $id", ("$id", id)) != 1)
            throw new IOException("Клиент уже удалён. Обновите список и повторите действие.");
        tx.Commit();
        return next;
    }

    internal static long DeleteTariff(long version, string id)
    {
        if (id == Tariff.BindingId)
            throw new InvalidDataException("Служебный тариф скрепления используется для расчёта брошюр. Его можно изменить, но нельзя удалить.");
        using var connection = Open();
        using var tx = connection.BeginTransaction();
        long next = AdvanceVersion(tx, "tariffs", version);
        if (Execute(tx, "DELETE FROM Tariffs WHERE Id = $id", ("$id", id)) != 1)
            throw new IOException("Тариф уже удалён. Обновите прайс и повторите действие.");
        tx.Commit();
        return next;
    }

    internal static TariffCatalog.CatalogFile LoadTariffs(Func<TariffCatalog.CatalogFile> readLegacy)
    {
        using var connection = Open();
        using var tx = connection.BeginTransaction();
        long? version = Version(tx, "tariffs");
        var items = new List<Tariff>();
        if (version == null)
        {
            items = readLegacy().Items;
            foreach (var tariff in items)
                WriteTariff(tx, tariff);
            version = 0;
            Execute(tx, "INSERT INTO State VALUES ('tariffs', 0)");
        }
        else
        {
            Read(tx, "SELECT Id, Name, Unit, Rate FROM Tariffs ORDER BY Position", row =>
            {
                items.Add(new Tariff(row.GetString(0), row.GetString(1),
                    (PriceUnit)row.GetInt32(2), row.GetDecimal(3)));
            });
        }
        tx.Commit();
        return new TariffCatalog.CatalogFile(version.Value, items);
    }

    internal static long SaveTariff(long version, Tariff tariff)
    {
        using var connection = Open();
        using var tx = connection.BeginTransaction();
        long next = AdvanceVersion(tx, "tariffs", version);
        WriteTariff(tx, tariff);
        tx.Commit();
        return next;
    }

    private static void WriteTariff(SqliteTransaction tx, Tariff tariff)
    {
        Execute(tx, """
            INSERT INTO Tariffs (Id, Name, Unit, Rate, Position)
            VALUES ($id, $name, $unit, $rate, (SELECT COALESCE(MAX(Position), 0) + 1 FROM Tariffs))
            ON CONFLICT(Id) DO UPDATE SET
                Name = excluded.Name, Unit = excluded.Unit, Rate = excluded.Rate
            """, ("$id", tariff.Id), ("$name", tariff.Name),
            ("$unit", (int)tariff.Unit), ("$rate", tariff.Rate));
    }
}
