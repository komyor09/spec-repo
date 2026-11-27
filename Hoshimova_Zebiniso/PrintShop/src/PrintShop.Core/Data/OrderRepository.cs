using Microsoft.Data.Sqlite;
using PrintShop.Core.Models;
using PrintShop.Core.Models.Products;

namespace PrintShop.Core.Data;

/// <summary>
/// Репозиторий заказов. Загружает заказ вместе с позициями, услугами, оплатами и историей статусов.
/// Сохранение заказа затрагивает несколько таблиц и всегда выполняется в транзакции.
/// </summary>
public class OrderRepository : RepositoryBase<Order>
{
    private readonly CustomerRepository _customers;
    private readonly UserRepository _users;
    private readonly ProductTypeRepository _productTypes;
    private readonly MaterialRepository _materials;
    private readonly ServiceRepository _services;

    // Справочники, загружаемые на время одного чтения
    private Lookups? _lookups;

    public OrderRepository(DatabaseContext context, CustomerRepository customers, UserRepository users,
        ProductTypeRepository productTypes, MaterialRepository materials, ServiceRepository services) : base(context)
    {
        _customers = customers;
        _users = users;
        _productTypes = productTypes;
        _materials = materials;
        _services = services;
    }

    protected override string TableName => "Orders";
    protected override string OrderBy => "CreatedAt DESC, Id DESC";

    private sealed record Lookups(
        Dictionary<int, Customer> Customers,
        Dictionary<int, User> Users,
        Dictionary<int, ProductType> ProductTypes,
        Dictionary<int, Material> Materials,
        Dictionary<int, AdditionalService> Services);

    private Lookups LoadLookups() => new(
        _customers.GetAll().ToDictionary(x => x.Id),
        _users.GetAll().ToDictionary(x => x.Id),
        _productTypes.GetAll().ToDictionary(x => x.Id),
        _materials.GetAll().ToDictionary(x => x.Id),
        _services.GetAll().ToDictionary(x => x.Id));

    public override IReadOnlyList<Order> GetAll()
    {
        _lookups = LoadLookups();
        try
        {
            using var s = Context.Open();
            var orders = Query(s, $"SELECT * FROM Orders ORDER BY {OrderBy}");
            LoadDetails(orders, s);
            return orders;
        }
        finally { _lookups = null; }
    }

    public override Order? GetById(int id)
    {
        _lookups = LoadLookups();
        try
        {
            using var s = Context.Open();
            var orders = Query(s, "SELECT * FROM Orders WHERE Id = $id", ("$id", id));
            LoadDetails(orders, s);
            return orders.FirstOrDefault();
        }
        finally { _lookups = null; }
    }

    protected override Order Map(SqliteDataReader r)
    {
        var lk = _lookups ?? throw new InvalidOperationException("Справочники не загружены.");
        var order = new Order
        {
            Id = Int(r, "Id"),
            Number = Str(r, "Number"),
            CreatedAt = Date(r, "CreatedAt"),
        };
        order.DueDate = Date(r, "DueDate");
        order.Customer = lk.Customers[Int(r, "CustomerId")];
        order.Manager = r["ManagerId"] is DBNull ? null : lk.Users.GetValueOrDefault(Int(r, "ManagerId"));
        order.TotalPrice = Dec(r, "TotalPrice");
        order.Discount = Dec(r, "Discount");
        order.LayoutPath = Str(r, "LayoutPath");
        order.Comment = Str(r, "Comment");
        // Статус временно сохраняем в истории — окончательно восстанавливается в LoadDetails
        _pendingStatus[order.Id] = (OrderStatus)Int(r, "Status");
        return order;
    }

    private readonly Dictionary<int, OrderStatus> _pendingStatus = new();

    private void LoadDetails(List<Order> orders, DbSession s)
    {
        var lk = _lookups!;
        if (orders.Count == 0) return;
        var ids = string.Join(",", orders.Select(o => o.Id));

        // Услуги позиций
        var itemServices = new Dictionary<int, List<AdditionalService>>();
        using (var cmd = s.Command($"SELECT ois.OrderItemId, ois.ServiceId FROM OrderItemServices ois " +
                                   $"JOIN OrderItems oi ON oi.Id = ois.OrderItemId WHERE oi.OrderId IN ({ids})"))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                int itemId = r.GetInt32(0);
                if (!itemServices.TryGetValue(itemId, out var list)) itemServices[itemId] = list = new();
                if (lk.Services.TryGetValue(r.GetInt32(1), out var svc)) list.Add(svc);
            }

        // Позиции
        var items = new List<OrderItem>();
        using (var cmd = s.Command($"SELECT * FROM OrderItems WHERE OrderId IN ({ids}) ORDER BY Id"))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var type = lk.ProductTypes[Int(r, "ProductTypeId")];
                var material = lk.Materials[Int(r, "MaterialId")];
                var product = ProductFactory.Create(type, material);
                product.Quantity = Int(r, "Quantity");
                if (product is not Banner) product.Format = Str(r, "Format");
                product.Color = (ColorMode)Int(r, "Color");
                product.DeserializeParameters(Str(r, "Params"));
                int itemId = Int(r, "Id");
                if (itemServices.TryGetValue(itemId, out var svcs))
                    foreach (var svc in svcs) product.AddService(svc);

                items.Add(new OrderItem(product)
                {
                    Id = itemId,
                    OrderId = Int(r, "OrderId"),
                    Price = Dec(r, "Price"),
                    Discount = Dec(r, "Discount")
                });
            }

        // Оплаты
        var payments = new List<Payment>();
        using (var cmd = s.Command($"SELECT * FROM Payments WHERE OrderId IN ({ids}) ORDER BY PaymentDate"))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                payments.Add(new Payment
                {
                    Id = Int(r, "Id"),
                    OrderId = Int(r, "OrderId"),
                    Amount = Dec(r, "Amount"),
                    PaymentDate = Date(r, "PaymentDate"),
                    PaymentType = (PaymentType)Int(r, "PaymentType")
                });

        // История статусов
        var history = new List<StatusHistoryEntry>();
        using (var cmd = s.Command($"SELECT * FROM StatusHistory WHERE OrderId IN ({ids}) ORDER BY ChangedAt, Id"))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                int? userId = r["UserId"] is DBNull ? null : Int(r, "UserId");
                history.Add(new StatusHistoryEntry
                {
                    Id = Int(r, "Id"),
                    OrderId = Int(r, "OrderId"),
                    Status = (OrderStatus)Int(r, "Status"),
                    ChangedAt = Date(r, "ChangedAt"),
                    UserId = userId,
                    UserName = userId.HasValue && lk.Users.TryGetValue(userId.Value, out var u) ? u.FullName : "—"
                });
            }

        // Группировка с помощью LINQ
        var itemsByOrder = items.ToLookup(i => i.OrderId);
        var paymentsByOrder = payments.ToLookup(p => p.OrderId);
        var historyByOrder = history.ToLookup(h => h.OrderId);

        foreach (var o in orders)
        {
            o.RestoreState(_pendingStatus[o.Id], itemsByOrder[o.Id], paymentsByOrder[o.Id], historyByOrder[o.Id]);
            _pendingStatus.Remove(o.Id);
        }
    }

    // ---------------- Запись ----------------

    protected override void Insert(Order o, DbSession s)
    {
        s.Execute("""
            INSERT INTO Orders (Number, CustomerId, ManagerId, CreatedAt, DueDate, Status, TotalPrice, Discount, LayoutPath, Comment)
            VALUES ($n, $c, $m, $cr, $d, $st, $t, $disc, $l, $com)
            """,
            ("$n", o.Number), ("$c", o.Customer.Id), ("$m", o.Manager?.Id), ("$cr", DatabaseContext.ToDb(o.CreatedAt)),
            ("$d", DatabaseContext.ToDb(o.DueDate)), ("$st", (int)o.Status), ("$t", o.TotalPrice), ("$disc", o.Discount),
            ("$l", o.LayoutPath), ("$com", o.Comment));
        o.Id = (int)s.LastInsertId();
        SaveItems(o, s);
        SaveNewPayments(o, s);
        SaveNewHistory(o, s);
    }

    protected override void UpdateRow(Order o, DbSession s)
    {
        s.Execute("""
            UPDATE Orders SET Number=$n, CustomerId=$c, ManagerId=$m, DueDate=$d, Status=$st,
                   TotalPrice=$t, Discount=$disc, LayoutPath=$l, Comment=$com
            WHERE Id=$id
            """,
            ("$n", o.Number), ("$c", o.Customer.Id), ("$m", o.Manager?.Id), ("$d", DatabaseContext.ToDb(o.DueDate)),
            ("$st", (int)o.Status), ("$t", o.TotalPrice), ("$disc", o.Discount), ("$l", o.LayoutPath),
            ("$com", o.Comment), ("$id", o.Id));
        // Позиции перезаписываются целиком (услуги удаляются каскадно)
        s.Execute("DELETE FROM OrderItems WHERE OrderId = $id", ("$id", o.Id));
        SaveItems(o, s);
        SaveNewPayments(o, s);
        SaveNewHistory(o, s);
    }

    /// <summary>Обновление только статуса и новых записей истории (без перезаписи позиций).</summary>
    public void UpdateStatus(Order o, DbSession s)
    {
        s.Execute("UPDATE Orders SET Status = $st WHERE Id = $id", ("$st", (int)o.Status), ("$id", o.Id));
        SaveNewHistory(o, s);
    }

    private static void SaveItems(Order o, DbSession s)
    {
        foreach (var item in o.Items)
        {
            var p = item.Product;
            s.Execute("""
                INSERT INTO OrderItems (OrderId, ProductTypeId, MaterialId, Quantity, Format, Color, Params, Price, Discount)
                VALUES ($o, $pt, $m, $q, $f, $c, $par, $p, $d)
                """,
                ("$o", o.Id), ("$pt", p.ProductType.Id), ("$m", p.Material.Id), ("$q", p.Quantity), ("$f", p.Format),
                ("$c", (int)p.Color), ("$par", p.SerializeParameters()), ("$p", item.Price), ("$d", item.Discount));
            item.Id = (int)s.LastInsertId();
            item.OrderId = o.Id;
            foreach (var svc in p.Services)
                s.Execute("INSERT INTO OrderItemServices (OrderItemId, ServiceId) VALUES ($i, $s)",
                    ("$i", item.Id), ("$s", svc.Id));
        }
    }

    private static void SaveNewPayments(Order o, DbSession s)
    {
        foreach (var pay in o.Payments.Where(p => p.IsNew))
            SavePayment(o, pay, s);
    }

    public static void SavePayment(Order o, Payment pay, DbSession s)
    {
        s.Execute("INSERT INTO Payments (OrderId, Amount, PaymentDate, PaymentType) VALUES ($o, $a, $d, $t)",
            ("$o", o.Id), ("$a", pay.Amount), ("$d", DatabaseContext.ToDb(pay.PaymentDate)), ("$t", (int)pay.PaymentType));
        pay.Id = (int)s.LastInsertId();
        pay.OrderId = o.Id;
    }

    private static void SaveNewHistory(Order o, DbSession s)
    {
        foreach (var h in o.History.Where(h => h.IsNew))
        {
            s.Execute("INSERT INTO StatusHistory (OrderId, Status, ChangedAt, UserId) VALUES ($o, $s, $d, $u)",
                ("$o", o.Id), ("$s", (int)h.Status), ("$d", DatabaseContext.ToDb(h.ChangedAt)), ("$u", h.UserId));
            h.Id = (int)s.LastInsertId();
            h.OrderId = o.Id;
        }
    }

    /// <summary>Следующий номер заказа вида ГГГГ-NNNN.</summary>
    public string GenerateNumber(DateTime date)
    {
        using var s = Context.Open();
        var prefix = date.Year + "-";
        var last = s.Scalar("SELECT MAX(Number) FROM Orders WHERE Number LIKE $p", ("$p", prefix + "%")) as string;
        int next = 1;
        if (last != null && int.TryParse(last[prefix.Length..], out var n)) next = n + 1;
        return $"{prefix}{next:D4}";
    }
}
