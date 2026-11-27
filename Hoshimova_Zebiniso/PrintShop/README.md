# Типография: заказы на печать

Курсовой проект по дисциплине «Объектно-ориентированное программирование».
C# / .NET 10 / WPF / SQLite (Microsoft.Data.Sqlite), архитектура MVVM.

## Запуск

1. Откройте `PrintShop.slnx` в Visual Studio 2026 (или 2022 17.14+).
2. Сделайте `PrintShop.App` запускаемым проектом (ПКМ → «Назначить запускаемым проектом»).
3. Нажмите F5. При первом запуске база `bin\...\Data\printshop.db` создаётся автоматически
   и заполняется справочниками и демонстрационными заказами.

Демо-пользователи (пароль = логин): `admin` — администратор, `manager` — менеджер, `printer` — печатник.

Тесты: «Тест → Обозреватель тестов → Запустить все» или `dotnet test`.

## Структура решения (многослойная архитектура)

```
src/PrintShop.Core            — модели, доступ к данным, бизнес-логика (без UI)
  Models/                     — слой моделей предметной области
    EntityBase                — базовый класс сущностей (Id, проверки)
    Customer (abstract)       → PersonCustomer, CompanyCustomer
    Products/PrintProduct (abstract, CalculatePrice())
                              → BusinessCard, Flyer, Booklet, Poster, Banner, Sticker
    Products/ProductFactory   — фабрика изделий по виду продукции
    Order, OrderItem, Payment, StatusHistoryEntry, Material, AdditionalService,
    ProductType, User, Enums (OrderStatus, UserRole, ...), OrderValidationException
  Data/                       — слой доступа к данным
    DatabaseContext           — подключение, создание таблиц, транзакции
    IRepository<T>, RepositoryBase<T> — обобщённый CRUD (шаблонный метод)
    UserRepository, CustomerRepository, ProductTypeRepository,
    MaterialRepository, ServiceRepository, OrderRepository
    DataSeeder                — начальное заполнение БД
  Services/                   — слой бизнес-логики
    IPriceCalculator, PriceCalculator — расчёт стоимости, скидки за тираж
    OrderService              — заказы, статусы, списание материалов, оплаты
    ReportService             — отчёты (LINQ) и экспорт в CSV
    AuthService               — вход, хэш паролей, права по ролям
    BackupService             — резервное копирование и восстановление БД
    AppServices               — сборка всех зависимостей
src/PrintShop.App             — слой представления (WPF, MVVM)
  Infrastructure/             — ObservableObject, RelayCommand, конвертеры, Ui
  ViewModels/                 — MainViewModel, OrdersViewModel, CrudViewModel<T>, ...
  Views/                      — окна и разделы, Views/Dialogs — формы редактирования
tests/PrintShop.Tests         — модульные и интеграционные тесты (xUnit)
```

## Где показаны принципы ООП

| Принцип | Где в коде |
|---|---|
| Инкапсуляция | закрытые поля + свойства с проверкой: `PrintProduct.Quantity` (≥ 1), `Material.Price` (≥ 0), `Customer.Phone/Email`, `Order.Status` (private set) |
| Наследование | `PrintProduct` → 6 видов продукции; `Customer` → `PersonCustomer`/`CompanyCustomer`; `EntityBase` → все сущности; `RepositoryBase<T>` → репозитории; `CrudViewModel<T>` → разделы |
| Полиморфизм | `CalculatePrice()`, `CalculateMaterialUsage()`, `AllowedFormats`, `Description`, `GetParameters()` переопределяются в наследниках; `List<OrderItem>` с разными изделиями |
| Абстракция | абстрактные классы `PrintProduct`, `Customer`, `RepositoryBase<T>`; интерфейсы `IRepository<T>`, `IPriceCalculator`, `IDialogService` |
| События | `Order.StatusChanged` (запись истории, обновление карточки заказа), `OrderService.OrdersChanged`, `AuthService.UserChanged` |
| Исключения | `OrderValidationException`; глобальный перехват в `App.xaml.cs`, `Ui.Safe(...)` |
| Generics | `IRepository<T>`, `RepositoryBase<T>`, `CrudViewModel<T>`, `ReportService.ExportCsv<T>`, `CsvColumn<T>`, `Option<T>` |
| LINQ | `OrderService.Search`, `ReportService` (GroupBy/SelectMany), `OrderRepository.LoadDetails` (ToLookup) |

## Правила расчёта стоимости

Стоимость позиции = (печать + материал) + доп. услуги − скидка за тираж.

* Цветность: ч/б ×1,0; 4+0 ×1,6; 4+4 ×2,2.
* Скидка за тираж: от 500 шт — 5 %, от 1000 — 10 %, от 5000 — 15 %.
* Расход материала считается по раскладке изделий на лист (`PaperFormats.ItemsPerSheet`),
  для баннеров — по площади в м². Материалы списываются со склада при переводе заказа
  в статус «В печати» (в транзакции; при нехватке — откат).

## Жизненный цикл заказа

Новый → Согласование макета → В печати → Постобработка → Готов → Выдан;
отмена возможна только до начала печати; выдача — только после полной оплаты.
