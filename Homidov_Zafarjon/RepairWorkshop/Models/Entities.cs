namespace RepairWorkshop.Models;
public enum RepairStatus { Accepted, Diagnostics, InRepair, Ready, Issued }
public abstract class Device
{
    public string Brand { get; }
    public string Model { get; }
    protected Device(string brand, string model)
    {
        if (string.IsNullOrWhiteSpace(brand) || string.IsNullOrWhiteSpace(model)) throw new ArgumentException("Укажите марку и модель.");
        Brand = brand.Trim(); Model = model.Trim();
    }
    public abstract string Describe();
}
public sealed class Phone : Device
{
    public Phone(string brand, string model) : base(brand,model) { }
    public override string Describe() => $"Телефон {Brand} {Model}";
}
public sealed class Laptop : Device
{
    public Laptop(string brand, string model) : base(brand,model) { }
    public override string Describe() => $"Ноутбук {Brand} {Model}";
}
public sealed class OtherDevice : Device
{
    private readonly string _type;
    public OtherDevice(string type,string brand,string model):base(brand,model) => _type=type;
    public override string Describe() => $"{_type} {Brand} {Model}";
}
public sealed record Customer(int Id,string FullName,string Phone);
public sealed record Technician(int Id,string FullName,string Specialty);
public sealed record SparePart(int Id,string Name,int Quantity,decimal Price);
public sealed class RepairOrder
{
    public int Id { get; init; }
    public int CustomerId { get; init; }
    public int? TechnicianId { get; private set; }
    public RepairStatus Status { get; private set; } = RepairStatus.Accepted;
    public decimal Cost { get; private set; }
    public void Assign(int? id) => TechnicianId = id;
    public void ChangeStatus(RepairStatus next)
    {
        if (next < Status) throw new InvalidOperationException("Нельзя вернуть заказ на предыдущий этап.");
        Status=next;
    }
    public void SetCost(decimal cost)
    {
        if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
        Cost=cost;
    }
}
