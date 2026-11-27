# Академия Вектор — Платформа онлайн-курсов (ООП на C# / .NET 8)

Проект по предмету **«Объектно-ориентированное программирование на C#»**, реализованный полностью на **C# 12 / .NET 8 / ASP.NET Core Minimal API** без сторонних NuGet-пакетов и без Node.js/React.

---

## Состав решения (`AcademyVector.sln`)

1. **`backend/AcademyVector.Api`** — основной сервер `ASP.NET Core 8 Web API` на порту `3000` с хранением данных в JSON (`data/courses-db.json`).
2. **`backend/AcademyVector.Tests`** — консольный проект самопроверки ООП-модели, инвариантов сущностей, исключений и бизнес-логики без внешних тестовых фреймворков.

---

## Требования и запуск

- Установленный **.NET 8 SDK** (`dotnet --version` показывает `8.x`), либо **Visual Studio 2022** / **JetBrains Rider**.

### Запуск Web API
```bash
dotnet run --project backend/AcademyVector.Api
```
После запуска API доступен по адресу **http://localhost:3000**:
- `GET /` — обзор сервиса и список всех маршрутов;
- `GET /api/platform/oop-summary` — полиморфная сводка доменных сущностей (`Describe()`);
- `GET /api/bootstrap?role=guest|student|teacher` — данные платформы с учётом роли;
- готовая коллекция запросов для Visual Studio / Rider находится в файле `backend/AcademyVector.Api/AcademyVector.Api.http`.

### Запуск автопроверки ООП-тестов
```bash
dotnet run --project backend/AcademyVector.Tests
```

---

## Реализация принципов ООП (`backend/AcademyVector.Api/`)

### 1. Абстракция и наследование (`Domain/`)
- `Domain/Entities/DomainEntity.cs`:
  - `DomainEntity` — абстрактный базовый класс сущности (`Id`, `CreatedAt`, абстрактный метод `Describe()`, виртуальные методы `GetMetricLabel()`, `GetMetricValue()`).
  - `UserBase : DomainEntity` — абстрактный класс пользователя (`FullName`, `Email`, абстрактный метод `GetRoleLabel()`).
  - `Student : UserBase` и `Instructor : UserBase` — конкретные роли пользователей (включая резюме преподавателя: стаж, образование, карьера, достижения, стек).
  - `StudyGroup : DomainEntity` — учебная группа.
- `Domain/Entities/LearningActivityBase.cs`:
  - `LearningActivityBase : DomainEntity` — абстрактный класс учебной активности (`GetActivityKind()`, `CalculatePotentialPoints()`).
  - Наследники: `LessonActivity`, `QuizActivity`, `AssignmentActivity`, `WebinarActivity`.
- `Domain/Exceptions/DomainException.cs`:
  - Иерархия доменных исключений `DomainException` -> `ValidationDomainException` (`400`), `NotFoundDomainException` (`404`), `ConflictDomainException` (`409`), обрабатываемая централизованно в `Middleware/DomainExceptionMiddleware.cs`.

### 2. Инкапсуляция (`Domain/Entities/CourseAndProgress.cs`)
- Свойства сущностей защищены модификаторами `protected set;` / `private set;` с `[JsonInclude]`.
- Изменение состояния и проверка инвариантов выполняются через доменные методы:
  - `Student.AssignToGroup(...)`, `Student.AddPoints(...)`
  - `Course.AddModule(...)`, `Course.IncrementStudents()`
  - `LessonActivity.UpdateContent(...)`
  - `Enrollment.MarkLessonCompleted(...)`, `Enrollment.SaveLessonNote(...)`, `Enrollment.UpdateProgress(...)`, `Enrollment.RecordQuizResult(...)`
  - `HomeworkSubmission.Resubmit(...)`, `HomeworkSubmission.Review(...)`
  - `WebinarActivity.OverlapsWith(...)`
  - `PaymentRecord.Confirm()`
  - `GuestInquiry` (валидация имени, email и текста вопроса в конструкторе)

### 3. Интерфейсы и внедрение зависимостей (DI) (`Application/Interfaces/` и `Program.cs`)
- `ICourseDbRepository` -> `Infrastructure/JsonCourseDbRepository.cs` (атомарная потокобезопасная запись JSON через `.tmp` и `SemaphoreSlim`).
- `ISeedDataProvider` -> `Infrastructure/SeedDataProvider.cs` (8 курсов, 4 преподавателя с резюме, 12 студентов в 4 группах, отзывы, вебинары, оплаты, обращения).
- `IWebinarScheduleValidator` -> `Application/Services/WebinarScheduleValidator.cs` (проверка пересечений расписания преподавателя с выбросом `409 Conflict`).
- `IAssessmentService` -> `Application/Services/AssessmentService.cs` (пересчёт прогресса, оценивание тестов без повторного начисления баллов, генерация сертификата `VEC-2026-XXXX`).
- `IPlatformService` -> `Application/Services/PlatformService.cs`.

### 4. Полиморфизм (`GET /api/platform/oop-summary`)
- Метод `PlatformService.GetOopSummaryAsync()` формирует единые коллекции базовых абстрактных типов `List<UserBase>` и `List<LearningActivityBase>` и вызывает переопределённые методы `Describe()`, `GetMetricLabel()` и `GetMetricValue()` без приведения типов.
