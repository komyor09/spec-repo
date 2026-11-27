using PrintShop.Core.Models;
using PrintShop.Core.Models.Products;
using PrintShop.Core.Services;

namespace PrintShop.Core.Data;

/// <summary>Заполнение новой базы данных начальными справочными и демонстрационными данными.</summary>
public static class DataSeeder
{
    public static void Seed(AppServices app, bool withDemoOrders = true)
    {
        var db = app.Database;
        using (var s = db.Open())
        {
            if (Convert.ToInt32(s.Scalar("SELECT COUNT(*) FROM Users")) > 0) return; // уже заполнено
        }

        db.InTransaction(s =>
        {
            // Пользователи (пароль = логин)
            foreach (var (login, name, role) in new[]
                     {
                         ("admin", "Администратор системы", UserRole.Admin),
                         ("manager", "Каримова Нилуфар", UserRole.Manager),
                         ("printer", "Рахимов Фаррух", UserRole.Printer)
                     })
                app.Users.Add(new User { Login = login, FullName = name, Role = role, PasswordHash = AuthService.HashPassword(login) }, s);

            // Виды продукции (прайс-лист)
            app.ProductTypes.Add(new ProductType { Name = "Визитки", Kind = ProductKind.BusinessCard, BasePrice = 0.30m, Unit = "шт" }, s);
            app.ProductTypes.Add(new ProductType { Name = "Листовки", Kind = ProductKind.Flyer, BasePrice = 0.80m, Unit = "шт" }, s);
            app.ProductTypes.Add(new ProductType { Name = "Буклеты", Kind = ProductKind.Booklet, BasePrice = 0.40m, Unit = "шт" }, s);
            app.ProductTypes.Add(new ProductType { Name = "Плакаты", Kind = ProductKind.Poster, BasePrice = 2.50m, Unit = "шт" }, s);
            app.ProductTypes.Add(new ProductType { Name = "Баннеры", Kind = ProductKind.Banner, BasePrice = 45m, Unit = "шт" }, s);
            app.ProductTypes.Add(new ProductType { Name = "Наклейки", Kind = ProductKind.Sticker, BasePrice = 0.90m, Unit = "шт" }, s);

            // Материалы
            app.Materials.Add(new Material { Name = "Бумага мелованная матовая", Density = 300, Format = PaperFormats.A3, Price = 3.50m, Quantity = 2000, MinQuantity = 300 }, s);
            app.Materials.Add(new Material { Name = "Бумага мелованная глянцевая", Density = 130, Format = PaperFormats.A3, Price = 1.80m, Quantity = 3000, MinQuantity = 500 }, s);
            app.Materials.Add(new Material { Name = "Бумага офсетная", Density = 80, Format = PaperFormats.A4, Price = 0.25m, Quantity = 10000, MinQuantity = 2000 }, s);
            app.Materials.Add(new Material { Name = "Бумага мелованная", Density = 170, Format = PaperFormats.A2, Price = 4.20m, Quantity = 400, MinQuantity = 100 }, s);
            app.Materials.Add(new Material { Name = "Самоклеящаяся бумага", Density = 80, Format = PaperFormats.A3, Price = 4.00m, Quantity = 150, MinQuantity = 200 }, s);
            app.Materials.Add(new Material { Name = "Баннерная ткань литая", Density = 440, Format = PaperFormats.Roll, Price = 25m, Quantity = 120, MinQuantity = 30 }, s);
            app.Materials.Add(new Material { Name = "Фотобумага", Density = 200, Format = PaperFormats.A1, Price = 12m, Quantity = 80, MinQuantity = 20 }, s);

            // Дополнительные услуги
            app.Services.Add(new AdditionalService { Name = "Ламинирование", Price = 0.40m, ChargeType = ServiceChargeType.PerUnit }, s);
            app.Services.Add(new AdditionalService { Name = "Резка", Price = 15m, ChargeType = ServiceChargeType.PerItem }, s);
            app.Services.Add(new AdditionalService { Name = "Переплёт (скоба)", Price = 0.50m, ChargeType = ServiceChargeType.PerUnit }, s);
            app.Services.Add(new AdditionalService { Name = "Скругление углов", Price = 0.05m, ChargeType = ServiceChargeType.PerUnit }, s);
            app.Services.Add(new AdditionalService { Name = "Люверсы (баннер)", Price = 20m, ChargeType = ServiceChargeType.PerItem }, s);
            app.Services.Add(new AdditionalService { Name = "Дизайн макета", Price = 100m, ChargeType = ServiceChargeType.PerItem }, s);

            // Клиенты
            app.Customers.Add(new PersonCustomer { Name = "Иванов Алишер Рустамович", Phone = "+992 92 111 22 33", Email = "alisher@mail.tj", Address = "г. Худжанд, ул. Ленина, 15" }, s);
            app.Customers.Add(new CompanyCustomer { Name = "ООО «Согд Трейд»", Phone = "+992 3422 6 45 78", Email = "info@sogdtrade.tj", Address = "г. Худжанд, пр. Исмоили Сомони, 40" }, s);
            app.Customers.Add(new PersonCustomer { Name = "Назарова Мадина", Phone = "+992 93 555 44 11", Address = "г. Бустон" }, s);
            app.Customers.Add(new CompanyCustomer { Name = "Кафе «Шарк»", Phone = "+992 90 700 80 90", Email = "shark.cafe@gmail.com", Address = "г. Худжанд, 12 мкр." }, s);
        });

        if (!withDemoOrders) return;

        // Демонстрационные заказы создаются через сервисы — так проверяется вся цепочка бизнес-логики
        var admin = app.Auth.Login("admin", "admin");
        try
        {
            var types = app.ProductTypes.GetAll().ToDictionary(t => t.Kind);
            var materials = app.Materials.GetAll();
            var services = app.Services.GetAll();
            var customers = app.Customers.GetAll();
            Material Mat(string name, int density) => materials.First(m => m.Name == name && m.Density == density);
            AdditionalService Svc(string name) => services.First(x => x.Name.StartsWith(name));

            // 1. Визитки для компании — выдан
            var o1 = app.Orders.CreateDraft();
            o1.CreatedAt = DateTime.Now.AddDays(-10);
            o1.DueDate = DateTime.Today.AddDays(-7);
            o1.Customer = customers.First(c => c.Name.Contains("Согд"));
            var bc = new BusinessCard(types[ProductKind.BusinessCard], Mat("Бумага мелованная матовая", 300)) { Quantity = 1000, Color = ColorMode.ColorTwoSide };
            bc.AddService(Svc("Ламинирование"));
            bc.AddService(Svc("Резка"));
            o1.AddItem(new OrderItem(bc));
            app.Orders.Save(o1);
            app.Orders.AddPayment(o1, Math.Round(o1.TotalPrice / 2, 2), PaymentType.Prepayment);
            app.Orders.ChangeStatus(o1, OrderStatus.LayoutApproval);
            app.Orders.ChangeStatus(o1, OrderStatus.Printing);
            app.Orders.ChangeStatus(o1, OrderStatus.PostProcessing);
            app.Orders.ChangeStatus(o1, OrderStatus.Ready);
            app.Orders.AddPayment(o1, o1.Debt, PaymentType.Final);
            app.Orders.ChangeStatus(o1, OrderStatus.Issued);

            // 2. Листовки и баннер для кафе — в печати, просрочен
            var o2 = app.Orders.CreateDraft();
            o2.CreatedAt = DateTime.Now.AddDays(-4);
            o2.DueDate = DateTime.Today.AddDays(-1);
            o2.Customer = customers.First(c => c.Name.Contains("Шарк"));
            o2.Comment = "Меню-листовки и баннер на фасад";
            o2.AddItem(new OrderItem(new Flyer(types[ProductKind.Flyer], Mat("Бумага мелованная глянцевая", 130)) { Format = PaperFormats.A5, Quantity = 500, Color = ColorMode.ColorTwoSide }));
            var banner = new Banner(types[ProductKind.Banner], Mat("Баннерная ткань литая", 440)) { Width = 3m, Height = 1m, Quantity = 1 };
            banner.AddService(Svc("Люверсы"));
            o2.AddItem(new OrderItem(banner));
            app.Orders.Save(o2);
            app.Orders.AddPayment(o2, 300m, PaymentType.Prepayment);
            app.Orders.ChangeStatus(o2, OrderStatus.LayoutApproval);
            app.Orders.ChangeStatus(o2, OrderStatus.Printing);

            // 3. Буклеты — срок сегодня, согласование макета
            var o3 = app.Orders.CreateDraft();
            o3.CreatedAt = DateTime.Now.AddDays(-2);
            o3.DueDate = DateTime.Today;
            o3.Customer = customers.First(c => c.Name.Contains("Иванов"));
            var booklet = new Booklet(types[ProductKind.Booklet], Mat("Бумага мелованная глянцевая", 130)) { Format = PaperFormats.A5, PageCount = 12, Quantity = 100 };
            booklet.AddService(Svc("Переплёт"));
            o3.AddItem(new OrderItem(booklet));
            app.Orders.Save(o3);
            app.Orders.ChangeStatus(o3, OrderStatus.LayoutApproval);

            // 4. Наклейки и плакаты — новый
            var o4 = app.Orders.CreateDraft();
            o4.DueDate = DateTime.Today.AddDays(5);
            o4.Customer = customers.First(c => c.Name.Contains("Назарова"));
            o4.AddItem(new OrderItem(new Sticker(types[ProductKind.Sticker], Mat("Самоклеящаяся бумага", 80)) { Format = PaperFormats.A6, Quantity = 200, DieCut = true }));
            o4.AddItem(new OrderItem(new Poster(types[ProductKind.Poster], Mat("Бумага мелованная", 170)) { Format = PaperFormats.A2, Quantity = 5 }));
            app.Orders.Save(o4);
        }
        finally
        {
            app.Auth.Logout();
        }
    }
}
