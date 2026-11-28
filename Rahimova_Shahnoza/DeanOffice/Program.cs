using System.Text;
using DeanOffice.Models;
using DeanOffice.Services;

namespace DeanOffice;

public static class Program
{
    private static DeanService _service = null!;
    private static DeanData _data = null!;

    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        _data = Storage.Load();
        _service = new DeanService(_data);
        Storage.Save(_data);

        while (true)
        {
            ShowMenu();
            int choice = ReadInt("Выберите пункт", 0, 11);
            Console.WriteLine();

            switch (choice)
            {
                case 1: ShowStudents(); break;
                case 2: AddStudent(); break;
                case 3: RemoveStudent(); break;
                case 4: SearchStudent(); break;
                case 5: ShowSubjects(); break;
                case 6: AddSubject(); break;
                case 7: SetResult(); break;
                case 8: ShowStudentSession(); break;
                case 9: ShowDebtors(); break;
                case 10: ShowGroupStats(); break;
                case 11: ShowRating(); break;
                case 0:
                    Storage.Save(_data);
                    Console.WriteLine("Данные сохранены. До свидания!");
                    return;
            }

            Storage.Save(_data);
            Console.WriteLine();
            Console.Write("Нажмите Enter, чтобы продолжить...");
            Console.ReadLine();
        }
    }

    // ---------- Меню ----------

    private static void ShowMenu()
    {
        Console.Clear();
        Console.WriteLine("==========================================");
        Console.WriteLine("   ДЕКАНАТ: СТУДЕНТЫ И ЭКЗАМЕНАЦИОННАЯ СЕССИЯ");
        Console.WriteLine("==========================================");
        Console.WriteLine(" 1  - Список студентов");
        Console.WriteLine(" 2  - Добавить студента");
        Console.WriteLine(" 3  - Удалить студента");
        Console.WriteLine(" 4  - Поиск студента (по ФИО или группе)");
        Console.WriteLine(" 5  - Список предметов сессии");
        Console.WriteLine(" 6  - Добавить предмет");
        Console.WriteLine(" 7  - Выставить оценку / зачёт");
        Console.WriteLine(" 8  - Результаты сессии студента");
        Console.WriteLine(" 9  - Должники (академические задолженности)");
        Console.WriteLine(" 10 - Статистика по группам");
        Console.WriteLine(" 11 - Рейтинг студентов по среднему баллу");
        Console.WriteLine(" 0  - Выход");
        Console.WriteLine("------------------------------------------");
    }

    // ---------- Студенты ----------

    private static void ShowStudents()
    {
        PrintStudents(_service.Students.OrderBy(s => s.Group).ThenBy(s => s.FullName).ToList());
    }

    private static void PrintStudents(List<Student> students)
    {
        if (students.Count == 0)
        {
            Console.WriteLine("Студентов нет.");
            return;
        }
        Console.WriteLine($"{"ID",-4}{"ФИО",-34}{"Группа",-10}{"Курс",-5}");
        Console.WriteLine(new string('-', 53));
        foreach (var s in students)
            Console.WriteLine($"{s.Id,-4}{s.FullName,-34}{s.Group,-10}{s.Course,-5}");
    }

    private static void AddStudent()
    {
        string name = ReadString("ФИО студента");
        string group = ReadString("Группа");
        int course = ReadInt("Курс (1-6)", 1, 6);
        var s = _service.AddStudent(name, group, course);
        Console.WriteLine($"Студент добавлен, ID = {s.Id}.");
    }

    private static void RemoveStudent()
    {
        ShowStudents();
        Console.WriteLine();
        int id = ReadInt("ID студента для удаления (0 - отмена)", 0, int.MaxValue);
        if (id == 0) return;
        Console.WriteLine(_service.RemoveStudent(id)
            ? "Студент и его результаты удалены."
            : "Студент с таким ID не найден.");
    }

    private static void SearchStudent()
    {
        string text = ReadString("Введите часть ФИО или группу");
        PrintStudents(_service.SearchStudents(text));
    }

    // ---------- Предметы ----------

    private static void ShowSubjects()
    {
        if (_service.Subjects.Count == 0)
        {
            Console.WriteLine("Предметов нет.");
            return;
        }
        Console.WriteLine($"{"ID",-4}{"Предмет",-28}{"Форма",-10}{"Курс",-5}");
        Console.WriteLine(new string('-', 47));
        foreach (var s in _service.Subjects.OrderBy(s => s.Course).ThenBy(s => s.Name))
            Console.WriteLine($"{s.Id,-4}{s.Name,-28}{ControlName(s.Control),-10}{s.Course,-5}");
    }

    private static void AddSubject()
    {
        string name = ReadString("Название предмета");
        int type = ReadInt("Форма контроля (1 - экзамен, 2 - зачёт)", 1, 2);
        int course = ReadInt("Курс (1-6)", 1, 6);
        var s = _service.AddSubject(name, type == 1 ? ControlType.Exam : ControlType.Credit, course);
        Console.WriteLine($"Предмет добавлен, ID = {s.Id}.");
    }

    private static string ControlName(ControlType c) => c == ControlType.Exam ? "экзамен" : "зачёт";

    // ---------- Результаты сессии ----------

    private static void SetResult()
    {
        if (_service.Students.Count == 0 || _service.Subjects.Count == 0)
        {
            Console.WriteLine("Сначала добавьте хотя бы одного студента и один предмет.");
            return;
        }

        ShowStudents();
        Console.WriteLine();
        int studentId = ReadInt("ID студента", 1, int.MaxValue);
        var student = _service.FindStudent(studentId);
        if (student == null)
        {
            Console.WriteLine("Студент не найден.");
            return;
        }

        Console.WriteLine();
        ShowSubjects();
        Console.WriteLine();
        int subjectId = ReadInt("ID предмета", 1, int.MaxValue);
        var subject = _service.FindSubject(subjectId);
        if (subject == null)
        {
            Console.WriteLine("Предмет не найден.");
            return;
        }

        int grade;
        if (subject.Control == ControlType.Exam)
            grade = ReadInt("Оценка (2-5)", 2, 5);
        else
            grade = ReadInt("Результат (1 - зачёт, 0 - незачёт)", 0, 1);

        string? error = _service.SetResult(studentId, subjectId, grade);
        Console.WriteLine(error ?? "Результат сохранён.");
    }

    private static void ShowStudentSession()
    {
        ShowStudents();
        Console.WriteLine();
        int id = ReadInt("ID студента", 1, int.MaxValue);
        var student = _service.FindStudent(id);
        if (student == null)
        {
            Console.WriteLine("Студент не найден.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Студент: {student.FullName}, группа {student.Group}, {student.Course} курс");
        Console.WriteLine();

        var results = _service.GetResults(id);
        if (results.Count == 0)
        {
            Console.WriteLine("Результатов пока нет.");
        }
        else
        {
            Console.WriteLine($"{"Предмет",-28}{"Форма",-10}{"Результат",-11}{"Дата",-12}");
            Console.WriteLine(new string('-', 61));
            foreach (var r in results)
            {
                var subject = _service.FindSubject(r.SubjectId);
                if (subject == null) continue;
                Console.WriteLine($"{subject.Name,-28}{ControlName(subject.Control),-10}" +
                                  $"{DeanService.GradeText(r, subject),-11}{r.Date:dd.MM.yyyy}");
            }
        }

        Console.WriteLine();
        double avg = _service.GetAverage(id);
        Console.WriteLine(avg > 0 ? $"Средний балл по экзаменам: {avg:F2}" : "Средний балл: нет экзаменов.");

        var debts = _service.GetDebts(student);
        if (debts.Count == 0)
            Console.WriteLine("Задолженностей нет. Сессия сдана!");
        else
            Console.WriteLine("Задолженности: " + string.Join(", ", debts.Select(d => d.Name)));
    }

    private static void ShowDebtors()
    {
        var debtors = _service.GetDebtors();
        if (debtors.Count == 0)
        {
            Console.WriteLine("Должников нет.");
            return;
        }
        Console.WriteLine($"{"Группа",-10}{"ФИО",-34}Задолженности");
        Console.WriteLine(new string('-', 80));
        foreach (var s in debtors)
        {
            string debts = string.Join(", ", _service.GetDebts(s).Select(d => d.Name));
            Console.WriteLine($"{s.Group,-10}{s.FullName,-34}{debts}");
        }
        Console.WriteLine();
        Console.WriteLine($"Всего должников: {debtors.Count}");
    }

    private static void ShowGroupStats()
    {
        var stats = _service.GetGroupStats();
        if (stats.Count == 0)
        {
            Console.WriteLine("Нет данных.");
            return;
        }
        Console.WriteLine($"{"Группа",-10}{"Студентов",-11}{"Ср. балл",-10}{"Должников",-11}{"Успеваемость",-12}");
        Console.WriteLine(new string('-', 54));
        foreach (var g in stats)
            Console.WriteLine($"{g.Group,-10}{g.StudentCount,-11}{g.AverageGrade,-10:F2}{g.DebtorsCount,-11}{g.SuccessPercent:F0}%");
    }

    private static void ShowRating()
    {
        var rating = _service.GetRating();
        if (rating.Count == 0)
        {
            Console.WriteLine("Нет оценок для рейтинга.");
            return;
        }
        Console.WriteLine($"{"#",-4}{"ФИО",-34}{"Группа",-10}{"Ср. балл",-8}");
        Console.WriteLine(new string('-', 56));
        int place = 1;
        foreach (var (student, average) in rating)
            Console.WriteLine($"{place++,-4}{student.FullName,-34}{student.Group,-10}{average:F2}");
    }

    // ---------- Ввод ----------

    private static string ReadString(string prompt)
    {
        while (true)
        {
            Console.Write(prompt + ": ");
            string? text = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(text)) return text.Trim();
            Console.WriteLine("Строка не должна быть пустой.");
        }
    }

    private static int ReadInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write(prompt + ": ");
            string? text = Console.ReadLine();
            if (int.TryParse(text, out int value) && value >= min && value <= max)
                return value;
            Console.WriteLine(max == int.MaxValue
                ? $"Введите целое число не меньше {min}."
                : $"Введите целое число от {min} до {max}.");
        }
    }
}
