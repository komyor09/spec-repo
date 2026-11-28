using System.Text.Json;
using DeanOffice.Models;

namespace DeanOffice.Services;

public static class Storage
{
    private static readonly string FilePath =
        Path.Combine(AppContext.BaseDirectory, "data.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static DeanData Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                var data = JsonSerializer.Deserialize<DeanData>(json, Options);
                if (data != null) return data;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Не удалось прочитать data.json: " + ex.Message);
        }
        return SeedData();
    }

    public static void Save(DeanData data)
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(data, Options));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка сохранения: " + ex.Message);
        }
    }

    /// <summary>Тестовые данные для первого запуска.</summary>
    private static DeanData SeedData()
    {
        var d = new DeanData();

        d.Students.Add(new Student { Id = 1, FullName = "Иванов Иван Иванович", Group = "ИС-21", Course = 2 });
        d.Students.Add(new Student { Id = 2, FullName = "Петрова Анна Сергеевна", Group = "ИС-21", Course = 2 });
        d.Students.Add(new Student { Id = 3, FullName = "Сидоров Олег Петрович", Group = "ИС-21", Course = 2 });
        d.Students.Add(new Student { Id = 4, FullName = "Каримова Малика Рустамовна", Group = "ПО-31", Course = 3 });
        d.Students.Add(new Student { Id = 5, FullName = "Алиев Джамшед Фарходович", Group = "ПО-31", Course = 3 });

        d.Subjects.Add(new Subject { Id = 1, Name = "Математика", Control = ControlType.Exam, Course = 2 });
        d.Subjects.Add(new Subject { Id = 2, Name = "Программирование", Control = ControlType.Exam, Course = 2 });
        d.Subjects.Add(new Subject { Id = 3, Name = "Физкультура", Control = ControlType.Credit, Course = 2 });
        d.Subjects.Add(new Subject { Id = 4, Name = "Базы данных", Control = ControlType.Exam, Course = 3 });
        d.Subjects.Add(new Subject { Id = 5, Name = "Английский язык", Control = ControlType.Credit, Course = 3 });

        var date = DateTime.Today;
        d.Results.Add(new ExamResult { StudentId = 1, SubjectId = 1, Grade = 5, Date = date });
        d.Results.Add(new ExamResult { StudentId = 1, SubjectId = 2, Grade = 4, Date = date });
        d.Results.Add(new ExamResult { StudentId = 1, SubjectId = 3, Grade = 1, Date = date });
        d.Results.Add(new ExamResult { StudentId = 2, SubjectId = 1, Grade = 4, Date = date });
        d.Results.Add(new ExamResult { StudentId = 2, SubjectId = 2, Grade = 5, Date = date });
        d.Results.Add(new ExamResult { StudentId = 2, SubjectId = 3, Grade = 1, Date = date });
        d.Results.Add(new ExamResult { StudentId = 3, SubjectId = 1, Grade = 2, Date = date });
        d.Results.Add(new ExamResult { StudentId = 4, SubjectId = 4, Grade = 5, Date = date });
        d.Results.Add(new ExamResult { StudentId = 4, SubjectId = 5, Grade = 1, Date = date });
        d.Results.Add(new ExamResult { StudentId = 5, SubjectId = 4, Grade = 3, Date = date });

        return d;
    }
}
