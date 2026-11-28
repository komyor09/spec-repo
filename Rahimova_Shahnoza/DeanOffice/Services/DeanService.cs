using DeanOffice.Models;

namespace DeanOffice.Services;

public class GroupStat
{
    public string Group { get; set; } = "";
    public int StudentCount { get; set; }
    public double AverageGrade { get; set; }
    public int DebtorsCount { get; set; }
    public double SuccessPercent { get; set; }
}

/// <summary>Вся бизнес-логика деканата: студенты, предметы, результаты сессии.</summary>
public class DeanService
{
    private readonly DeanData _data;

    public DeanService(DeanData data)
    {
        _data = data;
    }

    public IReadOnlyList<Student> Students => _data.Students;
    public IReadOnlyList<Subject> Subjects => _data.Subjects;

    // ---------- Студенты ----------

    public Student AddStudent(string fullName, string group, int course)
    {
        int id = _data.Students.Count == 0 ? 1 : _data.Students.Max(s => s.Id) + 1;
        var student = new Student { Id = id, FullName = fullName.Trim(), Group = group.Trim(), Course = course };
        _data.Students.Add(student);
        return student;
    }

    public bool RemoveStudent(int id)
    {
        var student = FindStudent(id);
        if (student == null) return false;
        _data.Students.Remove(student);
        _data.Results.RemoveAll(r => r.StudentId == id);
        return true;
    }

    public Student? FindStudent(int id) => _data.Students.FirstOrDefault(s => s.Id == id);

    public List<Student> SearchStudents(string text)
    {
        return _data.Students
            .Where(s => s.FullName.Contains(text, StringComparison.OrdinalIgnoreCase)
                     || s.Group.Contains(text, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.FullName)
            .ToList();
    }

    // ---------- Предметы ----------

    public Subject AddSubject(string name, ControlType control, int course)
    {
        int id = _data.Subjects.Count == 0 ? 1 : _data.Subjects.Max(s => s.Id) + 1;
        var subject = new Subject { Id = id, Name = name.Trim(), Control = control, Course = course };
        _data.Subjects.Add(subject);
        return subject;
    }

    public Subject? FindSubject(int id) => _data.Subjects.FirstOrDefault(s => s.Id == id);

    // ---------- Результаты ----------

    /// <summary>Выставить (или заменить) оценку. Возвращает текст ошибки или null, если всё хорошо.</summary>
    public string? SetResult(int studentId, int subjectId, int grade)
    {
        var student = FindStudent(studentId);
        if (student == null) return "Студент не найден.";

        var subject = FindSubject(subjectId);
        if (subject == null) return "Предмет не найден.";

        if (subject.Control == ControlType.Exam && (grade < 2 || grade > 5))
            return "Оценка за экзамен должна быть от 2 до 5.";
        if (subject.Control == ControlType.Credit && (grade < 0 || grade > 1))
            return "За зачёт: 1 - зачёт, 0 - незачёт.";

        var existing = _data.Results.FirstOrDefault(r => r.StudentId == studentId && r.SubjectId == subjectId);
        if (existing != null)
        {
            existing.Grade = grade;
            existing.Date = DateTime.Today;
        }
        else
        {
            _data.Results.Add(new ExamResult
            {
                StudentId = studentId,
                SubjectId = subjectId,
                Grade = grade,
                Date = DateTime.Today
            });
        }
        return null;
    }

    public List<ExamResult> GetResults(int studentId)
    {
        return _data.Results.Where(r => r.StudentId == studentId).OrderBy(r => r.SubjectId).ToList();
    }

    public static bool IsPassed(ExamResult result, Subject subject)
    {
        return subject.Control == ControlType.Exam ? result.Grade >= 3 : result.Grade == 1;
    }

    public static string GradeText(ExamResult result, Subject subject)
    {
        if (subject.Control == ControlType.Credit)
            return result.Grade == 1 ? "зачёт" : "незачёт";
        return result.Grade.ToString();
    }

    /// <summary>Средний балл по экзаменам (зачёты не считаются). 0, если экзаменов нет.</summary>
    public double GetAverage(int studentId)
    {
        var grades = new List<int>();
        foreach (var r in GetResults(studentId))
        {
            var subject = FindSubject(r.SubjectId);
            if (subject != null && subject.Control == ControlType.Exam)
                grades.Add(r.Grade);
        }
        return grades.Count == 0 ? 0 : grades.Average();
    }

    /// <summary>Предметы курса студента, которые не сданы (нет результата или неуд.).</summary>
    public List<Subject> GetDebts(Student student)
    {
        var debts = new List<Subject>();
        foreach (var subject in _data.Subjects.Where(s => s.Course == student.Course))
        {
            var result = _data.Results.FirstOrDefault(r => r.StudentId == student.Id && r.SubjectId == subject.Id);
            if (result == null || !IsPassed(result, subject))
                debts.Add(subject);
        }
        return debts;
    }

    public List<Student> GetDebtors()
    {
        return _data.Students.Where(s => GetDebts(s).Count > 0).OrderBy(s => s.Group).ThenBy(s => s.FullName).ToList();
    }

    public List<GroupStat> GetGroupStats()
    {
        var stats = new List<GroupStat>();
        foreach (var g in _data.Students.GroupBy(s => s.Group).OrderBy(g => g.Key))
        {
            var members = g.ToList();
            var averages = members.Select(s => GetAverage(s.Id)).Where(a => a > 0).ToList();
            int debtors = members.Count(s => GetDebts(s).Count > 0);

            stats.Add(new GroupStat
            {
                Group = g.Key,
                StudentCount = members.Count,
                AverageGrade = averages.Count == 0 ? 0 : averages.Average(),
                DebtorsCount = debtors,
                SuccessPercent = members.Count == 0 ? 0 : (members.Count - debtors) * 100.0 / members.Count
            });
        }
        return stats;
    }

    /// <summary>Рейтинг студентов по среднему баллу.</summary>
    public List<(Student Student, double Average)> GetRating()
    {
        return _data.Students
            .Select(s => (Student: s, Average: GetAverage(s.Id)))
            .Where(x => x.Average > 0)
            .OrderByDescending(x => x.Average)
            .ThenBy(x => x.Student.FullName)
            .ToList();
    }
}
