using System.Text.Json.Serialization;
using AcademyVector.Api.Domain.Exceptions;

namespace AcademyVector.Api.Domain.Entities;

/// <summary>
/// Абстрактный базовый класс учебной активности (урок, тест, задание, вебинар).
/// </summary>
public abstract class LearningActivityBase : DomainEntity
{
    [JsonInclude]
    public string Title { get; protected set; } = string.Empty;

    protected LearningActivityBase()
    {
    }

    protected LearningActivityBase(string id, string title, string? createdAt = null)
        : base(id, createdAt)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationDomainException("Название учебной активности обязательно.");
        }

        Title = title.Trim();
    }

    public abstract string GetActivityKind();
    public abstract int CalculatePotentialPoints();
}

/// <summary>
/// Структурированный раздел учебного урока.
/// </summary>
public sealed class LessonSection
{
    public string Heading { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

/// <summary>
/// Учебный урок внутри модуля курса.
/// </summary>
public sealed class LessonActivity : LearningActivityBase
{
    [JsonInclude]
    public int Order { get; private set; }

    [JsonInclude]
    public int DurationMinutes { get; private set; }

    [JsonInclude]
    public string Summary { get; private set; } = string.Empty;

    [JsonInclude]
    public string Content { get; private set; } = string.Empty;

    [JsonInclude]
    public string CodeExample { get; private set; } = string.Empty;

    [JsonInclude]
    public string KeyTakeaway { get; private set; } = string.Empty;

    [JsonInclude]
    public List<string> LearningGoals { get; private set; } = new();

    [JsonInclude]
    public List<LessonSection> Sections { get; private set; } = new();

    [JsonInclude]
    public List<string> Checklist { get; private set; } = new();

    public LessonActivity()
    {
    }

    public LessonActivity(
        string id,
        string title,
        int order,
        int durationMinutes,
        string summary,
        string content,
        string codeExample,
        string keyTakeaway,
        List<string>? learningGoals = null,
        List<LessonSection>? sections = null,
        List<string>? checklist = null)
        : base(id, title)
    {
        Order = Math.Max(1, order);
        DurationMinutes = Math.Max(5, durationMinutes);
        Summary = summary;
        Content = content;
        CodeExample = codeExample;
        KeyTakeaway = keyTakeaway;
        LearningGoals = learningGoals ?? new();
        Sections = sections ?? new();
        Checklist = checklist ?? new();
    }

    public void UpdateContent(
        string title,
        int durationMinutes,
        string summary,
        string content,
        string codeExample,
        string keyTakeaway)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            throw new ValidationDomainException("Название и основной текст урока обязательны.");
        }

        Title = title.Trim();
        DurationMinutes = Math.Max(5, durationMinutes);
        Summary = summary.Trim();
        Content = content.Trim();
        CodeExample = codeExample;
        KeyTakeaway = keyTakeaway.Trim();
    }

    public override string GetActivityKind() => "Урок";
    public override int CalculatePotentialPoints() => 25;
    public override string Describe() => $"Урок «{Title}» ({DurationMinutes} мин)";
    public override string GetMetricLabel() => "Длительность";
    public override string GetMetricValue() => $"{DurationMinutes} мин";
}

/// <summary>
/// Вопрос интерактивного теста.
/// </summary>
public sealed class QuizQuestion
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectIndex { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

/// <summary>
/// Интерактивный тест (квиз) с порогом прохождения и наградой в баллах.
/// </summary>
public sealed class QuizActivity : LearningActivityBase
{
    [JsonInclude]
    public int PassingScore { get; private set; } = 60;

    [JsonInclude]
    public int PointsReward { get; private set; } = 100;

    [JsonInclude]
    public List<QuizQuestion> Questions { get; private set; } = new();

    public QuizActivity()
    {
    }

    public QuizActivity(
        string id,
        string title,
        int passingScore,
        int pointsReward,
        List<QuizQuestion> questions)
        : base(id, title)
    {
        if (questions.Count == 0)
        {
            throw new ValidationDomainException("Тест должен содержать минимум один вопрос.");
        }

        PassingScore = Math.Clamp(passingScore, 10, 100);
        PointsReward = Math.Max(10, pointsReward);
        Questions = questions;
    }

    public override string GetActivityKind() => "Тест";
    public override int CalculatePotentialPoints() => PointsReward;
    public override string Describe() => $"Тест «{Title}», порог прохождения {PassingScore}%";
    public override string GetMetricLabel() => "Награда";
    public override string GetMetricValue() => $"+{PointsReward} б.";
}

/// <summary>
/// Практическое домашнее задание.
/// </summary>
public sealed class AssignmentActivity : LearningActivityBase
{
    [JsonInclude]
    public string Description { get; private set; } = string.Empty;

    [JsonInclude]
    public int MaxScore { get; private set; } = 100;

    public AssignmentActivity()
    {
    }

    public AssignmentActivity(string id, string title, string description, int maxScore = 100)
        : base(id, title)
    {
        Description = description;
        MaxScore = Math.Clamp(maxScore, 10, 100);
    }

    public override string GetActivityKind() => "Домашнее задание";
    public override int CalculatePotentialPoints() => 50;
    public override string Describe() => $"Практическое ДЗ «{Title}»";
    public override string GetMetricLabel() => "Макс. оценка";
    public override string GetMetricValue() => $"{MaxScore} б.";
}

/// <summary>
/// Живой вебинар или консультация преподавателя.
/// </summary>
public sealed class WebinarActivity : LearningActivityBase
{
    [JsonInclude]
    public string CourseId { get; private set; } = string.Empty;

    [JsonInclude]
    public string InstructorId { get; private set; } = string.Empty;

    [JsonInclude]
    public string StartTime { get; private set; } = string.Empty;

    [JsonInclude]
    public string EndTime { get; private set; } = string.Empty;

    [JsonInclude]
    public string MeetingUrl { get; private set; } = string.Empty;

    [JsonInclude]
    public string Description { get; private set; } = string.Empty;

    public WebinarActivity()
    {
    }

    public WebinarActivity(
        string id,
        string title,
        string courseId,
        string instructorId,
        string startTime,
        string endTime,
        string meetingUrl,
        string description,
        string? createdAt = null)
        : base(id, title, createdAt)
    {
        if (!DateTimeOffset.TryParse(startTime, out var startDto) ||
            !DateTimeOffset.TryParse(endTime, out var endDto) ||
            startDto >= endDto)
        {
            throw new ValidationDomainException("Время окончания вебинара должно быть позже времени начала.");
        }

        if (!Uri.TryCreate(meetingUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ValidationDomainException("Ссылка подключения должна использовать протокол http:// или https://.");
        }

        CourseId = courseId;
        InstructorId = instructorId;
        StartTime = startDto.UtcDateTime.ToString("O");
        EndTime = endDto.UtcDateTime.ToString("O");
        MeetingUrl = meetingUrl.Trim();
        Description = description.Trim();
    }

    public bool OverlapsWith(string otherStartIso, string otherEndIso)
    {
        if (!DateTimeOffset.TryParse(StartTime, out var s1) ||
            !DateTimeOffset.TryParse(EndTime, out var e1) ||
            !DateTimeOffset.TryParse(otherStartIso, out var s2) ||
            !DateTimeOffset.TryParse(otherEndIso, out var e2))
        {
            return false;
        }

        return s2 < e1 && e2 > s1;
    }

    public override string GetActivityKind() => "Вебинар";
    public override int CalculatePotentialPoints() => 0;

    public override string Describe()
    {
        var durationMin = 60;
        if (DateTimeOffset.TryParse(StartTime, out var s) && DateTimeOffset.TryParse(EndTime, out var e))
        {
            durationMin = Math.Max(0, (int)Math.Round((e - s).TotalMinutes));
        }

        return $"Живой вебинар «{Title}» ({durationMin} мин)";
    }

    public override string GetMetricLabel() => "Длительность";

    public override string GetMetricValue()
    {
        if (DateTimeOffset.TryParse(StartTime, out var s) && DateTimeOffset.TryParse(EndTime, out var e))
        {
            return $"{Math.Max(0, (int)Math.Round((e - s).TotalMinutes))} мин";
        }

        return "60 мин";
    }
}
