# Академия Вектор — Платформа онлайн-курсов (ООП на C# / .NET 8)

Консольный проект по предмету **«Объектно-ориентированное программирование на C#»**, реализованный полностью на **C# 12 / .NET 8** без внешних зависимостей, без Web API и без фронтенда.

---

## Состав решения (`AcademyVector.sln`)

1. **`backend/AcademyVector.Console`** — интерактивное консольное приложение (.NET 8 Console App) с кабинетами Гостя, Студента и Преподавателя, демонстрацией полиморфизма и хранением данных в JSON (`data/courses-db.json`).
2. **`backend/AcademyVector.Tests`** — консольный проект автотестов ООП-модели, инвариантов сущностей, исключений и бизнес-логики без сторонних библиотек.

---

## Требования и запуск

- Установленный **.NET 8 SDK** (`dotnet --version` показывает `8.x`), либо **Visual Studio 2022** / **JetBrains Rider**.

### Сборка всего решения
~~~bash
dotnet build
~~~

### Запуск консольного приложения
~~~bash
dotnet run --project backend/AcademyVector.Console
~~~

### Запуск автотестов ООП
~~~bash
dotnet run --project backend/AcademyVector.Tests
~~~

---

## Реализация принципов ООП (`backend/AcademyVector.Console/`)

### 1. Абстракция и наследование (`Domain/`)
- `Domain/Entities/DomainEntity.cs`:
  - `DomainEntity` — абстрактный базовый класс сущности (`Id`, `CreatedAt`, `Describe()`, `GetMetricLabel()`, `GetMetricValue()`).
  - `UserBase : DomainEntity` — абстрактный класс пользователя (`FullName`, `Email`, `GetRoleLabel()`).
  - `Student : UserBase`, `Instructor : UserBase`, `StudyGroup : DomainEntity`.
- `Domain/Entities/LearningActivityBase.cs`:
  - `LearningActivityBase : DomainEntity` — абстрактный класс учебной активности (`GetActivityKind()`, `CalculatePotentialPoints()`).
  - Наследники: `LessonActivity`, `QuizActivity`, `AssignmentActivity`, `WebinarActivity`.
- `Domain/Exceptions/DomainException.cs`:
  - Иерархия доменных исключений `DomainException` -> `ValidationDomainException`, `NotFoundDomainException`, `ConflictDomainException`.

### 2. Инкапсуляция (`Domain/Entities/CourseAndProgress.cs`)
- Свойства защищены модификаторами `protected set;` / `private set;` с атрибутом `[JsonInclude]`.
- Изменение состояния выполняется только через методы домена с проверкой инвариантов (`AssignToGroup`, `AddPoints`, `AddModule`, `UpdateContent`, `MarkLessonCompleted`, `RecordQuizResult`, `Review`, `OverlapsWith`, `Confirm`).

### 3. Интерфейсы и внедрение зависимостей (`Application/Interfaces/` и `Program.cs`)
- `ICourseDbRepository` -> `JsonCourseDbRepository` (атомарная потокобезопасная запись JSON).
- `ISeedDataProvider` -> `SeedDataProvider` (демо-база: курсы, преподаватели, студенты, группы, вебинары, тесты).
- `IWebinarScheduleValidator` -> `WebinarScheduleValidator` (проверка пересечения времени вебинаров).
- `IAssessmentService` -> `AssessmentService` (прогресс, оценка тестов, генерация сертификатов).
- `IPlatformService` -> `PlatformService`.

### 4. Полиморфизм
- В пункте меню **«4. Демонстрация ООП и полиморфизма»** (метод `PlatformService.GetOopSummaryAsync()`) объекты собираются в коллекции базовых типов `List<UserBase>` и `List<LearningActivityBase>`, после чего вызываются переопределённые методы `Describe()`, `GetMetricLabel()` и `GetMetricValue()`.