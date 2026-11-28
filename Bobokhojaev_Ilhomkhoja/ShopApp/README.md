# ShopApp — интернет-магазин (корзина и заказы)

Учебный проект: **бэкенд** на ASP.NET Core 8 (Web API) + Entity Framework Core,
**фронтенд** на чистых HTML / CSS / JavaScript (без фреймворков).
Фронтенд отдаётся самим ASP.NET-приложением из папки `wwwroot`.

## Возможности

**Покупатель**
- каталог с категориями, поиском, сортировкой и пагинацией;
- карточка товара (модальное окно) с выбором количества;
- корзина: добавление, изменение количества, удаление, очистка; контроль остатков;
- оформление заказа (имя, телефон, e-mail, адрес, комментарий) с валидацией;
- «Мои заказы»: история, статусы, отмена нового заказа.

**Администратор** (`/admin.html`, ключ по умолчанию — `admin123`)
- список всех заказов с фильтром по статусу, смена статуса;
- товары: добавление, редактирование цены/остатка/описания, скрытие из каталога.

## Запуск

Нужен [.NET 8 SDK](https://dotnet.microsoft.com/download) (подойдёт и .NET 9/10 — см. ниже).

```bash
cd ShopApp
dotnet restore
dotnet run
```

Открыть в браузере: **http://localhost:5000**

При первом запуске автоматически создаётся файл БД `shop.db` (SQLite) с тестовыми
категориями и товарами. Чтобы начать «с нуля», остановите приложение и удалите `shop.db`.

> Если у вас установлен только .NET 9 или 10 — поменяйте в `ShopApp.csproj`
> `net8.0` на `net9.0`/`net10.0` и версии пакетов EF Core на соответствующие (9.x / 10.x).

Можно открыть проект в Visual Studio / Rider (`ShopApp.csproj`) и запускать оттуда.
В файле `ShopApp.http` лежат готовые запросы к API для проверки.

## Структура

```
ShopApp/
├── Program.cs                  — настройка DI, EF Core, статики, middleware
├── appsettings.json            — строка подключения, ключ админа
├── Models/                     — сущности EF: Category, Product, Cart, CartItem, Order, OrderItem
├── Data/
│   ├── ShopDbContext.cs        — DbContext + Fluent API (ключи, связи, индексы)
│   └── SeedData.cs             — начальные данные (HasData)
├── Dtos/                       — DTO для API + маппинг сущность → DTO
├── Services/                   — бизнес-логика: ProductService, CartService, OrderService
├── Controllers/                — REST API
├── Infrastructure/             — обработка ошибок, ключ админа, статусы заказа
└── wwwroot/                    — фронтенд
    ├── index.html  cart.html  orders.html  admin.html
    ├── css/style.css
    └── js/  api.js  common.js  catalog.js  cart.js  orders.js  admin.js
```

## База данных

```
Categories (Id, Name, Icon)
Products   (Id, Name, Description, Price, Stock, ImageUrl, IsActive, CreatedAt, CategoryId → Categories)
Carts      (Id, ClientId [unique], UpdatedAt)
CartItems  (Id, CartId → Carts, ProductId → Products, Quantity)   unique(CartId, ProductId)
Orders     (Id, ClientId, CustomerName, Phone, Email, Address, Comment, Status, CreatedAt, Total)
OrderItems (Id, OrderId → Orders, ProductId → Products [SET NULL], ProductName, UnitPrice, Quantity)
```

Ключевые решения:
- **Покупатель без регистрации.** Браузер генерирует GUID (`ClientId`), хранит его в `localStorage`
  и передаёт в заголовке `X-Client-Id`. По нему сервер находит корзину и заказы.
- **«Снимок» цены в заказе.** `OrderItem` хранит название и цену на момент покупки —
  изменение каталога не меняет старые заказы.
- **Транзакция при оформлении.** Проверка остатков, списание со склада, создание заказа
  и очистка корзины выполняются атомарно. При отмене заказа товары возвращаются на склад.
- **Мягкое удаление товаров** (`IsActive`), чтобы не ломать историю заказов.
- **Статусы заказа:** `New → Processing → Shipped → Delivered`; отменить можно из `New`
  и `Processing` (покупатель — только из `New`). Правила — в `Infrastructure/OrderWorkflow.cs`.

## API

| Метод | URL | Описание |
|---|---|---|
| GET | `/api/categories` | категории |
| GET | `/api/products?categoryId=&search=&sort=&page=&pageSize=` | товары (sort: `new`, `price_asc`, `price_desc`, `name`) |
| GET | `/api/products/{id}` | товар |
| GET | `/api/cart` | корзина* |
| POST | `/api/cart/items` `{ productId, quantity }` | добавить в корзину* |
| PUT | `/api/cart/items/{productId}` `{ quantity }` | изменить количество (0 — удалить)* |
| DELETE | `/api/cart/items/{productId}` | удалить позицию* |
| DELETE | `/api/cart` | очистить корзину* |
| POST | `/api/orders` `{ customerName, phone, email, address, comment }` | оформить заказ* |
| GET | `/api/orders` | мои заказы* |
| GET | `/api/orders/{id}` | мой заказ* |
| POST | `/api/orders/{id}/cancel` | отменить заказ* |
| GET | `/api/admin/orders?status=` | все заказы** |
| PUT | `/api/admin/orders/{id}/status` `{ status }` | сменить статус** |
| GET / POST / PUT | `/api/admin/products[/{id}]` | управление товарами** |

\* нужен заголовок `X-Client-Id: <guid>` · \*\* нужен заголовок `X-Admin-Key: admin123`

Ошибки возвращаются в виде `{ "error": "текст" }` (или стандартный `ValidationProblemDetails`
с полем `errors` при ошибках валидации).

## Переход на миграции EF Core

Сейчас БД создаётся через `EnsureCreated()` — это проще для старта. Для миграций:

```bash
dotnet tool install --global dotnet-ef
# удалить shop.db, затем:
dotnet ef migrations add InitialCreate
```

И в `Program.cs` заменить `db.Database.EnsureCreated();` на `db.Database.Migrate();`.

## Переход на MySQL / SQL Server

1. Заменить пакет `Microsoft.EntityFrameworkCore.Sqlite` на
   `Pomelo.EntityFrameworkCore.MySql` или `Microsoft.EntityFrameworkCore.SqlServer`.
2. В `Program.cs` заменить `UseSqlite(...)` на `UseMySql(cs, ServerVersion.AutoDetect(cs))` или `UseSqlServer(cs)`.
3. Поменять строку подключения в `appsettings.json`.
4. (Необязательно) в `ProductService.SearchAsync` перенести поиск и сортировку в SQL-запрос —
   в комментарии там объяснено, почему для SQLite это сделано в памяти.
