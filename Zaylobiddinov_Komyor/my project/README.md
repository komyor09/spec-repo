# Автосервис: заявки на ремонт

Курсовая работа по ООП (C#). Это настольное приложение на Windows Forms для учёта заявок на ремонт в небольшом автосервисе. Данные хранятся в SQLite через Entity Framework Core.

## Что нужно для запуска

- Windows 10 или 11.
- [.NET 10 SDK](https://dotnet.microsoft.com/download).
- Visual Studio 2026, или Visual Studio 2022 версии 17.14 и новее, с рабочей нагрузкой «Разработка классических приложений .NET».

## Как запустить

1. Откройте `AutoService.sln` в Visual Studio.
2. Назначьте `AutoService.WinForms` запускаемым проектом: правой кнопкой по проекту → «Назначить запускаемым проектом».
3. Нажмите F5. При первой сборке NuGet-пакеты скачаются автоматически.

При первом запуске рядом с программой создаётся файл базы `autoservice.db` с демонстрационными данными: прайс-лист, склад, сотрудники, 3 клиента и 3 заявки в разных статусах. Чтобы начать с чистыми демонстрационными данными, удалите `autoservice.db` из папки `bin\Debug\net10.0-windows`.

Запуск из командной строки:

```
dotnet run --project src/AutoService.WinForms
dotnet test
```

## Структура решения

```
AutoService.sln
├── src/
│   ├── AutoService.Domain      модель предметной области: классы, правила, исключения, интерфейсы
│   ├── AutoService.Services    сценарии: заявки, клиенты, справочники, отчёты
│   ├── AutoService.Data        EF Core + SQLite: контекст, настройки таблиц, репозитории
│   └── AutoService.WinForms    формы и запуск приложения
└── tests/
    └── AutoService.Tests       модульные тесты xUnit (домен и сервисы)
```

Domain ни от чего не зависит. Data реализует интерфейсы из Domain, Services работает только с интерфейсами, а WinForms собирает всё вместе через внедрение зависимостей в `Program.cs`.

## Где в коде принципы ООП

| Принцип | Где посмотреть |
|---|---|
| Инкапсуляция | `RepairRequest`: статус и списки работ меняются только методами, которые проверяют правила; `SparePart.WriteOff` |
| Наследование | `Person → Client, Employee → Mechanic, Receptionist`; `Payment → CashPayment, CardPayment` |
| Абстракция | абстрактные `Person`, `Employee`, `Payment`; интерфейсы `IRepository<T>`, `IUnitOfWork` |
| Полиморфизм | `Employee.Position`, `Payment.Process()`, `Payment.GetReceiptLine()`, `IDiscountStrategy` |
| Композиция | заявка содержит `WorkItem` и `PartItem`, создать их можно только через методы заявки |
| Исключения | `DomainException → InvalidStatusTransitionException, NotEnoughStockException` |
