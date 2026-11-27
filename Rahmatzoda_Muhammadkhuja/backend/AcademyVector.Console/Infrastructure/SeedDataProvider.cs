using AcademyVector.ConsoleApp.Application.Interfaces;
using AcademyVector.ConsoleApp.Domain.Entities;

namespace AcademyVector.ConsoleApp.Infrastructure;

public sealed class SeedDataProvider : ISeedDataProvider
{
    private static LessonSection Sec(string heading, string body) =>
        new() { Heading = heading, Body = body };

    public PlatformDatabase CreateSeedDatabase()
    {
        var db = new PlatformDatabase();

        db.Instructors.AddRange(new[]
        {
            new Instructor(
                "inst-alexey",
                "Алексей Волков",
                "a.volkov@vector-academy.ru",
                "Ведущий архитектор .NET и преподаватель ООП",
                "12 лет промышленной разработки на C# и .NET, проектирование высоконагруженных финтех-сервисов, распределённых очередей и чистых доменных моделей.",
                "https://cdn.vector-academy.ru/images/instructor_alexey_1795754089726.jpg",
                "C# 12 · .NET 8 · Clean Architecture",
                12,
                "МГТУ им. Н. Э. Баумана, факультет «Информатика и системы управления»",
                new()
                {
                    "Principal .NET Architect в финтех-платформе «НордПэй» (2022–н.в.)",
                    "Lead Backend Engineer в «ТехноСфера» (2018–2022)",
                    "Senior C# Developer в банковском процессинге (2014–2018)"
                },
                new()
                {
                    "Спикер конференций DotNext и HighLoad++",
                    "Выпустил более 1 200 разработчиков на платформе C# / .NET",
                    "Автор корпоративного стандарта доменного моделирования и DDD"
                },
                new() { "C# 12", ".NET 8", "ASP.NET Core", "PostgreSQL", "Domain-Driven Design" },
                "2026-06-10T09:00:00.000Z"),
            new Instructor(
                "inst-elena",
                "Елена Соколова",
                "e.sokolova@vector-academy.ru",
                "Дизайн-директор продуктовых систем",
                "Руководила дизайн-системами в крупных SaaS- и B2B-продуктах. Специализируется на интерфейсной типографике, сетках и токенах доступности.",
                "https://cdn.vector-academy.ru/images/instructor_elena_1795754090761.jpg",
                "UI/UX · Дизайн-системы · Прототипирование",
                9,
                "НИУ ВШЭ, Школа дизайна — направление «Цифровой продукт»",
                new()
                {
                    "Design Director в облачной платформе «КонтурПро» (2023–н.в.)",
                    "Head of Design Systems в «МаркетПлюс» (2020–2023)",
                    "Senior Product Designer в продуктовой студии (2017–2020)"
                },
                new()
                {
                    "Создала мультибрендовую дизайн-систему на 140+ компонентов",
                    "Победитель премии Tagline Awards в номинации «Лучший B2B-интерфейс»",
                    "Наставник 30+ продуктовых дизайнеров уровня Middle и Senior"
                },
                new() { "Figma", "Design Tokens", "WCAG AA", "Типографика", "Прототипирование" },
                "2026-06-12T09:00:00.000Z"),
            new Instructor(
                "inst-dmitry",
                "Дмитрий Морозов",
                "d.morozov@vector-academy.ru",
                "Руководитель аналитики и продуктовой стратегии",
                "Эксперт по юнит-экономике, SQL-аналитике, статистическим A/B-экспериментам и прикладной науке о данных на Python.",
                "https://cdn.vector-academy.ru/images/instructor_dmitry_1795754090089.jpg",
                "SQL · Python · Продуктовая аналитика",
                10,
                "МФТИ, факультет управления и прикладной математики",
                new()
                {
                    "Head of Analytics в подписочном сервисе «СтримМедиа» (2021–н.в.)",
                    "Lead Product Analyst в «Логистика Онлайн» (2018–2021)",
                    "Data Analyst в телеком-операторе (2016–2018)"
                },
                new()
                {
                    "Запустил платформу автоматического расчёта A/B-тестов на 400+ экспериментов в год",
                    "Сократил время подготовки когортной отчётности в 6 раз через SQL-витрины",
                    "Автор практикума по статистике для продакт-менеджеров"
                },
                new() { "PostgreSQL", "Python", "Pandas", "A/B-тесты", "RICE & Unit Economics" },
                "2026-06-15T09:00:00.000Z"),
            new Instructor(
                "inst-irina",
                "Ирина Белова",
                "i.belova@vector-academy.ru",
                "Директор по маркетингу и росту (CMO)",
                "Выстраивает системный перфоманс-маркетинг, сквозную аналитику и продуктовые воронки для IT- и EdTech-компаний.",
                "https://cdn.vector-academy.ru/images/instructor_irina_1795756538021.jpg",
                "Growth · Юнит-экономика · Контент-стратегия",
                8,
                "СПбГУ, Высшая школа менеджмента — «Маркетинговая аналитика»",
                new()
                {
                    "CMO в B2B SaaS-сервисе «ВекторТрек» (2023–н.в.)",
                    "Head of Performance в агентстве «МедиаЛаб» (2020–2023)",
                    "Product Marketing Manager в IT-стартапе (2018–2020)"
                },
                new()
                {
                    "Вывела на окупаемость за 4 месяца 12 образовательных продуктов",
                    "Снизила CAC на 38% за счёт переработки контент-воронок",
                    "Провела 60+ живых разборов маркетинговых стратегий студентов"
                },
                new() { "LTV / CAC", "Сквозная аналитика", "CRM-маркетинг", "CJM", "JTBD" },
                "2026-06-18T09:00:00.000Z")
        });

        db.Groups.AddRange(new[]
        {
            new StudyGroup("grp-dotnet-26", "ПИ-26-1 · Разработка на C#", "inst-alexey", "Основной поток по объектно-ориентированному проектированию и .NET 8.", "2026-09-01T08:00:00.000Z"),
            new StudyGroup("grp-arch-26", "АРХ-26-2 · .NET Архитектура", "inst-alexey", "Углублённый поток по Clean Architecture, микросервисам и паттернам.", "2026-09-01T08:00:00.000Z"),
            new StudyGroup("grp-design-26", "ДС-26-3 · Продуктовый дизайн", "inst-elena", "Поток проектирования пользовательских интерфейсов и компонентных библиотек.", "2026-09-01T08:00:00.000Z"),
            new StudyGroup("grp-analytics-26", "АН-26-4 · Аналитика и рост", "inst-dmitry", "Группа по SQL-аналитике, Python, когортному анализу и маркетингу.", "2026-09-01T08:00:00.000Z")
        });

        db.Students.AddRange(new[]
        {
            new Student("stud-ivan", "Иван Петров", "i.petrov@student.vector.ru", "grp-dotnet-26", "ИП", 460, "2026-09-02T10:00:00.000Z"),
            new Student("stud-maria", "Мария Кузнецова", "m.kuznetsova@student.vector.ru", "grp-design-26", "МК", 390, "2026-09-03T10:00:00.000Z"),
            new Student("stud-artem", "Артём Смирнов", "a.smirnov@student.vector.ru", "grp-dotnet-26", "АС", 245, "2026-09-05T10:00:00.000Z"),
            new Student("stud-olga", "Ольга Новикова", "o.novikova@student.vector.ru", "grp-analytics-26", "ОН", 410, "2026-09-07T10:00:00.000Z"),
            new Student("stud-nikita", "Никита Лебедев", "n.lebedev@student.vector.ru", "grp-analytics-26", "НЛ", 165, "2026-09-10T10:00:00.000Z"),
            new Student("stud-ekaterina", "Екатерина Орлова", "e.orlova@student.vector.ru", "grp-arch-26", "ЕО", 520, "2026-09-11T10:00:00.000Z"),
            new Student("stud-sergey", "Сергей Захаров", "s.zakharov@student.vector.ru", "grp-arch-26", "СЗ", 315, "2026-09-12T10:00:00.000Z"),
            new Student("stud-anna", "Анна Фёдорова", "a.fedorova@student.vector.ru", "grp-design-26", "АФ", 280, "2026-09-14T10:00:00.000Z"),
            new Student("stud-maxim", "Максим Егоров", "m.egorov@student.vector.ru", "grp-dotnet-26", "МЕ", 195, "2026-09-16T10:00:00.000Z"),
            new Student("stud-daria", "Дарья Степанова", "d.stepanova@student.vector.ru", "grp-analytics-26", "ДС", 340, "2026-09-18T10:00:00.000Z"),
            new Student("stud-pavel", "Павел Николаев", "p.nikolaev@student.vector.ru", "grp-arch-26", "ПН", 150, "2026-09-20T10:00:00.000Z"),
            new Student("stud-alina", "Алина Козлова", "a.kozlova@student.vector.ru", "grp-design-26", "АК", 230, "2026-09-21T10:00:00.000Z")
        });

        // 1. C# ООП
        var csharpCourse = new Course(
            "course-csharp-oop",
            "Объектно-ориентированное программирование на C# (.NET 8)",
            "csharp-oop-dotnet-8",
            "IT и разработка",
            "Средний",
            "Глубокая практика принципов ООП: инкапсуляция, наследование, полиморфизм, SOLID, доменные исключения и внедрение зависимостей в ASP.NET Core.",
            "https://cdn.vector-academy.ru/images/course_csharp_oop_1795754087741.jpg",
            "inst-alexey",
            14900,
            36,
            4.9,
            412,
            "2026-07-01T09:00:00.000Z",
            "Профессия · 5 уроков");

        var csMod1 = new CourseModule("mod-cs-1", "Модуль 1. Инкапсуляция и доменные модели", 1);
        csMod1.AddLesson(new LessonActivity(
            "les-cs-1",
            "Инкапсуляция и защита инвариантов сущности",
            1,
            28,
            "Почему публичные сеттеры разрушают доменную модель и как проектировать безопасные методы изменения состояния.",
            "Инкапсуляция в C# — это не формальное сокрытие полей за свойствами get/set. Главная задача доменной сущности — гарантировать валидность собственного состояния в любой момент жизни объекта.",
            "public abstract class DomainEntity\n{\n    public string Id { get; protected set; }\n    public abstract string Describe();\n}",
            "Состояние объекта должно изменяться только через осмысленные доменные методы с проверкой инвариантов.",
            new() { "Отличать анемичную модель данных от богатой доменной модели", "Защищать свойства через private set и protected set с [JsonInclude]" },
            new()
            {
                Sec("1. Проблема публичных сеттеров", "Когда любое свойство можно изменить напрямую из контроллера, бизнес-логика расползается по всему проекту."),
                Sec("2. Защищённые сеттеры и доменные методы", "Все изменения состояния оформляются как осмысленные действия: MarkLessonCompleted(), Review() или Confirm()."),
                Sec("3. Базовый класс DomainEntity", "Вынесите общие идентификаторы, дату создания и базовую проверку инвариантов в абстрактный класс DomainEntity.")
            },
            new() { "Все изменяемые свойства сущности имеют private или protected сеттер", "Некорректные аргументы сразу выбрасывают ValidationDomainException" }));
        csMod1.AddLesson(new LessonActivity(
            "les-cs-2",
            "Абстракция и иерархия пользователей",
            2,
            34,
            "Проектирование абстрактного класса UserBase и специализированных ролей Student и Instructor.",
            "Абстрактный базовый класс фиксирует общий контракт и переиспользуемую логику, не позволяя создавать неполные экземпляры.",
            "public abstract class UserBase : DomainEntity\n{\n    public string FullName { get; protected set; }\n    public abstract string GetRoleLabel();\n}",
            "Выделяйте общее состояние и проверки в абстрактный базовый класс, оставляя ролевые различия наследникам.",
            new() { "Выделять общее поведение в абстрактный класс UserBase", "Применять sealed к конечным доменным сущностям" },
            new()
            {
                Sec("1. Зачем запрещать прямое создание UserBase", "Помечая UserBase ключевым словом abstract, вы защищаете систему от неполных учётных записей."),
                Sec("2. Наследники Student и Instructor", "Student инкапсулирует группу и баллы опыта, а Instructor хранит специализацию и резюме.")
            },
            new() { "Проверка email и имени выполняется один раз в конструкторе UserBase" }));
        csMod1.AddLesson(new LessonActivity(
            "les-cs-3",
            "Иерархия доменных исключений (400 / 404 / 409)",
            3,
            30,
            "Отделение ошибок бизнес-логики от транспортного уровня HTTP с помощью DomainException.",
            "Бизнес-сервисы не должны знать про HTTP-контекст и контроллеры. Выбрасывайте типизированные доменные исключения.",
            "public sealed class ConflictDomainException : DomainException\n{\n    public override int StatusCode => 409;\n}",
            "Централизованный перехват доменных исключений делает код сервисов чистым и предсказуемым.",
            new() { "Построить иерархию DomainException -> Validation / NotFound / Conflict", "Маппить исключения в ASP.NET Core Middleware" },
            new()
            {
                Sec("1. Проблема смешивания HTTP и домена", "Доменные исключения позволяют переиспользовать бизнес-сервисы в любой среде."),
                Sec("2. Три базовых сценария сбоя", "400 — ошибка ввода, 404 — сущность не найдена, 409 — конфликт расписания или повторная запись.")
            },
            new() { "Middleware перехватывает DomainException до 500 Internal Server Error" }));
        csMod1.AddQuiz(new QuizActivity(
            "quiz-cs-1",
            "Проверка понимания: Инкапсуляция и абстракция",
            60,
            120,
            new List<QuizQuestion>
            {
                new() { Id = "q-cs-1-1", Text = "Какой модификатор доступа лучше использовать для сеттера свойства, изменяемого только внутри класса и его наследников?", Options = new() { "public set;", "protected set;", "internal set;" }, CorrectIndex = 1, Explanation = "protected set; разрешает изменение свойства внутри самого класса и производных классов, защищая инвариант извне." },
                new() { Id = "q-cs-1-2", Text = "Можно ли создать экземпляр абстрактного класса UserBase напрямую через оператор new?", Options = new() { "Да, если у него есть конструктор", "Нет, можно инстанцировать только его неабстрактного наследника", "Только через static-метод" }, CorrectIndex = 1, Explanation = "Абстрактный класс служит базовым контрактом; оператор new применим только к конкретным наследникам." }
            }));
        csMod1.AddAssignment(new AssignmentActivity("hw-cs-1", "Иерархия учебной активности в C#", "Реализуйте абстрактный класс LearningActivityBase и наследников LessonActivity, QuizActivity и WebinarActivity с полиморфным методом Describe().", 100));

        var csMod2 = new CourseModule("mod-cs-2", "Модуль 2. Полиморфизм и внедрение зависимостей", 2);
        csMod2.AddLesson(new LessonActivity(
            "les-cs-4",
            "Полиморфизм и единый диспетчер сводки",
            1,
            32,
            "Единая обработка разнородных объектов через общий базовый тип и переопределение virtual/abstract методов.",
            "Полиморфизм позволяет работать со списком базового типа IEnumerable<LearningActivityBase> без проверок типов if/switch.",
            "public override string Describe() => $\"Живой вебинар «{Title}»\";",
            "Полиморфизм устраняет громоздкие условные ветвления по типу объекта.",
            new() { "Заменять ветвления if/switch на полиморфные вызовы", "Реализовывать единый эндпоинт агрегации сущностей" },
            new()
            {
                Sec("1. Позднее связывание в .NET CLR", "Среда выполнения выбирает конкретную реализацию Describe() в зависимости от фактического класса объекта."),
                Sec("2. Единый эндпоинт /api/platform/oop-summary", "Маршрут собирает UserBase и LearningActivityBase в общие коллекции.")
            },
            new() { "Каждый наследник LearningActivityBase переопределяет Describe()" }));
        csMod2.AddLesson(new LessonActivity(
            "les-cs-5",
            "DI-контейнер и тестируемость в ASP.NET Core",
            2,
            38,
            "Регистрация сервисов через интерфейсы и разделение ответственности (Dependency Inversion).",
            "Зависимость эндпоинтов от интерфейсов IPlatformService, ICourseDbRepository и IWebinarScheduleValidator делает бизнес-логику модульной.",
            "builder.Services.AddSingleton<ICourseDbRepository, JsonCourseDbRepository>();\nbuilder.Services.AddScoped<IPlatformService, PlatformService>();",
            "Бизнес-логика должна зависеть от абстракций, а не от конкретных деталей хранения данных.",
            new() { "Понимать разницу между Singleton, Scoped и Transient", "Организовывать потокобезопасное хранилище JSON через SemaphoreSlim" },
            new()
            {
                Sec("1. Регистрация в Program.cs", "Репозиторий регистрируется как Singleton для совместного доступа к SemaphoreSlim."),
                Sec("2. Изоляция валидатора вебинаров", "Выделение IWebinarScheduleValidator позволяет независимо тестировать конфликты 409.")
            },
            new() { "Program.cs регистрирует сервисы через интерфейсы" }));
        csMod2.AddQuiz(new QuizActivity("quiz-cs-2", "Итоговый тест по полиморфизму и DI", 60, 140, new List<QuizQuestion>
        {
            new() { Id = "q-cs-2-1", Text = "Какой жизненный цикл в ASP.NET Core DI лучше выбрать для JSON-репозитория с SemaphoreSlim?", Options = new() { "Transient", "Singleton", "Scoped" }, CorrectIndex = 1, Explanation = "Singleton гарантирует общий экземпляр SemaphoreSlim для всех параллельных HTTP-запросов." }
        }));
        csMod2.AddAssignment(new AssignmentActivity("hw-cs-2", "Валидатор расписания через DI", "Реализуйте интерфейс IWebinarScheduleValidator и подключите его к сервису платформы через конструктор.", 100));

        csharpCourse.AddModule(csMod1);
        csharpCourse.AddModule(csMod2);

        // 2. Clean Architecture
        var archCourse = new Course(
            "course-dotnet-arch",
            "Архитектура ASP.NET Core и Clean Architecture",
            "aspnet-core-clean-architecture",
            "IT и разработка",
            "Продвинутый",
            "Проектирование многослойных .NET-приложений: разделение Domain, Application и Infrastructure, CQRS-подход, интеграционные тесты Minimal API.",
            "https://cdn.vector-academy.ru/images/course_dotnet_architecture_1795756537431.jpg",
            "inst-alexey",
            19900,
            42,
            4.9,
            168,
            "2026-07-05T09:00:00.000Z",
            "Интенсив · 3 урока");
        var archMod1 = new CourseModule("mod-arch-1", "Модуль 1. Границы слоёв и правило зависимостей", 1);
        archMod1.AddLesson(new LessonActivity(
            "les-arch-1",
            "Правило зависимостей: Domain в центре системы",
            1,
            35,
            "Почему доменный слой никогда не ссылается на инфраструктуру и базу данных.",
            "Чистая архитектура ставит бизнес-правила в центр приложения. База данных и веб-фреймворк — подключаемые детали.",
            "public interface ICourseDbRepository { Task<PlatformDatabase> ReadAsync(CancellationToken ct = default); }",
            "Направление зависимостей всегда идёт снаружи внутрь — к домену.",
            new() { "Строить правильный граф зависимостей проектов", "Изолировать DTO-контракты от доменных сущностей" },
            new() { Sec("1. Инверсия зависимостей", "Слой Application объявляет интерфейс ICourseDbRepository, а Infrastructure его реализует.") },
            new() { "Domain не содержит ссылок на файловую систему" }));
        archMod1.AddLesson(new LessonActivity(
            "les-arch-2",
            "Minimal API и группировка маршрутов в .NET 8",
            2,
            30,
            "Организация REST-эндпоинтов без громоздких контроллеров.",
            "Minimal API в .NET 8 обеспечивает высокую производительность и лаконичную привязку сервисов из DI.",
            "var api = app.MapGroup(\"/api\");\napi.MapGet(\"/health\", () => Results.Ok(new { status = \"ok\" }));",
            "Minimal API снижает количество шаблонного кода при сохранении строгой типизации.",
            new() { "Использовать MapGroup для префикса /api" },
            new() { Sec("1. Группы маршрутов", "Вызов app.MapGroup(\"/api\") централизованно применяет политики к эндпоинтам.") },
            new() { "Все маршруты используют явные HTTP-методы" }));
        archMod1.AddQuiz(new QuizActivity("quiz-arch-1", "Проверка: Clean Architecture", 50, 130, new List<QuizQuestion>
        {
            new() { Id = "q-arch-1-1", Text = "На какой слой должен ссылаться слой Infrastructure в чистой архитектуре?", Options = new() { "На Application и Domain", "Ни на один из них", "Только на UI" }, CorrectIndex = 0, Explanation = "Infrastructure реализует интерфейсы Application и работает с сущностями Domain." }
        }));
        archMod1.AddAssignment(new AssignmentActivity("hw-arch-1", "Схема слоёв вашего веб-проекта", "Опишите распределение классов вашей системы по слоям Domain, Application и Infrastructure.", 100));
        var archMod2 = new CourseModule("mod-arch-2", "Модуль 2. Надёжность хранения данных", 2);
        archMod2.AddLesson(new LessonActivity(
            "les-arch-3",
            "Атомарная запись файлов и блокировки SemaphoreSlim",
            1,
            28,
            "Как избежать повреждения JSON-базы при одновременных запросах.",
            "Запись во временный файл с последующим File.Move защищает базу от частичной перезаписи.",
            "await File.WriteAllTextAsync(tempPath, json, ct);\nFile.Move(tempPath, _dbPath, overwrite: true);",
            "Никогда не перезаписывайте критичный файл состояния напрямую без буферного файла.",
            new() { "Реализовать потокобезопасную запись через SemaphoreSlim" },
            new() { Sec("1. Паттерн временного файла", "Сначала данные пишутся в .tmp, затем атомарно переименовываются.") },
            new() { "SemaphoreSlim освобождается в блоке finally" }));
        archCourse.AddModule(archMod1);
        archCourse.AddModule(archMod2);

        // 3. UI/UX
        var uiuxCourse = new Course(
            "course-uiux-systems",
            "Системный UI/UX-дизайн и дизайн-системы",
            "ui-ux-design-systems",
            "Дизайн",
            "Средний",
            "Проектирование сложных интерфейсов, типографические шкалы, контрастность тёмных тем, компонентные библиотеки и передача макетов в разработку.",
            "https://cdn.vector-academy.ru/images/course_uiux_design_1795754088326.jpg",
            "inst-elena",
            12500,
            28,
            4.8,
            284,
            "2026-07-10T09:00:00.000Z",
            "Профессия · 3 урока");
        var uxMod1 = new CourseModule("mod-ux-1", "Модуль 1. Архитектура сетки и иерархия", 1);
        uxMod1.AddLesson(new LessonActivity(
            "les-ux-1",
            "Правило 60-30-10 и поверхности тёмной темы",
            1,
            25,
            "Как выстроить глубину слоёв без тяжёлых теней и визуального шума.",
            "В тёмных интерфейсах глубина создаётся контролируемой разницей яркости поверхностей и тонкими 1px границами.",
            ":root { --bg-canvas: #0B0F17; --bg-surface: #131A26; --accent-primary: #F59E0B; }",
            "Дисциплина поверхностей и контрастность важнее декоративных эффектов.",
            new() { "Распределять цветовой бюджет 60% / 30% / 10%", "Проверять контрастность текста (WCAG AA >= 4.5:1)" },
            new() { Sec("1. Почему в тёмной теме не работают тени", "Используйте осветление вложенной поверхности и тонкую 1px границу.") },
            new() { "На экране присутствует не более одного доминирующего акцента" }));
        uxMod1.AddLesson(new LessonActivity(
            "les-ux-2",
            "Табличная типографика и читаемость метрик",
            2,
            22,
            "Подбор экранных шрифтов и выравнивание чисел в дашбордах с tabular-nums.",
            "Использование табличных цифр предотвращает горизонтальное дрожание столбцов в финансовых и аналитических таблицах.",
            ".metric-value { font-variant-numeric: tabular-nums; }",
            "Любая цифровая таблица и счётчик баллов требуют табличного выравнивания знаков.",
            new() { "Настраивать пары шрифтов Display + Body + Mono" },
            new() { Sec("1. Правило 2+1 для шрифтов", "Display для заголовков, Body для текста и Mono с tabular-nums для цифр.") },
            new() { "Все колонки со стоимостью и датами используют tabular-nums" }));
        uxMod1.AddLesson(new LessonActivity(
            "les-ux-3",
            "Доступность статусов и отказ от цветных капсул",
            3,
            20,
            "Почему иконка + текст работают надёжнее декоративных цветных тегов.",
            "Статусы проверки заданий и расписания должны передаваться через текстовую подпись и иконку без зависимости от цвета.",
            "<StatusLabel status=\"Принято\" />",
            "Состояние интерфейса всегда подтверждается текстом и иконкой.",
            new() { "Комбинировать цвет, иконку и чёткую подпись" },
            new() { Sec("1. Триада статус-индикатора", "Статусы всегда содержат понятную иконку и короткий текстовый лейбл.") },
            new() { "Статус считывается однозначно без опоры только на оттенок" }));
        uxMod1.AddQuiz(new QuizActivity("quiz-ux-1", "Основы дизайн-систем", 50, 100, new List<QuizQuestion>
        {
            new() { Id = "q-ux-1-1", Text = "Какую долю интерфейса должен занимать акцентный цвет по правилу 60-30-10?", Options = new() { "До 10%", "Около 30%", "Ровно 50%" }, CorrectIndex = 0, Explanation = "10% акцентного бюджета сохраняют чёткий фокус на целевых кнопках." },
            new() { Id = "q-ux-1-2", Text = "Допустимо ли передавать критический статус ошибки только цветом без текста или иконки?", Options = new() { "Да, красного цвета достаточно", "Нет, требуется текст или иконка для доступности" }, CorrectIndex = 1, Explanation = "Статус всегда дублируется текстом и/или иконкой." }
        }));
        uxMod1.AddAssignment(new AssignmentActivity("hw-ux-1", "Аудит карточки курса и шапки навигации", "Опишите 3 улучшения визуальной иерархии для каталога курсов и приложите ссылку на ваш макет.", 100));
        uiuxCourse.AddModule(uxMod1);

        // 4. Брендинг и прототипирование
        var brandCourse = new Course(
            "course-brand-proto",
            "Брендинг и интерактивное прототипирование",
            "branding-and-prototyping",
            "Дизайн",
            "Начальный",
            "Создание визуальной айдентики цифрового продукта, модульные сетки, интерактивные переходы и тестирование гипотез на прототипах.",
            "https://cdn.vector-academy.ru/images/course_brand_motion_1795756537801.jpg",
            "inst-elena",
            9900,
            18,
            4.8,
            142,
            "2026-07-15T09:00:00.000Z",
            "Интенсив · 2 урока");
        var brMod1 = new CourseModule("mod-br-1", "Модуль 1. Типографика и интерактивные сценарии", 1);
        brMod1.AddLesson(new LessonActivity("les-br-1", "Характер бренда через шрифт и ритм отступов", 1, 24, "Подбор визуального голоса цифровой компании.", "Сильный бренд узнаётся ещё до прочтения логотипа — по ритму заголовков и пропорциям карточек.", ".btn-primary { padding: 10px 20px; }", "Единая ритмическая сетка создаёт ощущение собранности продукта."));
        brMod1.AddLesson(new LessonActivity("les-br-2", "Проверка сценария на кликабельном прототипе", 2, 26, "Быстрая проверка понятности навигации до старта разработки.", "Соберите связный сценарий из 5 экранов и попросите респондента выполнить задачу без подсказок.", "Каталог -> Выбор курса -> Запись -> Первый урок", "Тестирование прототипа экономит недели дорогой разработки."));
        brMod1.AddQuiz(new QuizActivity("quiz-br-1", "Тест по сеткам и прототипам", 50, 90, new List<QuizQuestion>
        {
            new() { Id = "q-br-1-1", Text = "Каким должно быть соотношение внешнего отступа контейнера и расстояния между его дочерними карточками?", Options = new() { "Внешний отступ меньше внутреннего", "Внешний отступ больше или равен внутреннему" }, CorrectIndex = 1, Explanation = "Это визуально группирует элементы внутри контейнера по принципу близости." }
        }));
        brandCourse.AddModule(brMod1);

        // 5. Цифровой маркетинг
        var mktCourse = new Course(
            "course-digital-marketing",
            "Системный цифровой маркетинг и юнит-экономика",
            "digital-marketing-unit-economics",
            "Маркетинг",
            "Начальный",
            "Открытый вводный курс: построение воронки привлечения, расчёт CAC и LTV, настройка сквозной аналитики и гипотез роста.",
            "https://cdn.vector-academy.ru/images/course_digital_marketing_1795754088725.jpg",
            "inst-irina",
            0,
            16,
            4.8,
            690,
            "2026-07-20T09:00:00.000Z",
            "Открытый курс · 3 урока");
        var mktMod1 = new CourseModule("mod-mkt-1", "Модуль 1. Метрики привлечения и удержания", 1);
        mktMod1.AddLesson(new LessonActivity("les-mkt-1", "Соотношение LTV и CAC на практике", 1, 20, "Как оценивать окупаемость рекламных каналов до масштабирования бюджета.", "Здоровая модель онлайн-образования стремится к соотношению LTV / CAC >= 3.", "LTV = ARPU * GrossMargin * LifetimeMonths", "Масштабируйте канал только после подтверждения окупаемости на первой когорте."));
        mktMod1.AddLesson(new LessonActivity("les-mkt-2", "Сегментация аудитории и офферы (JTBD)", 2, 24, "Формулирование ценностного предложения под конкретную задачу студента.", "Разделение аудитории по уровню подготовки и карьерной цели снижает стоимость привлечения.", "Сегмент A -> Бесплатный практикум", "Точное попадание в задачу сегмента важнее агрессивных скидок."));
        mktMod1.AddLesson(new LessonActivity("les-mkt-3", "Сквозная воронка: от заявки к активному студенту", 3, 22, "Анализ узких мест между просмотром каталога и завершением первого модуля.", "Маркетинг отвечает и за онбординг студента в первые 48 часов.", "Визит -> Заявка (8%) -> Оплата (45%) -> Урок (82%)", "Первый успешный урок — главный драйвер удержания на платформе."));
        mktMod1.AddQuiz(new QuizActivity("quiz-mkt-1", "Контроль по юнит-экономике", 50, 90, new List<QuizQuestion>
        {
            new() { Id = "q-mkt-1-1", Text = "Что показывает метрика CAC?", Options = new() { "Стоимость привлечения одного платящего клиента", "Среднюю длительность сессии", "Общую выручку" }, CorrectIndex = 0, Explanation = "Customer Acquisition Cost отражает полные затраты на привлечение одного клиента." }
        }));
        mktCourse.AddModule(mktMod1);

        // 6. SQL Аналитика
        var anCourse = new Course(
            "course-data-analytics",
            "Продуктовая и SQL-аналитика данных",
            "product-sql-analytics",
            "Аналитика",
            "Средний",
            "Оконные функции SQL, когортный анализ удержания, оценка статистической значимости A/B-тестов и построение дашбордов решений.",
            "https://cdn.vector-academy.ru/images/course_data_analytics_1795754089234.jpg",
            "inst-dmitry",
            16900,
            32,
            4.9,
            318,
            "2026-08-05T09:00:00.000Z",
            "Профессия · 3 урока");
        var anMod1 = new CourseModule("mod-an-1", "Модуль 1. Продвинутый SQL и когорты", 1);
        anMod1.AddLesson(new LessonActivity("les-an-1", "Оконные функции OVER и PARTITION BY", 1, 30, "Расчёт накопительного итога и ранжирования студентов без потери детализации строк.", "Оконные функции позволяют вычислять агрегаты по окну записей, сохраняя каждую строку выборки.", "DENSE_RANK() OVER (PARTITION BY course_id ORDER BY score DESC)", "PARTITION BY задаёт границы окна расчёта, не схлопывая таблицу как GROUP BY."));
        anMod1.AddLesson(new LessonActivity("les-an-2", "Дизайн A/B-экспериментов и MDE", 2, 35, "Определение минимально детектируемого эффекта и длительности теста.", "Преждевременная остановка A/B-теста резко повышает риск ложноположительного результата.", "alpha = 0.05; power = 0.80", "Никогда не останавливайте эксперимент раньше достижения расчётной выборки."));
        anMod1.AddLesson(new LessonActivity("les-an-3", "Когортный анализ удержания студентов", 3, 28, "Построение матрицы Retention по неделям старта обучения.", "Когортный анализ показывает, как меняется активность потока с течением времени.", "DATE_TRUNC('week', enrolled_at) AS cohort_week", "Когорты показывают реальное качество продукта на дистанции."));
        anMod1.AddQuiz(new QuizActivity("quiz-an-1", "Тест: SQL и эксперименты", 50, 110, new List<QuizQuestion>
        {
            new() { Id = "q-an-1-1", Text = "Чем оконная функция отличается от GROUP BY?", Options = new() { "Не сворачивает строки, а добавляет вычисленное значение к каждой строке", "Работает только с текстом", "Запрещает ORDER BY" }, CorrectIndex = 0, Explanation = "Оконные функции сохраняют исходное количество строк." }
        }));
        anMod1.AddAssignment(new AssignmentActivity("hw-an-1", "Когортный SQL-отчёт удержания студентов", "Напишите запрос расчёта удержания студентов по неделям после записи на курс.", 100));
        anCourse.AddModule(anMod1);

        // 7. Python & ML
        var pyCourse = new Course(
            "course-python-ml",
            "Python и прикладной анализ данных",
            "python-applied-data-analysis",
            "Аналитика",
            "Продвинутый",
            "Автоматизация отчётности на Python и Pandas, очистка аномалий, сегментация пользователей и прогнозирование оттока студентов.",
            "https://cdn.vector-academy.ru/images/course_python_ml_1795756538578.jpg",
            "inst-dmitry",
            17500,
            30,
            4.9,
            210,
            "2026-08-10T09:00:00.000Z",
            "Интенсив · 2 урока");
        var pyMod1 = new CourseModule("mod-py-1", "Модуль 1. Пайплайны данных в Pandas", 1);
        pyMod1.AddLesson(new LessonActivity("les-py-1", "Очистка данных и векторные операции в Pandas", 1, 32, "Быстрая обработка сотен тысяч строк без медленных циклов Python.", "Векторные вычисления NumPy и Pandas готовят витрины данных в десятки раз быстрее обычных циклов.", "df = raw_df.dropna(subset=[\"student_id\"]).assign(passed=lambda x: x[\"score\"] >= 60)", "Пишите пайплайн очистки как последовательность чистых шагов."));
        pyMod1.AddLesson(new LessonActivity("les-py-2", "Прогнозирование риска оттока студентов", 2, 34, "Построение признаков активности и оценка точности модели.", "Выявляйте студентов, которые снизили темп прохождения уроков, чтобы куратор вовремя помог.", "features = [\"days_since_last_lesson\", \"avg_hw_score\"]", "Временная валидация обязательна для любых поведенческих данных."));
        pyMod1.AddQuiz(new QuizActivity("quiz-py-1", "Проверка: Pandas и валидация", 50, 120, new List<QuizQuestion>
        {
            new() { Id = "q-py-1-1", Text = "Почему при прогнозе оттока нельзя делать случайный train_test_split по всем событиям?", Options = new() { "Возникает утечка будущего времени в обучающую выборку", "Pandas не поддерживает random" }, CorrectIndex = 0, Explanation = "Случайное перемешивание смешивает будущие и прошлые события одного пользователя." }
        }));
        pyCourse.AddModule(pyMod1);

        // 8. Менеджмент
        var pmCourse = new Course(
            "course-product-management",
            "Управление IT-продуктом и дорожными картами",
            "it-product-management",
            "Менеджмент",
            "Продвинутый",
            "Приоритизация бэклога по RICE, управление рисками релизов, метрики командной поставки и синхронизация бизнеса с разработкой.",
            "https://cdn.vector-academy.ru/images/course_product_management_1795754090173.jpg",
            "inst-irina",
            18500,
            40,
            4.8,
            195,
            "2026-08-18T09:00:00.000Z",
            "Профессия · 3 урока");
        var pmMod1 = new CourseModule("mod-pm-1", "Модуль 1. Стратегия и приоритизация", 1);
        pmMod1.AddLesson(new LessonActivity("les-pm-1", "Фреймворк RICE для объективного бэклога", 1, 27, "Сравнение продуктовых инициатив через охват, влияние, уверенность и трудозатраты.", "Модель RICE помогает команде уйти от субъективных споров к прозрачной оценке отдачи.", "RICE_Score = (Reach * Impact * Confidence) / Effort", "Сопоставляйте ценность инициативы с реальной стоимостью её разработки."));
        pmMod1.AddLesson(new LessonActivity("les-pm-2", "Контроль технического долга в релизном цикле", 2, 29, "Выделение квоты на рефакторинг и стабильность архитектуры.", "Регулярная квота 15–20% спринта сохраняет стабильную скорость поставки новых функций.", "SprintCapacity = 80% Product Features + 20% Reliability", "Планируйте инженерное качество как обязательную часть дорожной карты."));
        pmMod1.AddLesson(new LessonActivity("les-pm-3", "Запуск релиза и пост-релизный разбор метрик", 3, 25, "Критерии приёмки релиза и оценка реального влияния на KPI через 14 дней.", "Выкатка кода в продакшен — это только старт проверки гипотезы.", "Definition of Done = Код + Тесты + Аналитика + Замер", "Ценность фичи измеряется изменением поведения пользователей."));
        pmMod1.AddQuiz(new QuizActivity("quiz-pm-1", "Тест: Приоритизация RICE", 50, 110, new List<QuizQuestion>
        {
            new() { Id = "q-pm-1-1", Text = "Что находится в знаменателе формулы RICE?", Options = new() { "Effort (трудозатраты)", "Reach (охват)", "Confidence (уверенность)" }, CorrectIndex = 0, Explanation = "Чем выше трудозатраты Effort, тем ниже итоговый приоритет RICE." }
        }));
        pmCourse.AddModule(pmMod1);

        db.Courses.AddRange(new[] { csharpCourse, archCourse, uiuxCourse, brandCourse, mktCourse, anCourse, pyCourse, pmCourse });

        db.Enrollments.AddRange(new[]
        {
            new Enrollment(
                "enr-1", "stud-ivan", "course-csharp-oop", "2026-09-05T12:00:00.000Z",
                new() { "les-cs-1", "les-cs-2", "les-cs-3", "les-cs-4", "les-cs-5" },
                new() { ["quiz-cs-1"] = new QuizAttemptResult { QuizId = "quiz-cs-1", Score = 100, MaxScore = 100, Passed = true, PointsEarned = 120, Answers = new() { 1, 1 }, SubmittedAt = "2026-09-20T14:30:00.000Z" } },
                100, "VEC-2026-4812", "2026-10-15T18:00:00.000Z",
                new() { ["les-cs-1"] = "Запомнить: для защиты инвариантов в JSON-моделях используем protected set + [JsonInclude]." }),
            new Enrollment("enr-2", "stud-ivan", "course-data-analytics", "2026-10-18T10:00:00.000Z", new() { "les-an-1" }, new(), 33),
            new Enrollment(
                "enr-3", "stud-maria", "course-uiux-systems", "2026-09-12T11:00:00.000Z",
                new() { "les-ux-1", "les-ux-2", "les-ux-3" },
                new() { ["quiz-ux-1"] = new QuizAttemptResult { QuizId = "quiz-ux-1", Score = 100, MaxScore = 100, Passed = true, PointsEarned = 100, Answers = new() { 0, 1 }, SubmittedAt = "2026-10-01T15:00:00.000Z" } },
                100, "VEC-2026-6104", "2026-10-28T16:10:00.000Z"),
            new Enrollment("enr-4", "stud-artem", "course-csharp-oop", "2026-09-15T09:30:00.000Z", new() { "les-cs-1", "les-cs-2" }, new(), 40),
            new Enrollment(
                "enr-5", "stud-olga", "course-data-analytics", "2026-09-18T14:00:00.000Z",
                new() { "les-an-1", "les-an-2", "les-an-3" },
                new() { ["quiz-an-1"] = new QuizAttemptResult { QuizId = "quiz-an-1", Score = 100, MaxScore = 100, Passed = true, PointsEarned = 110, Answers = new() { 0 }, SubmittedAt = "2026-10-22T12:15:00.000Z" } },
                100, "VEC-2026-7391", "2026-11-04T16:45:00.000Z"),
            new Enrollment(
                "enr-6", "stud-ekaterina", "course-dotnet-arch", "2026-09-20T10:00:00.000Z",
                new() { "les-arch-1", "les-arch-2", "les-arch-3" },
                new() { ["quiz-arch-1"] = new QuizAttemptResult { QuizId = "quiz-arch-1", Score = 100, MaxScore = 100, Passed = true, PointsEarned = 130, Answers = new() { 0 }, SubmittedAt = "2026-10-25T15:00:00.000Z" } },
                100, "VEC-2026-9120", "2026-11-10T17:00:00.000Z"),
            new Enrollment("enr-7", "stud-sergey", "course-dotnet-arch", "2026-10-02T11:00:00.000Z", new() { "les-arch-1", "les-arch-2" }, new(), 67),
            new Enrollment("enr-8", "stud-anna", "course-brand-proto", "2026-10-05T12:00:00.000Z", new() { "les-br-1" }, new(), 50),
            new Enrollment("enr-9", "stud-maxim", "course-csharp-oop", "2026-10-12T10:00:00.000Z", new() { "les-cs-1" }, new(), 20),
            new Enrollment(
                "enr-10", "stud-daria", "course-python-ml", "2026-10-14T14:00:00.000Z",
                new() { "les-py-1" },
                new() { ["quiz-py-1"] = new QuizAttemptResult { QuizId = "quiz-py-1", Score = 100, MaxScore = 100, Passed = true, PointsEarned = 120, Answers = new() { 0 }, SubmittedAt = "2026-11-01T16:00:00.000Z" } },
                50),
            new Enrollment("enr-11", "stud-pavel", "course-digital-marketing", "2026-11-01T09:00:00.000Z", new() { "les-mkt-1" }, new(), 33),
            new Enrollment("enr-12", "stud-alina", "course-uiux-systems", "2026-11-03T13:00:00.000Z", new() { "les-ux-1" }, new(), 33)
        });

        db.Homeworks.AddRange(new[]
        {
            new HomeworkSubmission("sub-1", "stud-ivan", "course-csharp-oop", "hw-cs-1", "Реализовал базовый класс LearningActivityBase с абстрактным методом Describe() и наследниками.", "https://github.com/ivan-petrov/academy-oop-csharp", "Принято", 96, "Отличная архитектура классов и аккуратная валидация инвариантов в конструкторе.", "2026-10-10T16:00:00.000Z", "2026-10-11T11:20:00.000Z"),
            new HomeworkSubmission("sub-2", "stud-ivan", "course-data-analytics", "hw-an-1", "Подготовил SQL-запрос с оконной функцией DENSE_RANK и разбивкой студентов по неделям.", "https://gist.github.com/ivan-petrov/cohort-sql", "На проверке", null, "", "2026-11-24T13:15:00.000Z", null),
            new HomeworkSubmission("sub-3", "stud-maria", "course-uiux-systems", "hw-ux-1", "Провела аудит контрастности карточек курсов и предложила унифицированную сетку отступов 8px.", "https://www.figma.com/file/vector-ui-audit", "Принято", 94, "Системный разбор контрастности и чистая аргументация по 60-30-10.", "2026-10-25T10:40:00.000Z", "2026-10-26T12:00:00.000Z"),
            new HomeworkSubmission("sub-4", "stud-artem", "course-csharp-oop", "hw-cs-1", "Добавил классы уроков и вебинаров, но пока без отдельного доменного исключения при пересечении времени.", "https://github.com/artem-smirnov/csharp-oop-draft", "Нужна доработка", 64, "Добавьте ConflictDomainException (409) в валидатор расписания вебинаров.", "2026-11-20T17:30:00.000Z", "2026-11-21T09:45:00.000Z"),
            new HomeworkSubmission("sub-5", "stud-ekaterina", "course-dotnet-arch", "hw-arch-1", "Спроектировала три независимых слоя Domain, Application и Infrastructure.", "https://github.com/ekaterina-orlova/clean-arch-sample", "Принято", 98, "Образцовое разделение границ и грамотный разбор потокобезопасности.", "2026-11-08T14:20:00.000Z", "2026-11-09T10:15:00.000Z"),
            new HomeworkSubmission("sub-6", "stud-sergey", "course-dotnet-arch", "hw-arch-1", "Описал структуру проектов и перенёс DTO в Application.", "https://github.com/sergey-zakharov/dotnet-layers", "На проверке", null, "", "2026-11-25T16:10:00.000Z", null),
            new HomeworkSubmission("sub-7", "stud-alina", "course-uiux-systems", "hw-ux-1", "Пересобрала карточку курса без лишних пилюль и выровняла типографику по модульной сетке.", "https://www.figma.com/file/alina-ui-card", "На проверке", null, "", "2026-11-26T09:30:00.000Z", null)
        });

        db.Webinars.AddRange(new[]
        {
            new WebinarActivity("web-1", "Разбор архитектуры ASP.NET Core Web API и принципов SOLID", "course-csharp-oop", "inst-alexey", "2026-11-28T16:00:00.000Z", "2026-11-28T17:30:00.000Z", "https://meet.vector-academy.ru/csharp-solid-live", "Живой разбор типичных ошибок при проектировании доменных сущностей и DI-контейнера.", "2026-11-15T10:00:00.000Z"),
            new WebinarActivity("web-2", "Токены дизайн-систем и контрастность в тёмных интерфейсах", "course-uiux-systems", "inst-elena", "2026-11-29T14:00:00.000Z", "2026-11-29T15:30:00.000Z", "https://meet.vector-academy.ru/design-tokens-session", "Практический воркшоп по проверке WCAG AA и построению модульных сеток.", "2026-11-16T10:00:00.000Z"),
            new WebinarActivity("web-3", "Когортный анализ и проверка продуктовых гипотез", "course-data-analytics", "inst-dmitry", "2026-11-30T15:00:00.000Z", "2026-11-30T16:30:00.000Z", "https://meet.vector-academy.ru/sql-cohorts-qa", "Разбор реальных SQL-отчётов студентов и ответы на вопросы по A/B-тестам.", "2026-11-18T10:00:00.000Z"),
            new WebinarActivity("web-4", "Разбор юнит-экономики и сквозной воронки для стартапов", "course-digital-marketing", "inst-irina", "2026-12-01T16:00:00.000Z", "2026-12-01T17:15:00.000Z", "https://meet.vector-academy.ru/growth-unit-econ", "Разбираем кейсы студентов и считаем реальную окупаемость рекламных каналов.", "2026-11-19T10:00:00.000Z"),
            new WebinarActivity("web-5", "Clean Architecture и паттерны хранения данных в .NET 8", "course-dotnet-arch", "inst-alexey", "2026-12-03T16:00:00.000Z", "2026-12-03T17:30:00.000Z", "https://meet.vector-academy.ru/clean-arch-qa", "Практическая сессия вопросов и ответов по проектированию слоёв приложения.", "2026-11-20T10:00:00.000Z")
        });

        db.Payments.AddRange(new[]
        {
            new PaymentRecord("pay-1", "stud-ivan", "course-csharp-oop", 14900, "Мгновенная оплата", "Оплачено", "2026-09-05T11:58:00.000Z", "2026-09-05T12:00:00.000Z"),
            new PaymentRecord("pay-2", "stud-ivan", "course-data-analytics", 16900, "Мгновенная оплата", "Оплачено", "2026-10-18T09:59:00.000Z", "2026-10-18T10:00:00.000Z"),
            new PaymentRecord("pay-3", "stud-maria", "course-uiux-systems", 12500, "Мгновенная оплата", "Оплачено", "2026-09-12T10:55:00.000Z", "2026-09-12T11:00:00.000Z"),
            new PaymentRecord("pay-4", "stud-nikita", "course-product-management", 18500, "Заявка на обучение", "Ожидает подтверждения", "2026-11-25T15:20:00.000Z", null),
            new PaymentRecord("pay-5", "stud-olga", "course-data-analytics", 16900, "Мгновенная оплата", "Оплачено", "2026-09-18T13:55:00.000Z", "2026-09-18T14:00:00.000Z"),
            new PaymentRecord("pay-6", "stud-ekaterina", "course-dotnet-arch", 19900, "Мгновенная оплата", "Оплачено", "2026-09-20T09:55:00.000Z", "2026-09-20T10:00:00.000Z"),
            new PaymentRecord("pay-7", "stud-daria", "course-python-ml", 17500, "Мгновенная оплата", "Оплачено", "2026-10-14T13:55:00.000Z", "2026-10-14T14:00:00.000Z"),
            new PaymentRecord("pay-8", "stud-maxim", "course-dotnet-arch", 19900, "Заявка на обучение", "Ожидает подтверждения", "2026-11-26T11:10:00.000Z", null)
        });

        db.MonthlyDynamics.AddRange(new[]
        {
            new MonthlyDynamic { Month = "Июн", Enrollments = 18, Graduations = 6 },
            new MonthlyDynamic { Month = "Июл", Enrollments = 29, Graduations = 11 },
            new MonthlyDynamic { Month = "Авг", Enrollments = 42, Graduations = 17 },
            new MonthlyDynamic { Month = "Сен", Enrollments = 58, Graduations = 26 },
            new MonthlyDynamic { Month = "Окт", Enrollments = 71, Graduations = 35 },
            new MonthlyDynamic { Month = "Ноя", Enrollments = 84, Graduations = 44 }
        });

        db.Reviews.AddRange(new[]
        {
            new PlatformReview { Id = "rev-1", AuthorName = "Иван Петров", AuthorRole = "Backend-разработчик в «ФинТех Сервис»", CourseId = "course-csharp-oop", CourseTitle = "ООП на C# (.NET 8)", Outcome = "С нуля в C# до защиты промышленного Web API и оффера на Middle-разработчика", Quote = "До курса писал весь код в контроллерах. Разбор инкапсуляции, иерархии исключений и DI с комментариями Алексея полностью изменил мой подход к архитектуре.", Rating = 5, CreatedAt = "2026-10-20T12:00:00.000Z" },
            new PlatformReview { Id = "rev-2", AuthorName = "Екатерина Орлова", AuthorRole = "Senior .NET Engineer в «КлаудПлатформа»", CourseId = "course-dotnet-arch", CourseTitle = "Архитектура ASP.NET Core и Clean Architecture", Outcome = "Перевела монолит команды на слоистую архитектуру за 6 недель", Quote = "Особенно ценны практические задания и живые код-ревью. Каждое замечание преподавателя можно сразу применять в продакшене.", Rating = 5, CreatedAt = "2026-11-12T15:30:00.000Z" },
            new PlatformReview { Id = "rev-3", AuthorName = "Мария Кузнецова", AuthorRole = "Продуктовый дизайнер в «НордДизайн»", CourseId = "course-uiux-systems", CourseTitle = "Системный UI/UX-дизайн и дизайн-системы", Outcome = "Собрала дизайн-систему из 60 компонентов и прошла грейдирование", Quote = "Без воды про «вдохновение»: чёткие правила контрастности тёмных тем, сетки, табличные цифры и аккуратная передача макетов в разработку.", Rating = 5, CreatedAt = "2026-11-01T14:00:00.000Z" },
            new PlatformReview { Id = "rev-4", AuthorName = "Ольга Новикова", AuthorRole = "Продуктовый аналитик в «ТрекМаркет»", CourseId = "course-data-analytics", CourseTitle = "Продуктовая и SQL-аналитика данных", Outcome = "Внедрила когортную отчётность и сократила ошибки в A/B-тестах", Quote = "Конспекты уроков с готовыми примерами оконных функций SQL стали моей настольной шпаргалкой на работе.", Rating = 5, CreatedAt = "2026-11-06T11:20:00.000Z" },
            new PlatformReview { Id = "rev-5", AuthorName = "Дарья Степанова", AuthorRole = "Data Analyst в «ЛогистикПро»", CourseId = "course-python-ml", CourseTitle = "Python и прикладной анализ данных", Outcome = "Автоматизировала еженедельные отчёты, сэкономив 8 часов в неделю", Quote = "Удобно, что внутри урока можно вести свои заметки и сразу проверять понимание короткими тестами.", Rating = 5, CreatedAt = "2026-11-18T09:40:00.000Z" },
            new PlatformReview { Id = "rev-6", AuthorName = "Павел Николаев", AuthorRole = "Руководитель направления в «СмартРитейл»", CourseId = "course-digital-marketing", CourseTitle = "Системный цифровой маркетинг и юнит-экономика", Outcome = "Пересчитал CAC/LTV и отказался от двух убыточных каналов трафика", Quote = "Даже вводный бесплатный курс дал больше прикладных формул, чем платные конференции.", Rating = 5, CreatedAt = "2026-11-21T18:00:00.000Z" }
        });

        db.Inquiries.AddRange(new[]
        {
            new GuestInquiry("inq-1", "Виктор Соколов", "v.sokolov@corp-it.ru", "Корпоративное обучение команды (.NET)", "Планируем обучение 8 бэкенд-разработчиков по курсу C# ООП и Clean Architecture. Подскажите ближайший старт потока.", "2026-11-24T11:15:00.000Z"),
            new GuestInquiry("inq-2", "Алина Громова", "alina.g@mail.ru", "Подбор курса для старта в аналитике", "Имею базовый опыт работы в Excel, хочу выбрать между SQL-аналитикой и курсом по Python. Что лучше пройти первым?", "2026-11-25T16:40:00.000Z")
        });

        return db;
    }
}
