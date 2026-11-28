namespace DeanOffice.Models;

/// <summary>Форма контроля: экзамен (оценка 2-5) или зачёт (0 - незачёт, 1 - зачёт).</summary>
public enum ControlType
{
    Exam = 0,
    Credit = 1
}

public class Student
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Group { get; set; } = "";
    public int Course { get; set; }
}

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ControlType Control { get; set; }
    public int Course { get; set; }
}

public class ExamResult
{
    public int StudentId { get; set; }
    public int SubjectId { get; set; }
    public int Grade { get; set; }
    public DateTime Date { get; set; }
}

/// <summary>Все данные программы (сохраняются в data.json).</summary>
public class DeanData
{
    public List<Student> Students { get; set; } = new();
    public List<Subject> Subjects { get; set; } = new();
    public List<ExamResult> Results { get; set; } = new();
}
