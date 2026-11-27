using PrintShop.Core.Data;
using PrintShop.Core.Models;
using PrintShop.Core.Models.Products;
using PrintShop.Core.Services;

namespace PrintShop.Tests;

/// <summary>Интеграционные тесты с настоящей БД SQLite во временном файле.</summary>
public sealed class DatabaseTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"printshop_test_{Guid.NewGuid():N}.db");
    private readonly AppServices _app;

    public DatabaseTests()
    {
        _app = new AppServices(_path);
        DataSeeder.Seed(_app);
    }

    public void Dispose()
    {
        try { File.Delete(_path); } catch { /* ignore */ }
    }

    [Fact]
    public void Seed_CreatesReferenceDataAndOrders()
    {
        Assert.Equal(3, _app.Users.GetAll().Count);
        Assert.Equal(6, _app.ProductTypes.GetAll().Count);
        Assert.Equal(4, _app.Orders.GetAll().Count);
    }

    [Fact]
    public void Login_WrongPassword_Throws()
    {
        Assert.Throws<OrderValidationException>(() => _app.Auth.Login("admin", "wrong"));
        Assert.Equal(UserRole.Admin, _app.Auth.Login("admin", "admin").Role);
    }

    [Fact]
    public void Order_IsPersistedWithItemsServicesAndHistory()
    {
        var issued = _app.Orders.GetAll().Single(o => o.Status == OrderStatus.Issued);
        Assert.Equal(0m, issued.Debt);
        Assert.Equal(2, issued.Items[0].Product.Services.Count);
        Assert.Equal(6, issued.History.Count); // Новый + 5 переходов
        Assert.IsType<BusinessCard>(issued.Items[0].Product);

        var printing = _app.Orders.GetAll().Single(o => o.Status == OrderStatus.Printing);
        var banner = Assert.IsType<Banner>(printing.Items.Single(i => i.Product is Banner).Product);
        Assert.Equal(3m, banner.Width);
    }

    [Fact]
    public void Printing_WritesOffMaterials()
    {
        _app.Auth.Login("manager", "manager");
        var order = _app.Orders.GetAll().Single(o => o.Status == OrderStatus.LayoutApproval);
        var material = order.Items[0].Product.Material;
        var need = order.Items[0].Product.CalculateMaterialUsage();
        var before = _app.Materials.GetById(material.Id)!.Quantity;

        _app.Orders.ChangeStatus(order, OrderStatus.Printing);

        Assert.Equal(before - need, _app.Materials.GetById(material.Id)!.Quantity);
        Assert.Equal(OrderStatus.Printing, _app.Orders.GetById(order.Id)!.Status);
    }

    [Fact]
    public void Printing_NotEnoughMaterial_RollsBack()
    {
        _app.Auth.Login("manager", "manager");
        var order = _app.Orders.GetAll().Single(o => o.Status == OrderStatus.LayoutApproval);
        var material = _app.Materials.GetById(order.Items[0].Product.Material.Id)!;
        material.Quantity = 1;
        _app.Materials.Update(material);

        Assert.Throws<OrderValidationException>(() => _app.Orders.ChangeStatus(order, OrderStatus.Printing));

        Assert.Equal(OrderStatus.LayoutApproval, order.Status);
        Assert.Equal(OrderStatus.LayoutApproval, _app.Orders.GetById(order.Id)!.Status);
        Assert.Equal(1m, _app.Materials.GetById(material.Id)!.Quantity);
    }

    [Fact]
    public void Printer_CannotCreateOrders()
    {
        _app.Auth.Login("printer", "printer");
        var draft = _app.Orders.CreateDraft();
        Assert.Throws<OrderValidationException>(() => _app.Orders.Save(draft));
    }

    [Fact]
    public void CustomerWithOrders_CannotBeDeleted()
    {
        var c = _app.Customers.GetAll().First(x => x.Name.Contains("Согд"));
        Assert.Throws<OrderValidationException>(() => _app.Customers.Delete(c.Id));
    }

    [Fact]
    public void Reports_Work()
    {
        var revenue = _app.Reports.GetRevenue(DateTime.Today.AddDays(-30), DateTime.Today);
        Assert.True(revenue.Sum(r => r.Total) > 0);
        var byProduct = _app.Reports.GetByProducts(DateTime.Today.AddDays(-30), DateTime.Today);
        Assert.Contains(byProduct, r => r.ProductName == "Визитки");
        Assert.Contains(_app.Reports.GetMaterials(onlyLow: true), r => r.Name == "Самоклеящаяся бумага");

        var csv = Path.ChangeExtension(_path, ".csv");
        ReportService.ExportCsv(csv, revenue, new[] { new CsvColumn<RevenueRow>("Дата", r => r.Date), new CsvColumn<RevenueRow>("Сумма", r => r.Total) });
        Assert.Contains("Дата;Сумма", File.ReadAllText(csv));
        File.Delete(csv);
    }

    [Fact]
    public void Backup_CreatesCopy()
    {
        var target = Path.ChangeExtension(_path, ".bak.db");
        _app.Backup.Backup(target);
        Assert.True(new FileInfo(target).Length > 0);
        File.Delete(target);
    }
}
