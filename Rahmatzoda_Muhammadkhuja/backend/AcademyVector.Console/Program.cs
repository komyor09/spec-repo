using System.Text;
using AcademyVector.ConsoleApp.Application.Interfaces;
using AcademyVector.ConsoleApp.Application.Services;
using AcademyVector.ConsoleApp.Domain.Entities;
using AcademyVector.ConsoleApp.Domain.Exceptions;
using AcademyVector.ConsoleApp.Infrastructure;

namespace AcademyVector.ConsoleApp;

internal static class Program
{
    private static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        ISeedDataProvider seedProvider = new SeedDataProvider();
        ICourseDbRepository repository = new JsonCourseDbRepository(seedProvider);
        IAssessmentService assessmentService = new AssessmentService();
        IWebinarScheduleValidator webinarValidator = new WebinarScheduleValidator();
        IPlatformService platformService = new PlatformService(repository, assessmentService, webinarValidator);

        var app = new AcademyConsoleApp(platformService);
        await app.RunAsync();
    }
}

internal sealed class AcademyConsoleApp
{
    private readonly IPlatformService _platform;

    public AcademyConsoleApp(IPlatformService platform)
    {
        _platform = platform;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("==================================================");
            Console.WriteLine("   АКАДЕМИЯ ВЕКТОР — Платформа онлайн-курсов");
            Console.WriteLine("   Консольное приложение на C# (.NET 8 / ООП)");
            Console.WriteLine("==================================================");
            Console.WriteLine("1. Кабинет гостя (каталог, преподаватели, вопрос)");
            Console.WriteLine("2. Кабинет студента (запись, уроки, тесты, ДЗ)");
            Console.WriteLine("3. Кабинет преподавателя (курсы, ДЗ, вебинары, оплаты)");
            Console.WriteLine("4. Демонстрация ООП и полиморфизма (Describe)");
            Console.WriteLine("5. Сбросить базу данных (data/courses-db.json)");
            Console.WriteLine("0. Выход");
            Console.Write("Выберите пункт меню: ");

            var choice = Console.ReadLine()?.Trim();
            try
            {
                switch (choice)
                {
                    case "1":
                        await RunGuestMenuAsync();
                        break;
                    case "2":
                        await RunStudentMenuAsync();
                        break;
                    case "3":
                        await RunTeacherMenuAsync();
                        break;
                    case "4":
                        await ShowOopSummaryAsync();
                        break;
                    case "5":
                        await _platform.ResetDatabaseAsync();
                        PrintOk("Демо-база данных успешно сброшена к исходному состоянию.");
                        break;
                    case "0":
                        Console.WriteLine("До свидания!");
                        return;
                    default:
                        PrintWarn("Неизвестный пункт меню.");
                        break;
                }
            }
            catch (DomainException ex)
            {
                PrintError($"[Доменная ошибка {ex.StatusCode} / {ex.ErrorCode}] {ex.Message}");
            }
            catch (Exception ex)
            {
                PrintError($"[Ошибка] {ex.Message}");
            }
        }
    }

    private async Task RunGuestMenuAsync()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("--- КАБИНЕТ ГОСТЯ ---");
            Console.WriteLine("1. Каталог курсов и программа");
            Console.WriteLine("2. Преподаватели и их резюме");
            Console.WriteLine("3. Отзывы выпускников");
            Console.WriteLine("4. Задать вопрос консультанту");
            Console.WriteLine("0. Назад");
            Console.Write("Выбор: ");

            var choice = Console.ReadLine()?.Trim();
            if (choice == "0") return;

            var db = await _platform.GetBootstrapAsync("guest");
            switch (choice)
            {
                case "1":
                    ShowCourses(db);
                    break;
                case "2":
                    foreach (var inst in db.Instructors)
                    {
                        Console.WriteLine($"- {inst.Describe()}");
                        Console.WriteLine($"  Стек: {string.Join(", ", inst.Stack)}");
                        Console.WriteLine($"  Образование: {inst.Education}");
                    }
                    break;
                case "3":
                    foreach (var rev in db.Reviews)
                    {
                        Console.WriteLine($"- {rev.AuthorName} ({rev.CourseTitle}, {rev.Rating}/5): «{rev.Quote}»");
                    }
                    break;
                case "4":
                    var name = ReadText("Ваше имя: ");
                    var email = ReadText("Email: ");
                    var topic = ReadText("Тема (можно Enter для темы по умолчанию): ", allowEmpty: true);
                    var msg = ReadText("Текст вопроса (от 10 символов): ");
                    var inq = await _platform.SubmitInquiryAsync(new SubmitInquiryRequest(name, email, topic, msg));
                    PrintOk($"Обращение #{inq.Id} принято!");
                    break;
            }
        }
    }

    private async Task RunStudentMenuAsync()
    {
        var dbInit = await _platform.GetBootstrapAsync("student");
        var student = PickFromList("Выберите активного студента:", dbInit.Students, s => $"{s.FullName} (баллы: {s.TotalPoints})");
        if (student == null) return;

        while (true)
        {
            var db = await _platform.GetBootstrapAsync("student");
            student = db.Students.First(s => s.Id == student.Id);

            Console.WriteLine();
            Console.WriteLine($"--- КАБИНЕТ СТУДЕНТА: {student.FullName} | Баллы: {student.TotalPoints} ---");
            Console.WriteLine("1. Мои курсы и прогресс");
            Console.WriteLine("2. Записаться на курс");
            Console.WriteLine("3. Пройти урок и добавить заметку");
            Console.WriteLine("4. Пройти тест (квиз)");
            Console.WriteLine("5. Сдать домашнее задание");
            Console.WriteLine("6. Мои домашние задания и статусы");
            Console.WriteLine("7. Расписание вебинаров");
            Console.WriteLine("0. Назад");
            Console.Write("Выбор: ");

            var choice = Console.ReadLine()?.Trim();
            if (choice == "0") return;

            switch (choice)
            {
                case "1":
                    var myEnr = db.Enrollments.Where(e => e.StudentId == student.Id).ToList();
                    if (myEnr.Count == 0)
                    {
                        Console.WriteLine("Пока нет активных курсов.");
                        break;
                    }
                    foreach (var enr in myEnr)
                    {
                        var c = db.Courses.FirstOrDefault(x => x.Id == enr.CourseId);
                        var cert = string.IsNullOrWhiteSpace(enr.CertificateNumber) ? "в процессе" : enr.CertificateNumber;
                        Console.WriteLine($"- {c?.Title ?? enr.CourseId}: {enr.ProgressPercent}% | Сертификат: {cert}");
                    }
                    break;

                case "2":
                    var courseToEnroll = PickFromList("Выберите курс для записи:", db.Courses, c => $"{c.Title} ({c.PriceRub} руб.)");
                    if (courseToEnroll == null) break;
                    Console.Write("Способ: 1 — Мгновенная оплата, 2 — Заявка на подтверждение: ");
                    var mode = Console.ReadLine()?.Trim() == "2" ? "application" : "instant";
                    var enrollRes = await _platform.EnrollCourseAsync(courseToEnroll.Id, new EnrollRequest(student.Id, mode));
                    PrintOk(enrollRes.Enrolled
                        ? $"Вы зачислены на курс «{courseToEnroll.Title}»!"
                        : enrollRes.Message ?? "Заявка создана.");
                    break;

                case "3":
                    var myCourses = db.Courses.Where(c => db.Enrollments.Any(e => e.StudentId == student.Id && e.CourseId == c.Id)).ToList();
                    var courseL = PickFromList("Выберите ваш курс:", myCourses, c => c.Title);
                    if (courseL == null) break;
                    var lessons = courseL.Modules.SelectMany(m => m.Lessons).ToList();
                    var lesson = PickFromList("Выберите урок:", lessons, l => l.Describe());
                    if (lesson == null) break;
                    Console.WriteLine($"\n[{lesson.Title}]\n{lesson.Content}\nКлючевой вывод: {lesson.KeyTakeaway}");
                    var compRes = await _platform.CompleteLessonAsync(courseL.Id, lesson.Id, student.Id);
                    PrintOk($"Урок отмечен пройденным! Прогресс: {compRes.Enrollment.ProgressPercent}% | Баллы: {compRes.StudentTotalPoints}");
                    var note = ReadText("Добавить заметку к уроку (Enter — пропустить): ", allowEmpty: true);
                    if (!string.IsNullOrWhiteSpace(note))
                    {
                        await _platform.SaveLessonNoteAsync(courseL.Id, lesson.Id, new SaveLessonNoteRequest(student.Id, note));
                        PrintOk("Заметка сохранена.");
                    }
                    break;

                case "4":
                    var qCourses = db.Courses.Where(c => db.Enrollments.Any(e => e.StudentId == student.Id && e.CourseId == c.Id)).ToList();
                    var courseQ = PickFromList("Выберите ваш курс:", qCourses, c => c.Title);
                    if (courseQ == null) break;
                    var quizzes = courseQ.Modules.SelectMany(m => m.Quizzes).ToList();
                    var quiz = PickFromList("Выберите тест:", quizzes, q => q.Describe());
                    if (quiz == null) break;
                    var answers = new List<int>();
                    foreach (var q in quiz.Questions)
                    {
                        Console.WriteLine($"\nВопрос: {q.Text}");
                        for (int i = 0; i < q.Options.Count; i++)
                        {
                            Console.WriteLine($"  {i + 1}. {q.Options[i]}");
                        }
                        var ansIdx = ReadInt($"Ваш ответ (1-{q.Options.Count}): ", 1, q.Options.Count) - 1;
                        answers.Add(ansIdx);
                    }
                    var quizRes = await _platform.SubmitQuizAsync(courseQ.Id, quiz.Id, new SubmitQuizRequest(student.Id, answers));
                    PrintOk($"Результат теста: {quizRes.Attempt.Score}% ({(quizRes.Attempt.Passed ? "Сдан" : "Не сдан")}). Всего баллов: {quizRes.StudentTotalPoints}");
                    foreach (var d in quizRes.Details)
                    {
                        Console.WriteLine($"  - {(d.IsCorrect ? "Верно" : "Ошибка")}: {d.Explanation}");
                    }
                    break;

                case "5":
                    var hwCourses = db.Courses.Where(c => db.Enrollments.Any(e => e.StudentId == student.Id && e.CourseId == c.Id)).ToList();
                    var courseHw = PickFromList("Выберите ваш курс:", hwCourses, c => c.Title);
                    if (courseHw == null) break;
                    var assignments = courseHw.Modules.SelectMany(m => m.Assignments).ToList();
                    var assignment = PickFromList("Выберите задание:", assignments, a => a.Describe());
                    if (assignment == null) break;
                    var text = ReadText("Описание решения: ");
                    var url = ReadText("Ссылка на GitHub/решение (можно Enter): ", allowEmpty: true);
                    var sub = await _platform.SubmitHomeworkAsync(courseHw.Id, assignment.Id, new SubmitHomeworkRequest(student.Id, text, url));
                    PrintOk($"Решение отправлено (#{sub.Id}, статус: {sub.Status}).");
                    break;

                case "6":
                    var myHws = db.Homeworks.Where(h => h.StudentId == student.Id).ToList();
                    if (myHws.Count == 0) { Console.WriteLine("Домашних заданий пока нет."); break; }
                    foreach (var h in myHws)
                    {
                        Console.WriteLine($"- {h.Describe()} | Оценка: {h.Score?.ToString() ?? "-"} | Комментарий: {h.TeacherComment}");
                    }
                    break;

                case "7":
                    ShowWebinars(db);
                    break;
            }
        }
    }

    private async Task RunTeacherMenuAsync()
    {
        while (true)
        {
            var db = await _platform.GetBootstrapAsync("teacher");
            Console.WriteLine();
            Console.WriteLine("--- КАБИНЕТ ПРЕПОДАВАТЕЛЯ ---");
            Console.WriteLine("1. Создать новый курс");
            Console.WriteLine("2. Проверить домашние задания");
            Console.WriteLine("3. Запланировать вебинар (проверка конфликтов расписания)");
            Console.WriteLine("4. Удалить вебинар");
            Console.WriteLine("5. Подтвердить заявку на оплату");
            Console.WriteLine("6. Создать учебную группу и перевести студента");
            Console.WriteLine("7. Посмотреть обращения гостей");
            Console.WriteLine("0. Назад");
            Console.Write("Выбор: ");

            var choice = Console.ReadLine()?.Trim();
            if (choice == "0") return;

            switch (choice)
            {
                case "1":
                    var title = ReadText("Название курса (от 4 символов): ");
                    var cat = PickFromList("Категория:", new List<string> { "IT и разработка", "Дизайн", "Маркетинг", "Аналитика", "Менеджмент" }, x => x);
                    if (cat == null) break;
                    var lvl = PickFromList("Уровень:", new List<string> { "Начальный", "Средний", "Продвинутый" }, x => x);
                    if (lvl == null) break;
                    var inst = PickFromList("Преподаватель:", db.Instructors, i => i.FullName);
                    if (inst == null) break;
                    var desc = ReadText("Описание курса: ");
                    var price = ReadInt("Цена (руб., >= 0): ", 0, 500000);
                    var hours = ReadInt("Длительность (часов, > 0): ", 1, 1000);
                    var created = await _platform.CreateCourseAsync(new CreateCourseRequest(title, cat, lvl, desc, inst.Id, price, hours, null));
                    PrintOk($"Курс создан: {created.Describe()}");
                    break;

                case "2":
                    var hw = PickFromList("Выберите работу на проверку:", db.Homeworks, h => h.Describe());
                    if (hw == null) break;
                    Console.Write("Решение: 1 — Принято, 2 — Нужна доработка: ");
                    var status = Console.ReadLine()?.Trim() == "2" ? "Нужна доработка" : "Принято";
                    var score = ReadInt("Оценка (0-100): ", 0, 100);
                    var comment = ReadText("Комментарий преподавателя: ", allowEmpty: status == "Принято");
                    var reviewed = await _platform.ReviewHomeworkAsync(hw.Id, new ReviewHomeworkRequest(status, score, comment));
                    PrintOk($"Проверено: {reviewed.Status}, оценка {reviewed.Score}.");
                    break;

                case "3":
                    var wTitle = ReadText("Название вебинара: ");
                    var wCourse = PickFromList("Курс:", db.Courses, c => c.Title);
                    if (wCourse == null) break;
                    var wInst = PickFromList("Преподаватель:", db.Instructors, i => i.FullName);
                    if (wInst == null) break;
                    var start = ReadText("Начало (ISO, например 2026-12-05T15:00:00Z): ");
                    var end = ReadText("Окончание (ISO, например 2026-12-05T16:30:00Z): ");
                    var url = ReadText("Ссылка (https://...): ");
                    var wDesc = ReadText("Описание (можно Enter): ", allowEmpty: true);
                    var webinar = await _platform.ScheduleWebinarAsync(new CreateWebinarRequest(wTitle, wCourse.Id, wInst.Id, start, end, url, wDesc));
                    PrintOk($"Вебинар запланирован: {webinar.Describe()}");
                    break;

                case "4":
                    var wDel = PickFromList("Выберите вебинар для удаления:", db.Webinars, w => $"{w.Describe()} [{w.StartTime}]");
                    if (wDel == null) break;
                    await _platform.DeleteWebinarAsync(wDel.Id);
                    PrintOk("Вебинар удалён.");
                    break;

                case "5":
                    var pending = db.Payments.Where(p => p.Status != "Оплачено").ToList();
                    if (pending.Count == 0) { Console.WriteLine("Нет неподтверждённых заявок."); break; }
                    var pay = PickFromList("Выберите заявку:", pending, p => $"{p.Describe()} (студент: {p.StudentId})");
                    if (pay == null) break;
                    var conf = await _platform.ConfirmPaymentAsync(pay.Id);
                    PrintOk($"Оплата подтверждена, студент зачислен (Запись #{conf.Enrollment.Id}).");
                    break;

                case "6":
                    Console.Write("1 — Создать группу, 2 — Перевести студента в группу: ");
                    if (Console.ReadLine()?.Trim() == "1")
                    {
                        var gName = ReadText("Название группы: ");
                        var cur = PickFromList("Куратор:", db.Instructors, i => i.FullName);
                        if (cur == null) break;
                        var gDesc = ReadText("Описание (можно Enter): ", allowEmpty: true);
                        var grp = await _platform.CreateGroupAsync(new CreateGroupRequest(gName, cur.Id, gDesc));
                        PrintOk($"Создана: {grp.Describe()}");
                    }
                    else
                    {
                        var st = PickFromList("Студент:", db.Students, s => s.FullName);
                        if (st == null) break;
                        var grp = PickFromList("Новая группа:", db.Groups, g => g.Name);
                        if (grp == null) break;
                        await _platform.AssignStudentGroupAsync(st.Id, grp.Id);
                        PrintOk($"Студент {st.FullName} переведён в группу «{grp.Name}».");
                    }
                    break;

                case "7":
                    if (db.Inquiries.Count == 0) { Console.WriteLine("Обращений нет."); break; }
                    foreach (var i in db.Inquiries)
                    {
                        Console.WriteLine($"- {i.Describe()} | {i.Email}: {i.Message}");
                    }
                    break;
            }
        }
    }

    private async Task ShowOopSummaryAsync()
    {
        var summary = await _platform.GetOopSummaryAsync();
        Console.WriteLine();
        Console.WriteLine($"=== ПОЛИМОРФНАЯ ООП-СВОДКА (всего сущностей: {summary.TotalEntities}) ===");
        Console.WriteLine("Иерархии наследования:");
        foreach (var note in summary.HierarchyNotes)
        {
            Console.WriteLine($"  * {note}");
        }
        Console.WriteLine("\nПримеры полиморфного вызова Describe() через базовые типы:");
        foreach (var item in summary.Items.Take(20))
        {
            Console.WriteLine($"  [{item.BaseClass} -> {item.ClassName}] {item.PolymorphicDescription} | {item.MetricLabel}: {item.MetricValue}");
        }
    }

    private static void ShowCourses(PlatformDatabase db)
    {
        foreach (var c in db.Courses)
        {
            var inst = db.Instructors.FirstOrDefault(i => i.Id == c.InstructorId);
            Console.WriteLine($"- {c.Title} [{c.Category}, {c.Level}] — {c.PriceRub} руб., {c.DurationHours} ч., рейтинг {c.Rating}");
            Console.WriteLine($"  Преподаватель: {inst?.FullName ?? c.InstructorId} | Уроков: {c.CountAllLessons()}");
        }
    }

    private static void ShowWebinars(PlatformDatabase db)
    {
        if (db.Webinars.Count == 0) { Console.WriteLine("Вебинаров пока нет."); return; }
        foreach (var w in db.Webinars)
        {
            Console.WriteLine($"- {w.Describe()} | Начало: {w.StartTime} | Ссылка: {w.MeetingUrl}");
        }
    }

    private static T? PickFromList<T>(string title, IReadOnlyList<T> items, Func<T, string> labelSelector) where T : class
    {
        if (items.Count == 0)
        {
            PrintWarn("Список пуст.");
            return null;
        }
        Console.WriteLine($"\n{title}");
        for (int i = 0; i < items.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {labelSelector(items[i])}");
        }
        Console.WriteLine("  0. Отмена");
        var idx = ReadInt($"Номер (0-{items.Count}): ", 0, items.Count);
        return idx == 0 ? null : items[idx - 1];
    }

    private static string ReadText(string prompt, bool allowEmpty = false)
    {
        while (true)
        {
            Console.Write(prompt);
            var val = Console.ReadLine()?.Trim() ?? string.Empty;
            if (allowEmpty || !string.IsNullOrWhiteSpace(val)) return val;
            PrintWarn("Поле не может быть пустым.");
        }
    }

    private static int ReadInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write(prompt);
            if (int.TryParse(Console.ReadLine(), out int val) && val >= min && val <= max)
            {
                return val;
            }
            PrintWarn($"Введите целое число от {min} до {max}.");
        }
    }

    private static void PrintOk(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    private static void PrintWarn(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    private static void PrintError(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ResetColor();
    }
}