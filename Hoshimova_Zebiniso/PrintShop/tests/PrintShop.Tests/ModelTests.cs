using PrintShop.Core.Models;
using PrintShop.Core.Models.Products;
using PrintShop.Core.Services;

namespace PrintShop.Tests;

public class ModelTests
{
    private static ProductType Type(ProductKind kind, decimal basePrice) =>
        new() { Id = (int)kind + 1, Name = EnumNames.Of(kind), Kind = kind, BasePrice = basePrice };

    private static Material Sheet(string format = PaperFormats.A3, decimal price = 2m) =>
        new() { Id = 1, Name = "Бумага", Density = 300, Format = format, Price = price, Quantity = 1000 };

    private static Material Roll() =>
        new() { Id = 2, Name = "Баннер", Density = 440, Format = PaperFormats.Roll, Price = 25m, Quantity = 100 };

    [Fact]
    public void Quantity_LessThanOne_Throws()
    {
        var p = new Flyer(Type(ProductKind.Flyer, 1m), Sheet());
        Assert.Throws<OrderValidationException>(() => p.Quantity = 0);
    }

    [Fact]
    public void NegativePrice_Throws()
    {
        Assert.Throws<OrderValidationException>(() => new Material { Price = -1 });
        Assert.Throws<OrderValidationException>(() => new AdditionalService { Price = -5 });
    }

    [Fact]
    public void BusinessCard_MaterialUsage_UsesSheetLayout()
    {
        // На A3 (297×420) помещается визиток 90×50: max(3*8, 5*4) = 24
        Assert.Equal(24, PaperFormats.ItemsPerSheet(PaperFormats.BusinessCard, PaperFormats.A3));
        var card = new BusinessCard(Type(ProductKind.BusinessCard, 0.3m), Sheet()) { Quantity = 100 };
        Assert.Equal(5m, card.CalculateMaterialUsage()); // ceil(100/24)
    }

    [Fact]
    public void BusinessCard_Price_IsPolymorphic()
    {
        PrintProduct card = new BusinessCard(Type(ProductKind.BusinessCard, 0.3m), Sheet(price: 2m))
        {
            Quantity = 100,
            Color = ColorMode.BlackWhite
        };
        // 0.3 * 100 * 1.0 + 5 листов * 2 = 40
        Assert.Equal(40m, card.CalculatePrice());
    }

    [Fact]
    public void Banner_PriceByArea()
    {
        var banner = new Banner(Type(ProductKind.Banner, 45m), Roll()) { Width = 2m, Height = 1.5m, Quantity = 1 };
        // 45 * 3 м² + 3 м² * 25 = 210
        Assert.Equal(210m, banner.CalculatePrice());
    }

    [Fact]
    public void Banner_OnSheetMaterial_Throws()
    {
        var banner = new Banner(Type(ProductKind.Banner, 45m), Sheet());
        Assert.Throws<OrderValidationException>(() => banner.CalculateMaterialUsage());
    }

    [Fact]
    public void Booklet_PageCountMustBeMultipleOf4()
    {
        var b = new Booklet(Type(ProductKind.Booklet, 0.4m), Sheet());
        Assert.Throws<OrderValidationException>(() => b.PageCount = 10);
        b.PageCount = 12;
        Assert.Equal(12, b.PageCount);
    }

    [Fact]
    public void ProductParameters_RoundTrip()
    {
        var b = new Banner(Type(ProductKind.Banner, 45m), Roll()) { Width = 2.5m, Height = 1.2m };
        var copy = new Banner(Type(ProductKind.Banner, 45m), Roll());
        copy.DeserializeParameters(b.SerializeParameters());
        Assert.Equal(2.5m, copy.Width);
        Assert.Equal(1.2m, copy.Height);
    }

    [Theory]
    [InlineData(100, 0)]
    [InlineData(500, 5)]
    [InlineData(999, 5)]
    [InlineData(1000, 10)]
    [InlineData(5000, 15)]
    public void VolumeDiscount_Scale(int qty, decimal expected)
    {
        Assert.Equal(expected, new PriceCalculator().GetVolumeDiscountPercent(qty));
    }

    [Fact]
    public void Calculator_AppliesServicesAndDiscount()
    {
        var card = new BusinessCard(Type(ProductKind.BusinessCard, 0.3m), Sheet(price: 2m))
        {
            Quantity = 1000,
            Color = ColorMode.BlackWhite
        };
        card.AddService(new AdditionalService { Id = 1, Name = "Резка", Price = 10m, ChargeType = ServiceChargeType.PerItem });
        var b = new PriceCalculator().CalculateItem(card);
        // печать 300 + материал ceil(1000/24)=42 листа * 2 = 84 → 384; + резка 10 = 394; скидка 10% = 39.40
        Assert.Equal(394m, b.Subtotal);
        Assert.Equal(39.40m, b.DiscountAmount);
        Assert.Equal(354.60m, b.Total);
    }

    [Fact]
    public void Order_StatusTransitions_AreValidated()
    {
        var order = new Order { Number = "T-1", Customer = new PersonCustomer { Name = "Тест", Phone = "+992 900000000" } };
        order.AddItem(new OrderItem(new Flyer(Type(ProductKind.Flyer, 1m), Sheet())));

        Assert.Throws<OrderValidationException>(() => order.ChangeStatus(OrderStatus.Ready, null));

        OrderStatusChangedEventArgs? raised = null;
        order.StatusChanged += (_, e) => raised = e;
        order.ChangeStatus(OrderStatus.LayoutApproval, null);

        Assert.NotNull(raised);
        Assert.Equal(OrderStatus.New, raised!.OldStatus);
        Assert.Equal(OrderStatus.LayoutApproval, order.Status);
        Assert.Single(order.History);
    }

    [Fact]
    public void Order_CannotIssueWithDebt()
    {
        var order = new Order { Number = "T-2", Customer = new PersonCustomer { Name = "Тест", Phone = "+992 900000000" } };
        order.AddItem(new OrderItem(new Flyer(Type(ProductKind.Flyer, 1m), Sheet())));
        order.TotalPrice = 100;
        order.ChangeStatus(OrderStatus.LayoutApproval, null);
        order.ChangeStatus(OrderStatus.Printing, null);
        order.ChangeStatus(OrderStatus.Ready, null);
        Assert.Throws<OrderValidationException>(() => order.ChangeStatus(OrderStatus.Issued, null));

        order.AddPayment(new Payment { Amount = 100 });
        order.ChangeStatus(OrderStatus.Issued, null);
        Assert.Equal(0m, order.Debt);
    }

    [Fact]
    public void Order_PaymentOverDebt_Throws()
    {
        var order = new Order { Number = "T-3", TotalPrice = 50 };
        Assert.Throws<OrderValidationException>(() => order.AddPayment(new Payment { Amount = 60 }));
    }

    [Fact]
    public void Customer_InvalidEmail_Throws()
    {
        var c = new CompanyCustomer();
        Assert.Throws<OrderValidationException>(() => c.Email = "not-an-email");
        Assert.Equal("Организация «Тест»", new CompanyCustomer { Name = "Тест" }.GetDisplayName());
    }
}
