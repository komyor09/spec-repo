using RepairWorkshop.Data;
using RepairWorkshop.Models;
namespace RepairWorkshop.Services;
public sealed class WorkshopService
{
    private readonly Database db;
    public WorkshopService(Database database)=>db=database;
    public void AddCustomer(string name,string phone)
    {Required(name,phone);db.Execute("INSERT INTO Customers(FullName,Phone) VALUES($n,$p)",("$n",name.Trim()),("$p",phone.Trim()));}
    public void AddTechnician(string name,string specialty)
    {Required(name,specialty);db.Execute("INSERT INTO Technicians(FullName,Specialty) VALUES($n,$s)",("$n",name.Trim()),("$s",specialty.Trim()));}
    public void AddPart(string name,int qty,decimal price)
    {Required(name);if(qty<0||price<0)throw new ArgumentException("Количество и цена должны быть неотрицательными.");db.Execute("INSERT INTO Parts(Name,Quantity,Price) VALUES($n,$q,$p)",("$n",name.Trim()),("$q",qty),("$p",(double)price));}
    public void CreateOrder(int clientId,string type,string brand,string model,string fault,int? technicianId,decimal cost)
    {
        Required(type,brand,model,fault);if(cost<0)throw new ArgumentException("Стоимость не может быть отрицательной.");
        Device device=type switch { "Телефон"=>new Phone(brand,model),"Ноутбук"=>new Laptop(brand,model),_=>new OtherDevice(type,brand,model)};
        _=device.Describe(); // Виртуальный вызов демонстрирует полиморфизм.
        db.Execute("INSERT INTO Orders(CustomerId,TechnicianId,DeviceType,Brand,Model,Fault,Status,Cost,CreatedAt) VALUES($c,$t,$d,$b,$m,$f,0,$p,$at)",
        ("$c",clientId),("$t",technicianId),("$d",type),("$b",brand.Trim()),("$m",model.Trim()),("$f",fault.Trim()),("$p",(double)cost),("$at",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
    }
    public void UpdateStatus(int id, RepairStatus next)
    {
        var t=db.Query("SELECT Status FROM Orders WHERE Id=$i",("$i",id));
        if(t.Rows.Count==0)throw new ArgumentException("Заказ не найден.");
        var current=(RepairStatus)Convert.ToInt32(t.Rows[0][0]);
        if(next<current)throw new InvalidOperationException("Возврат к предыдущему статусу запрещён.");
        db.Execute("UPDATE Orders SET Status=$s WHERE Id=$i",("$s",(int)next),("$i",id));
    }
    private static void Required(params string[] values)
    {if(values.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Заполните все обязательные поля.");}
}
