using Microsoft.Data.Sqlite;
using PrintShop.Core.Models;

namespace PrintShop.Core.Data;

/// <summary>Репозиторий пользователей.</summary>
public class UserRepository : RepositoryBase<User>
{
    public UserRepository(DatabaseContext context) : base(context) { }

    protected override string TableName => "Users";
    protected override string OrderBy => "FullName";

    protected override User Map(SqliteDataReader r) => new()
    {
        Id = Int(r, "Id"),
        Login = Str(r, "Login"),
        PasswordHash = Str(r, "PasswordHash"),
        FullName = Str(r, "FullName"),
        Role = (UserRole)Int(r, "Role")
    };

    protected override void Insert(User u, DbSession s) => s.Execute(
        "INSERT INTO Users (Login, PasswordHash, FullName, Role) VALUES ($l, $p, $f, $r)",
        ("$l", u.Login), ("$p", u.PasswordHash), ("$f", u.FullName), ("$r", (int)u.Role));

    protected override void UpdateRow(User u, DbSession s) => s.Execute(
        "UPDATE Users SET Login=$l, PasswordHash=$p, FullName=$f, Role=$r WHERE Id=$id",
        ("$l", u.Login), ("$p", u.PasswordHash), ("$f", u.FullName), ("$r", (int)u.Role), ("$id", u.Id));

    public User? GetByLogin(string login)
    {
        using var s = Context.Open();
        return Query(s, "SELECT * FROM Users WHERE Login = $l", ("$l", login.Trim().ToLowerInvariant())).FirstOrDefault();
    }

    public int CountAdmins()
    {
        using var s = Context.Open();
        return Convert.ToInt32(s.Scalar("SELECT COUNT(*) FROM Users WHERE Role = $r", ("$r", (int)UserRole.Admin)));
    }
}

/// <summary>Репозиторий клиентов. Создаёт объекты PersonCustomer/CompanyCustomer по полю Type.</summary>
public class CustomerRepository : RepositoryBase<Customer>
{
    public CustomerRepository(DatabaseContext context) : base(context) { }

    protected override string TableName => "Customers";
    protected override string OrderBy => "Name COLLATE NOCASE";

    protected override Customer Map(SqliteDataReader r)
    {
        var c = Customer.Create((CustomerType)Int(r, "Type"));
        c.Id = Int(r, "Id");
        c.Name = Str(r, "Name");
        c.Phone = Str(r, "Phone");
        c.Email = Str(r, "Email");
        c.Address = Str(r, "Address");
        c.CreatedAt = Date(r, "CreatedAt");
        return c;
    }

    protected override void Insert(Customer c, DbSession s) => s.Execute(
        "INSERT INTO Customers (Type, Name, Phone, Email, Address, CreatedAt) VALUES ($t, $n, $p, $e, $a, $c)",
        ("$t", (int)c.Type), ("$n", c.Name), ("$p", c.Phone), ("$e", c.Email), ("$a", c.Address),
        ("$c", DatabaseContext.ToDb(c.CreatedAt)));

    protected override void UpdateRow(Customer c, DbSession s) => s.Execute(
        "UPDATE Customers SET Type=$t, Name=$n, Phone=$p, Email=$e, Address=$a WHERE Id=$id",
        ("$t", (int)c.Type), ("$n", c.Name), ("$p", c.Phone), ("$e", c.Email), ("$a", c.Address), ("$id", c.Id));

    public override void Delete(int id)
    {
        using (var s = Context.Open())
        {
            var count = Convert.ToInt32(s.Scalar("SELECT COUNT(*) FROM Orders WHERE CustomerId = $id", ("$id", id)));
            if (count > 0)
                throw new OrderValidationException($"Нельзя удалить клиента: у него есть заказы ({count} шт.).");
        }
        base.Delete(id);
    }
}

/// <summary>Репозиторий видов продукции (прайс-лист).</summary>
public class ProductTypeRepository : RepositoryBase<ProductType>
{
    public ProductTypeRepository(DatabaseContext context) : base(context) { }

    protected override string TableName => "ProductTypes";
    protected override string OrderBy => "Kind, Name";

    protected override ProductType Map(SqliteDataReader r) => new()
    {
        Id = Int(r, "Id"),
        Name = Str(r, "Name"),
        Kind = (ProductKind)Int(r, "Kind"),
        BasePrice = Dec(r, "BasePrice"),
        Unit = Str(r, "Unit")
    };

    protected override void Insert(ProductType p, DbSession s) => s.Execute(
        "INSERT INTO ProductTypes (Name, Kind, BasePrice, Unit) VALUES ($n, $k, $b, $u)",
        ("$n", p.Name), ("$k", (int)p.Kind), ("$b", p.BasePrice), ("$u", p.Unit));

    protected override void UpdateRow(ProductType p, DbSession s) => s.Execute(
        "UPDATE ProductTypes SET Name=$n, Kind=$k, BasePrice=$b, Unit=$u WHERE Id=$id",
        ("$n", p.Name), ("$k", (int)p.Kind), ("$b", p.BasePrice), ("$u", p.Unit), ("$id", p.Id));
}

/// <summary>Репозиторий материалов и их складских остатков.</summary>
public class MaterialRepository : RepositoryBase<Material>
{
    public MaterialRepository(DatabaseContext context) : base(context) { }

    protected override string TableName => "Materials";
    protected override string OrderBy => "Name";

    protected override Material Map(SqliteDataReader r) => new()
    {
        Id = Int(r, "Id"),
        Name = Str(r, "Name"),
        Density = Int(r, "Density"),
        Format = Str(r, "Format"),
        Price = Dec(r, "Price"),
        Quantity = Dec(r, "Quantity"),
        MinQuantity = Dec(r, "MinQuantity")
    };

    protected override void Insert(Material m, DbSession s) => s.Execute(
        "INSERT INTO Materials (Name, Density, Format, Price, Quantity, MinQuantity) VALUES ($n, $d, $f, $p, $q, $m)",
        ("$n", m.Name), ("$d", m.Density), ("$f", m.Format), ("$p", m.Price), ("$q", m.Quantity), ("$m", m.MinQuantity));

    protected override void UpdateRow(Material m, DbSession s) => s.Execute(
        "UPDATE Materials SET Name=$n, Density=$d, Format=$f, Price=$p, Quantity=$q, MinQuantity=$m WHERE Id=$id",
        ("$n", m.Name), ("$d", m.Density), ("$f", m.Format), ("$p", m.Price), ("$q", m.Quantity), ("$m", m.MinQuantity),
        ("$id", m.Id));

    /// <summary>Текущий остаток материала, прочитанный в рамках сеанса (для списания в транзакции).</summary>
    public decimal GetQuantity(int id, DbSession s) =>
        DatabaseContext.Dec(s.Scalar("SELECT Quantity FROM Materials WHERE Id = $id", ("$id", id))!);

    public void SetQuantity(int id, decimal quantity, DbSession s) =>
        s.Execute("UPDATE Materials SET Quantity = $q WHERE Id = $id", ("$q", quantity), ("$id", id));
}

/// <summary>Репозиторий дополнительных услуг.</summary>
public class ServiceRepository : RepositoryBase<AdditionalService>
{
    public ServiceRepository(DatabaseContext context) : base(context) { }

    protected override string TableName => "Services";
    protected override string OrderBy => "Name";

    protected override AdditionalService Map(SqliteDataReader r) => new()
    {
        Id = Int(r, "Id"),
        Name = Str(r, "Name"),
        Price = Dec(r, "Price"),
        ChargeType = (ServiceChargeType)Int(r, "ChargeType")
    };

    protected override void Insert(AdditionalService a, DbSession s) => s.Execute(
        "INSERT INTO Services (Name, Price, ChargeType) VALUES ($n, $p, $c)",
        ("$n", a.Name), ("$p", a.Price), ("$c", (int)a.ChargeType));

    protected override void UpdateRow(AdditionalService a, DbSession s) => s.Execute(
        "UPDATE Services SET Name=$n, Price=$p, ChargeType=$c WHERE Id=$id",
        ("$n", a.Name), ("$p", a.Price), ("$c", (int)a.ChargeType), ("$id", a.Id));
}
