using AcademyVector.Api.Application.Interfaces;
using AcademyVector.Api.Application.Services;
using AcademyVector.Api.Domain.Entities;
using AcademyVector.Api.Domain.Exceptions;
using AcademyVector.Api.Infrastructure;

namespace AcademyVector.Tests;

internal static class Program
{
    private static int _passed;
    private static int _failed;

    private static async Task Main()
    {
        Console.WriteLine("=== Проверка ООП-модели и сервисов «Академия Вектор» (.NET 8) ===");

        Run("Иерархия пользователей (наследование и полиморфизм UserBase)", TestUserHierarchyAndPolymorphism);
        Run("Инкапсуляция и инварианты сущностей (Student, Course, Enrollment, Homework, GuestInquiry)", TestEncapsulationAndInvariants);
        Run("Иерархия доменных исключений (400 / 404 / 409)", TestDomainExceptionHierarchy);
        Run("Сервис оценивания и прогресса (AssessmentService)", TestAssessmentService);
        Run("Валидатор расписания вебинаров (409 Conflict)", TestWebinarConflictValidator);
        await RunAsync("Сценарии PlatformService, маскировка ролей и полиморфная сводка", TestPlatformServiceWorkflowAsync);

        Console.WriteLine();
        Console.WriteLine($"Итого: пройдено {_passed}, ошибок {_failed}");
        Environment.ExitCode = _failed == 0 ? 0 : 1;
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            _passed++;
            Console.WriteLine($"[OK] {name}");
        }
        catch (Exception ex)
        {
            _failed++;
            Console.WriteLine($"[FAIL] {name}: {ex.Message}");
        }
    }

    private static async Task RunAsync(string name, Func<Task> test)
    {
        try
        {
            await test();
            _passed++;
            Console.WriteLine($"[OK] {name}");
        }
        catch (Exception ex)
        {
            _failed++;
            Console.WriteLine($"[FAIL] {name}: {ex.Message}");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertThrows<TException>(Action action, string message) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Ожидалось исключение {typeof(TException).Name}: {message}");
    }

    private static void TestUserHierarchyAndPolymorphism()
    {
        List<UserBase> users =
        [
            new Student("s-1", "Иван Петров", "ivan@student.ru", "grp-1", "ИП", 100),
            new Instructor("i-1", "Алексей Волков", "alexey@academy.ru", "Архитектор .NET", "Опыт 12 лет", "https://cdn.vector-academy.ru/a.jpg", "C# 12", 12)
        ];

        Assert(users[0].GetRoleLabel() == "Студент", "Роль студента должна определяться полиморфно");
        Assert(users[1].GetRoleLabel() == "Преподаватель", "Роль преподавателя должна определяться полиморфно");
        Assert(users[0].Describe().Contains("100 XP"), "Describe() студента должен содержать XP");
        Assert(users[1].Describe().Contains("12 лет опыта"), "Describe() преподавателя должен содержать стаж");
    }

    private static void TestEncapsulationAndInvariants()
    {
        var student = new Student("s-1", "Анна Фёдорова", "anna@student.ru", null, "АФ", 50);
        student.AddPoints(25);
        Assert(student.TotalPoints == 75, "Баллы должны корректно суммироваться");
        AssertThrows<ValidationDomainException>(() => student.AddPoints(-10), "Отрицательные баллы недопустимы");

        AssertThrows<ValidationDomainException>(
            () => new Course("c-1", "C#", "c", "IT и разработка", "Средний", "Описание", "https://cdn.vector-academy.ru/c.jpg", "i-1", 1000, 10),
            "Короткое название курса должно отклоняться");

        var enrollment = new Enrollment("enr-1", "s-1", "c-1");
        Assert(enrollment.MarkLessonCompleted("les-1"), "Первое завершение урока должно возвращать true");
        Assert(!enrollment.MarkLessonCompleted("les-1"), "Повторное завершение урока должно быть идемпотентным");
        enrollment.SaveLessonNote("les-1", "  Заметка по ООП  ");
        Assert(enrollment.LessonNotes["les-1"] == "Заметка по ООП", "Заметка должна сохраняться и очищаться от пробелов");

        var hw = new HomeworkSubmission("hw-1", "s-1", "c-1", "a-1", "Решение", "https://github.com/test/repo");
        AssertThrows<ValidationDomainException>(() => hw.Review("Нужна доработка", 55, "   "), "Возврат на доработку требует комментарий");
        var newlyAccepted = hw.Review("Принято", 95, "Отличная работа");
        Assert(newlyAccepted && hw.Status == "Принято" && hw.Score == 95, "Проверка ДЗ должна обновлять статус и оценку");

        AssertThrows<ValidationDomainException>(
            () => new GuestInquiry("inq-1", "А", "bad-email", "Тема", "Коротко"),
            "Некорректное обращение гостя должно отклоняться");
    }

    private static void TestDomainExceptionHierarchy()
    {
        DomainException validation = new ValidationDomainException("Ошибка 400");
        DomainException notFound = new NotFoundDomainException("Ошибка 404");
        DomainException conflict = new ConflictDomainException("Ошибка 409");

        Assert(validation.StatusCode == 400, "ValidationDomainException должен иметь код 400");
        Assert(notFound.StatusCode == 404, "NotFoundDomainException должен иметь код 404");
        Assert(conflict.StatusCode == 409, "ConflictDomainException должен иметь код 409");
    }

    private static void TestAssessmentService()
    {
        var service = new AssessmentService();
        var course = new Course("c-1", "ООП на C#", "csharp-oop", "IT и разработка", "Средний", "Описание курса", "https://cdn.vector-academy.ru/c.jpg", "i-1", 14900, 30);
        var mod = new CourseModule("m-1", "Модуль 1", 1);
        mod.AddLesson(new LessonActivity("l-1", "Урок 1", 1, 20, "Кратко", "Текст", "code", "Вывод"));
        mod.AddLesson(new LessonActivity("l-2", "Урок 2", 2, 25, "Кратко", "Текст", "code", "Вывод"));
        var quiz = new QuizActivity("q-1", "Тест 1", 50, 100,
        [
            new QuizQuestion { Id = "qq-1", Text = "Вопрос 1", Options = ["A", "B"], CorrectIndex = 0, Explanation = "Верно A" },
            new QuizQuestion { Id = "qq-2", Text = "Вопрос 2", Options = ["A", "B"], CorrectIndex = 1, Explanation = "Верно B" }
        ]);
        mod.AddQuiz(quiz);
        course.AddModule(mod);

        Assert(service.CalculateProgress(course, ["l-1", "l-1"]) == 50, "Повторный ID урока не должен завышать прогресс");
        Assert(service.CalculateProgress(course, ["l-1", "l-2"]) == 100, "Все завершённые уроки дают 100%");

        var cert = service.GenerateCertificateNumber("enr-1", "c-1");
        Assert(cert.StartsWith("VEC-2026-"), $"Ожидался формат VEC-2026-XXXX, получено: {cert}");

        var (attempt1, _, delta1) = service.EvaluateQuiz(quiz, [0, 1], null);
        Assert(attempt1.Passed && attempt1.Score == 100 && delta1 == 100, "Первое прохождение теста должно начислить 100 баллов");

        var (attempt2, _, delta2) = service.EvaluateQuiz(quiz, [0, 1], attempt1);
        Assert(attempt2.Passed && delta2 == 0, "Повторное прохождение теста не должно повторно начислять баллы");
    }

    private static void TestWebinarConflictValidator()
    {
        var validator = new WebinarScheduleValidator();
        var existing = new List<WebinarActivity>
        {
            new("w-1", "Разбор SOLID", "c-1", "inst-alexey", "2026-11-28T13:00:00.000Z", "2026-11-28T14:30:00.000Z", "https://meet.vector-academy.ru/1", "Описание")
        };

        validator.EnsureNoInstructorConflict(existing, "inst-elena", "2026-11-28T13:15:00.000Z", "2026-11-28T14:00:00.000Z", "Елена Соколова");

        AssertThrows<ConflictDomainException>(
            () => validator.EnsureNoInstructorConflict(existing, "inst-alexey", "2026-11-28T14:00:00.000Z", "2026-11-28T15:00:00.000Z", "Алексей Волков"),
            "Пересечение слотов у одного преподавателя должно выбрасывать ConflictDomainException");
    }

    private static async Task TestPlatformServiceWorkflowAsync()
    {
        var repo = new InMemoryTestRepository(new SeedDataProvider());
        var platform = new PlatformService(repo, new AssessmentService(), new WebinarScheduleValidator());

        var summary = await platform.GetOopSummaryAsync();
        Assert(summary.TotalEntities >= 40, "Полиморфная сводка должна включать всех пользователей и активности");

        var guestDb = await platform.GetBootstrapAsync("guest");
        Assert(guestDb.Courses.Count == 8, "В демо-базе должно быть 8 курсов");
        Assert(guestDb.Students.Count == 12, "В демо-базе должно быть 12 студентов");
        Assert(guestDb.Payments.Count == 0 && guestDb.Homeworks.Count == 0, "Гостю не должны раскрываться платежи и ДЗ");
        Assert(guestDb.Courses[0].Modules[0].Quizzes[0].Questions[0].CorrectIndex == -1, "Гостю не должны раскрываться ответы тестов");

        var inquiry = await platform.SubmitInquiryAsync(new SubmitInquiryRequest("Олег Тихонов", "oleg@example.ru", "Консультация", "Интересует углублённый трек по ASP.NET Core 8."));
        var teacherDb = await platform.GetBootstrapAsync("teacher");
        Assert(teacherDb.Inquiries.Any(i => i.Id == inquiry.Id), "Обращение гостя должно появляться в данных преподавателя");

        var noteRes = await platform.SaveLessonNoteAsync("course-data-analytics", "les-an-1", new SaveLessonNoteRequest("stud-ivan", "Тестовая заметка"));
        Assert(noteRes.Ok && noteRes.LessonNotes["les-an-1"] == "Тестовая заметка", "Заметка студента должна сохраняться");
    }

    private sealed class InMemoryTestRepository : ICourseDbRepository
    {
        private readonly ISeedDataProvider _seed;
        private PlatformDatabase _db;

        public InMemoryTestRepository(ISeedDataProvider seed)
        {
            _seed = seed;
            _db = _seed.CreateSeedDatabase();
        }

        public Task<PlatformDatabase> ReadAsync(CancellationToken ct = default) => Task.FromResult(_db);

        public Task<T> UpdateAsync<T>(Func<PlatformDatabase, T> mutator, CancellationToken ct = default)
        {
            var result = mutator(_db);
            return Task.FromResult(result);
        }

        public Task<PlatformDatabase> ResetAsync(CancellationToken ct = default)
        {
            _db = _seed.CreateSeedDatabase();
            return Task.FromResult(_db);
        }
    }
}
