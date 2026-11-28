using System.Text.Json.Serialization;
using AcademyVector.ConsoleApp.Domain.Exceptions;

namespace AcademyVector.ConsoleApp.Domain.Entities;

public sealed class CourseModule : DomainEntity
{
    [JsonInclude]
    public string Title { get; private set; } = string.Empty;

    [JsonInclude]
    public int Order { get; private set; }

    [JsonInclude]
    public List<LessonActivity> Lessons { get; private set; } = new();

    [JsonInclude]
    public List<QuizActivity> Quizzes { get; private set; } = new();

    [JsonInclude]
    public List<AssignmentActivity> Assignments { get; private set; } = new();

    public CourseModule()
    {
    }

    public CourseModule(string id, string title, int order) : base(id)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationDomainException("Название модуля обязательно.");
        }

        Title = title.Trim();
        Order = Math.Max(1, order);
    }

    public void AddLesson(LessonActivity lesson) => Lessons.Add(lesson);
    public void AddQuiz(QuizActivity quiz) => Quizzes.Add(quiz);
    public void AddAssignment(AssignmentActivity assignment) => Assignments.Add(assignment);

    public override string Describe() => $"Модуль «{Title}» (уроков: {Lessons.Count})";
}

public sealed class Course : DomainEntity
{
    [JsonInclude]
    public string Title { get; private set; } = string.Empty;

    [JsonInclude]
    public string Slug { get; private set; } = string.Empty;

    [JsonInclude]
    public string Category { get; private set; } = string.Empty;

    [JsonInclude]
    public string Level { get; private set; } = string.Empty;

    [JsonInclude]
    public string Description { get; private set; } = string.Empty;

    [JsonInclude]
    public string CoverUrl { get; private set; } = string.Empty;

    [JsonInclude]
    public string InstructorId { get; private set; } = string.Empty;

    [JsonInclude]
    public int PriceRub { get; private set; }

    [JsonInclude]
    public int DurationHours { get; private set; }

    [JsonInclude]
    public double Rating { get; private set; } = 5.0;

    [JsonInclude]
    public int StudentsCount { get; private set; }

    [JsonInclude]
    public string FormatLabel { get; private set; } = string.Empty;

    [JsonInclude]
    public List<CourseModule> Modules { get; private set; } = new();

    public Course()
    {
    }

    public Course(
        string id,
        string title,
        string slug,
        string category,
        string level,
        string description,
        string coverUrl,
        string instructorId,
        int priceRub,
        int durationHours,
        double rating = 5.0,
        int studentsCount = 0,
        string? createdAt = null,
        string formatLabel = "")
        : base(id, createdAt)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 4)
        {
            throw new ValidationDomainException("Название курса должно содержать минимум 4 символа.");
        }

        if (priceRub < 0)
        {
            throw new ValidationDomainException("Стоимость курса не может быть отрицательной.");
        }

        if (durationHours <= 0)
        {
            throw new ValidationDomainException("Длительность курса должна быть больше нуля.");
        }

        Title = title.Trim();
        Slug = slug;
        Category = category;
        Level = level;
        FormatLabel = formatLabel;
        Description = description.Trim();
        CoverUrl = coverUrl;
        InstructorId = instructorId;
        PriceRub = priceRub;
        DurationHours = durationHours;
        Rating = rating;
        StudentsCount = Math.Max(0, studentsCount);
    }

    public void AddModule(CourseModule module) => Modules.Add(module);
    public void IncrementStudents() => StudentsCount += 1;

    public int CountAllLessons() => Modules.Sum(m => m.Lessons.Count);

    public override string Describe() =>
        $"Курс «{Title}» ({Category}, {Level}), модулей: {Modules.Count}";
}

public sealed class QuizAttemptResult
{
    public string QuizId { get; set; } = string.Empty;
    public int Score { get; set; }
    public int MaxScore { get; set; } = 100;
    public bool Passed { get; set; }
    public int PointsEarned { get; set; }
    public List<int> Answers { get; set; } = new();
    public string SubmittedAt { get; set; } = string.Empty;
}

public sealed class Enrollment : DomainEntity
{
    [JsonInclude]
    public string StudentId { get; private set; } = string.Empty;

    [JsonInclude]
    public string CourseId { get; private set; } = string.Empty;

    [JsonInclude]
    public string EnrolledAt { get; private set; } = string.Empty;

    [JsonInclude]
    public List<string> CompletedLessonIds { get; private set; } = new();

    [JsonInclude]
    public Dictionary<string, QuizAttemptResult> QuizResults { get; private set; } = new();

    [JsonInclude]
    public Dictionary<string, string> LessonNotes { get; private set; } = new();

    [JsonInclude]
    public int ProgressPercent { get; private set; }

    [JsonInclude]
    public string? CertificateNumber { get; private set; }

    [JsonInclude]
    public string? CompletedAt { get; private set; }

    public Enrollment()
    {
    }

    public Enrollment(
        string id,
        string studentId,
        string courseId,
        string? enrolledAt = null,
        List<string>? completedLessonIds = null,
        Dictionary<string, QuizAttemptResult>? quizResults = null,
        int progressPercent = 0,
        string? certificateNumber = null,
        string? completedAt = null,
        Dictionary<string, string>? lessonNotes = null)
        : base(id, enrolledAt)
    {
        StudentId = studentId;
        CourseId = courseId;
        EnrolledAt = string.IsNullOrWhiteSpace(enrolledAt) ? DateTime.UtcNow.ToString("O") : enrolledAt;
        CompletedLessonIds = completedLessonIds ?? new();
        QuizResults = quizResults ?? new();
        LessonNotes = lessonNotes ?? new();
        ProgressPercent = Math.Clamp(progressPercent, 0, 100);
        CertificateNumber = certificateNumber;
        CompletedAt = completedAt;
    }

    public bool MarkLessonCompleted(string lessonId)
    {
        if (CompletedLessonIds.Contains(lessonId))
        {
            return false;
        }

        CompletedLessonIds.Add(lessonId);
        return true;
    }

    public void SaveLessonNote(string lessonId, string note)
    {
        if (string.IsNullOrWhiteSpace(lessonId))
        {
            throw new ValidationDomainException("Идентификатор урока обязателен.");
        }

        LessonNotes[lessonId] = (note ?? string.Empty).Trim();
    }

    public void UpdateProgress(int newProgressPercent, string? generatedCertificateNumber)
    {
        ProgressPercent = Math.Clamp(newProgressPercent, 0, 100);
        if (ProgressPercent == 100 && string.IsNullOrWhiteSpace(CertificateNumber))
        {
            CertificateNumber = generatedCertificateNumber;
            CompletedAt = DateTime.UtcNow.ToString("O");
        }
    }

    public void RecordQuizResult(string quizId, QuizAttemptResult result)
    {
        QuizResults[quizId] = result;
    }

    public override string Describe() =>
        $"Запись студента {StudentId} на курс {CourseId} — прогресс {ProgressPercent}%";
}

public sealed class HomeworkSubmission : DomainEntity
{
    [JsonInclude]
    public string StudentId { get; private set; } = string.Empty;

    [JsonInclude]
    public string CourseId { get; private set; } = string.Empty;

    [JsonInclude]
    public string AssignmentId { get; private set; } = string.Empty;

    [JsonInclude]
    public string SubmissionText { get; private set; } = string.Empty;

    [JsonInclude]
    public string SubmissionUrl { get; private set; } = string.Empty;

    [JsonInclude]
    public string Status { get; private set; } = "На проверке";

    [JsonInclude]
    public int? Score { get; private set; }

    [JsonInclude]
    public string TeacherComment { get; private set; } = string.Empty;

    [JsonInclude]
    public string SubmittedAt { get; private set; } = string.Empty;

    [JsonInclude]
    public string? ReviewedAt { get; private set; }

    public HomeworkSubmission()
    {
    }

    public HomeworkSubmission(
        string id,
        string studentId,
        string courseId,
        string assignmentId,
        string submissionText,
        string submissionUrl,
        string status = "На проверке",
        int? score = null,
        string teacherComment = "",
        string? submittedAt = null,
        string? reviewedAt = null)
        : base(id, submittedAt)
    {
        StudentId = studentId;
        CourseId = courseId;
        AssignmentId = assignmentId;
        SubmissionText = submissionText.Trim();
        SubmissionUrl = submissionUrl.Trim();
        Status = status;
        Score = score;
        TeacherComment = teacherComment;
        SubmittedAt = string.IsNullOrWhiteSpace(submittedAt) ? DateTime.UtcNow.ToString("O") : submittedAt;
        ReviewedAt = reviewedAt;
    }

    public void Resubmit(string text, string url)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ValidationDomainException("Заполните текстовое описание решения.");
        }

        SubmissionText = text.Trim();
        SubmissionUrl = url.Trim();
        Status = "На проверке";
        Score = null;
        TeacherComment = string.Empty;
        SubmittedAt = DateTime.UtcNow.ToString("O");
        ReviewedAt = null;
    }

    public bool Review(string newStatus, int score, string teacherComment)
    {
        if (newStatus != "Принято" && newStatus != "Нужна доработка")
        {
            throw new ValidationDomainException("Допустимые статусы проверки: Принято или Нужна доработка.");
        }

        if (score is < 0 or > 100)
        {
            throw new ValidationDomainException("Оценка должна быть в диапазоне от 0 до 100.");
        }

        if (newStatus == "Нужна доработка" && string.IsNullOrWhiteSpace(teacherComment))
        {
            throw new ValidationDomainException("При возврате на доработку укажите комментарий преподавателя.");
        }

        var newlyAccepted = Status != "Принято" && newStatus == "Принято";
        Status = newStatus;
        Score = score;
        TeacherComment = teacherComment.Trim();
        ReviewedAt = DateTime.UtcNow.ToString("O");
        return newlyAccepted;
    }

    public override string Describe() =>
        $"Решение задания {AssignmentId} от студента {StudentId}: {Status}";
}

public sealed class PaymentRecord : DomainEntity
{
    [JsonInclude]
    public string StudentId { get; private set; } = string.Empty;

    [JsonInclude]
    public string CourseId { get; private set; } = string.Empty;

    [JsonInclude]
    public int AmountRub { get; private set; }

    [JsonInclude]
    public string PaymentType { get; private set; } = "Мгновенная оплата";

    [JsonInclude]
    public string Status { get; private set; } = "Оплачено";

    [JsonInclude]
    public string? ConfirmedAt { get; private set; }

    public PaymentRecord()
    {
    }

    public PaymentRecord(
        string id,
        string studentId,
        string courseId,
        int amountRub,
        string paymentType,
        string status,
        string? createdAt = null,
        string? confirmedAt = null)
        : base(id, createdAt)
    {
        StudentId = studentId;
        CourseId = courseId;
        AmountRub = Math.Max(0, amountRub);
        PaymentType = paymentType;
        Status = status;
        ConfirmedAt = confirmedAt;
    }

    public void Confirm()
    {
        Status = "Оплачено";
        ConfirmedAt = DateTime.UtcNow.ToString("O");
    }

    public override string Describe() =>
        $"Платёж {AmountRub} ₽ ({PaymentType}) — {Status}";
}

public sealed class MonthlyDynamic
{
    public string Month { get; set; } = string.Empty;
    public int Enrollments { get; set; }
    public int Graduations { get; set; }
}

public sealed class PlatformReview
{
    public string Id { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string Quote { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
    public string CreatedAt { get; set; } = string.Empty;
}

public sealed class GuestInquiry : DomainEntity
{
    [JsonInclude]
    public string Name { get; private set; } = string.Empty;

    [JsonInclude]
    public string Email { get; private set; } = string.Empty;

    [JsonInclude]
    public string Topic { get; private set; } = string.Empty;

    [JsonInclude]
    public string Message { get; private set; } = string.Empty;

    public GuestInquiry()
    {
    }

    public GuestInquiry(string id, string name, string email, string topic, string message, string? createdAt = null)
        : base(id, createdAt)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2)
        {
            throw new ValidationDomainException("Укажите ваше имя (минимум 2 символа).");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || !email.Contains('.'))
        {
            throw new ValidationDomainException("Укажите корректный email для ответа.");
        }

        if (string.IsNullOrWhiteSpace(message) || message.Trim().Length < 10)
        {
            throw new ValidationDomainException("Опишите ваш вопрос подробнее (от 10 символов).");
        }

        Name = name.Trim();
        Email = email.Trim();
        Topic = string.IsNullOrWhiteSpace(topic) ? "Консультация по обучению" : topic.Trim();
        Message = message.Trim();
    }

    public override string Describe() => $"Обращение от {Name} ({Topic})";
}

public sealed class PlatformDatabase
{
    public List<Instructor> Instructors { get; set; } = new();
    public List<Student> Students { get; set; } = new();
    public List<StudyGroup> Groups { get; set; } = new();
    public List<Course> Courses { get; set; } = new();
    public List<Enrollment> Enrollments { get; set; } = new();
    public List<HomeworkSubmission> Homeworks { get; set; } = new();
    public List<WebinarActivity> Webinars { get; set; } = new();
    public List<PaymentRecord> Payments { get; set; } = new();
    public List<MonthlyDynamic> MonthlyDynamics { get; set; } = new();
    public List<PlatformReview> Reviews { get; set; } = new();
    public List<GuestInquiry> Inquiries { get; set; } = new();
}
