using PrintShop.Core.Data;

namespace PrintShop.Core.Services;

/// <summary>
/// Композиция зависимостей приложения: создаёт контекст БД, репозитории и сервисы
/// и связывает их между собой (простая альтернатива DI-контейнеру).
/// </summary>
public class AppServices
{
    public AppServices(string databasePath)
    {
        Database = new DatabaseContext(databasePath);
        Database.Initialize();

        Users = new UserRepository(Database);
        Customers = new CustomerRepository(Database);
        ProductTypes = new ProductTypeRepository(Database);
        Materials = new MaterialRepository(Database);
        Services = new ServiceRepository(Database);
        OrderRepository = new OrderRepository(Database, Customers, Users, ProductTypes, Materials, Services);

        Calculator = new PriceCalculator();
        Auth = new AuthService(Users);
        Orders = new OrderService(Database, OrderRepository, Materials, Calculator, Auth);
        Reports = new ReportService(OrderRepository, Materials);
        Backup = new BackupService(Database);
    }

    public DatabaseContext Database { get; }

    public UserRepository Users { get; }
    public CustomerRepository Customers { get; }
    public ProductTypeRepository ProductTypes { get; }
    public MaterialRepository Materials { get; }
    public ServiceRepository Services { get; }
    public OrderRepository OrderRepository { get; }

    public IPriceCalculator Calculator { get; }
    public AuthService Auth { get; }
    public OrderService Orders { get; }
    public ReportService Reports { get; }
    public BackupService Backup { get; }
}
