using AutoService.Domain.Discounts;
using AutoService.Domain.Entities;
using AutoService.Domain.Enums;
using AutoService.Domain.Exceptions;

namespace AutoService.Tests;

public class RepairRequestTests
{
    [Fact]
    public void New_request_has_status_New_and_zero_total()
    {
        var r = TestData.NewRequest();

        Assert.Equal(RequestStatus.New, r.Status);
        Assert.Equal(0m, r.Total);
    }

    [Fact]
    public void Request_requires_problem_description()
    {
        var (client, car) = TestData.ClientWithCar();

        Assert.Throws<DomainException>(() => new RepairRequest("1", client, car, "  "));
    }

    [Fact]
    public void Car_of_another_client_is_rejected()
    {
        var (_, car) = TestData.ClientWithCar();
        var stranger = TestData.Client();

        Assert.Throws<DomainException>(() => new RepairRequest("1", stranger, car, "Стук"));
    }

    [Fact]
    public void Cannot_jump_from_New_to_Ready()
    {
        var r = TestData.NewRequest();

        var ex = Assert.Throws<InvalidStatusTransitionException>(() => r.Complete());
        Assert.Equal(RequestStatus.New, ex.From);
        Assert.Equal(RequestStatus.Ready, ex.To);
    }

    [Fact]
    public void Cannot_send_for_approval_without_works()
    {
        var r = TestData.NewRequest();
        r.StartDiagnostics();

        Assert.Throws<DomainException>(() => r.SendForApproval());
    }

    [Fact]
    public void Mechanic_of_other_specialization_cannot_take_work()
    {
        var r = TestData.NewRequest();

        Assert.Throws<DomainException>(() => r.AddWork(TestData.Wiring(), TestData.Locksmith()));
    }

    [Fact]
    public void Universal_mechanic_can_take_any_work()
    {
        var r = TestData.NewRequest();
        var universal = new Mechanic("Сидоров", "+992900000003", DateTime.Today, Specialization.Universal, 5);

        r.AddWork(TestData.Wiring(), universal);

        Assert.Single(r.Works);
        Assert.Equal(500m, r.WorksSum);
    }

    [Fact]
    public void Adding_part_writes_it_off_from_stock()
    {
        var r = TestData.NewRequest();
        var oil = TestData.Oil(stock: 5);

        r.AddPart(oil, 2);
        r.AddPart(oil, 1); // та же запчасть — увеличивается количество

        Assert.Equal(2, oil.Stock);
        Assert.Single(r.Parts);
        Assert.Equal(3, r.Parts[0].Quantity);
        Assert.Equal(1050m, r.PartsSum);
    }

    [Fact]
    public void Not_enough_stock_throws_and_keeps_stock()
    {
        var r = TestData.NewRequest();
        var oil = TestData.Oil(stock: 1);

        Assert.Throws<NotEnoughStockException>(() => r.AddPart(oil, 3));
        Assert.Equal(1, oil.Stock);
        Assert.Empty(r.Parts);
    }

    [Fact]
    public void Removing_part_returns_it_to_stock()
    {
        var r = TestData.NewRequest();
        var oil = TestData.Oil(stock: 5);
        var item = r.AddPart(oil, 2);

        r.RemovePart(item);

        Assert.Equal(5, oil.Stock);
        Assert.Empty(r.Parts);
    }

    [Fact]
    public void Works_cannot_be_changed_after_approval()
    {
        var r = TestData.NewRequest();
        r.StartDiagnostics();
        r.AddWork(TestData.OilChange(), TestData.Locksmith());
        r.SendForApproval();
        r.Approve();

        Assert.Throws<DomainException>(() => r.AddWork(TestData.OilChange(), TestData.Locksmith()));
    }

    [Fact]
    public void Total_includes_discount()
    {
        var r = TestData.NewRequest();
        r.AddWork(TestData.Wiring(), new Mechanic("Ахмедов", "+1", DateTime.Today, Specialization.Electrician, 3)); // 500
        r.AddPart(TestData.Oil(), 1); // 350

        r.ApplyDiscount(new PercentDiscount(10));

        Assert.Equal(850m, r.Subtotal);
        Assert.Equal(85m, r.DiscountAmount);
        Assert.Equal(765m, r.Total);
    }

    [Fact]
    public void Discount_over_50_percent_is_rejected()
    {
        var r = TestData.NewRequest();

        Assert.Throws<DomainException>(() => r.ApplyDiscount(new PercentDiscount(70)));
    }

    [Fact]
    public void Full_cycle_with_cash_payment()
    {
        var r = TestData.ReadyRequest(); // итог 450
        var cash = new CashPayment(450m, received: 500m);

        r.Pay(cash);

        Assert.Equal(RequestStatus.Issued, r.Status);
        Assert.NotNull(r.IssuedAt);
        Assert.Equal(50m, cash.Change);
        Assert.True(r.IsClosed);
    }

    [Fact]
    public void Payment_must_equal_total()
    {
        var r = TestData.ReadyRequest();

        Assert.Throws<DomainException>(() => r.Pay(new CashPayment(100m, 100m)));
        Assert.Equal(RequestStatus.Ready, r.Status);
    }

    [Fact]
    public void Cannot_pay_unfinished_request()
    {
        var r = TestData.NewRequest();

        Assert.Throws<InvalidStatusTransitionException>(() => r.Pay(new CashPayment(1m, 1m)));
    }

    [Fact]
    public void Card_payment_requires_four_digits()
    {
        var r = TestData.ReadyRequest();

        Assert.Throws<DomainException>(() => r.Pay(new CardPayment(r.Total, "12a")));
        Assert.Equal(RequestStatus.Ready, r.Status);

        r.Pay(new CardPayment(r.Total, "4242"));
        Assert.Equal(RequestStatus.Issued, r.Status);
    }

    [Fact]
    public void Cancel_returns_parts_to_stock()
    {
        var r = TestData.NewRequest();
        var oil = TestData.Oil(stock: 4);
        r.AddPart(oil, 3);

        r.Cancel();

        Assert.Equal(RequestStatus.Cancelled, r.Status);
        Assert.Equal(4, oil.Stock);
    }

    [Fact]
    public void Issued_request_cannot_be_cancelled()
    {
        var r = TestData.ReadyRequest();
        r.Pay(new CardPayment(r.Total, "1111"));

        Assert.Throws<InvalidStatusTransitionException>(() => r.Cancel());
    }

    [Fact]
    public void Waiting_for_parts_returns_to_work()
    {
        var r = TestData.NewRequest();
        r.StartDiagnostics();
        r.AddWork(TestData.OilChange(), TestData.Locksmith());
        r.SendForApproval();
        r.Approve();
        r.StartWork();

        r.WaitForParts();
        r.StartWork();

        Assert.Equal(RequestStatus.InProgress, r.Status);
    }
}
