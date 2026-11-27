using System.Text.Json.Serialization;
using AcademyVector.Api.Domain.Exceptions;

namespace AcademyVector.Api.Domain.Entities;

/// <summary>
/// Абстрактный базовый класс любой доменной сущности платформы.
/// </summary>
public abstract class DomainEntity
{
    [JsonInclude]
    public string Id { get; protected set; } = string.Empty;

    [JsonInclude]
    public string CreatedAt { get; protected set; } = DateTime.UtcNow.ToString("O");

    protected DomainEntity()
    {
    }

    protected DomainEntity(string id, string? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ValidationDomainException("Идентификатор доменной сущности не может быть пустым.");
        }

        Id = id.Trim();
        CreatedAt = string.IsNullOrWhiteSpace(createdAt) ? DateTime.UtcNow.ToString("O") : createdAt;
    }

    public abstract string Describe();
    public virtual string GetMetricLabel() => "Сущность";
    public virtual string GetMetricValue() => Id;
}

/// <summary>
/// Абстрактный базовый класс пользователя образовательной платформы.
/// </summary>
public abstract class UserBase : DomainEntity
{
    [JsonInclude]
    public string FullName { get; protected set; } = string.Empty;

    [JsonInclude]
    public string Email { get; protected set; } = string.Empty;

    protected UserBase()
    {
    }

    protected UserBase(string id, string fullName, string email, string? createdAt = null)
        : base(id, createdAt)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ValidationDomainException("Имя пользователя обязательно для заполнения.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new ValidationDomainException("Укажите корректный адрес электронной почты.");
        }

        FullName = fullName.Trim();
        Email = email.Trim();
    }

    public abstract string GetRoleLabel();
}

/// <summary>
/// Сущность студента с инкапсулированным начислением баллов и назначением в группу.
/// </summary>
public sealed class Student : UserBase
{
    [JsonInclude]
    public string? GroupId { get; private set; }

    [JsonInclude]
    public string AvatarInitials { get; private set; } = string.Empty;

    [JsonInclude]
    public int TotalPoints { get; private set; }

    public Student()
    {
    }

    public Student(
        string id,
        string fullName,
        string email,
        string? groupId,
        string avatarInitials,
        int totalPoints = 0,
        string? createdAt = null)
        : base(id, fullName, email, createdAt)
    {
        GroupId = groupId;
        AvatarInitials = avatarInitials;
        TotalPoints = Math.Max(0, totalPoints);
    }

    public void AssignToGroup(string? groupId)
    {
        GroupId = string.IsNullOrWhiteSpace(groupId) ? null : groupId.Trim();
    }

    public void AddPoints(int points)
    {
        if (points < 0)
        {
            throw new ValidationDomainException("Нельзя начислить отрицательное количество баллов.");
        }

        TotalPoints += points;
    }

    public override string GetRoleLabel() => "Студент";

    public override string Describe() =>
        $"Студент «{FullName}» ({Email}), начислено {TotalPoints} XP";

    public override string GetMetricLabel() => "Баллы";
    public override string GetMetricValue() => $"{TotalPoints} XP";
}

/// <summary>
/// Сущность преподавателя и куратора курсов.
/// </summary>
public sealed class Instructor : UserBase
{
    [JsonInclude]
    public string Title { get; private set; } = string.Empty;

    [JsonInclude]
    public string Bio { get; private set; } = string.Empty;

    [JsonInclude]
    public string AvatarUrl { get; private set; } = string.Empty;

    [JsonInclude]
    public string Expertise { get; private set; } = string.Empty;

    [JsonInclude]
    public int ExperienceYears { get; private set; } = 5;

    [JsonInclude]
    public string Education { get; private set; } = string.Empty;

    [JsonInclude]
    public List<string> CareerHistory { get; private set; } = new();

    [JsonInclude]
    public List<string> Achievements { get; private set; } = new();

    [JsonInclude]
    public List<string> Stack { get; private set; } = new();

    public Instructor()
    {
    }

    public Instructor(
        string id,
        string fullName,
        string email,
        string title,
        string bio,
        string avatarUrl,
        string expertise,
        int experienceYears = 5,
        string education = "",
        List<string>? careerHistory = null,
        List<string>? achievements = null,
        List<string>? stack = null,
        string? createdAt = null)
        : base(id, fullName, email, createdAt)
    {
        Title = title;
        Bio = bio;
        AvatarUrl = avatarUrl;
        Expertise = expertise;
        ExperienceYears = Math.Max(1, experienceYears);
        Education = education;
        CareerHistory = careerHistory ?? new();
        Achievements = achievements ?? new();
        Stack = stack ?? new();
    }

    public override string GetRoleLabel() => "Преподаватель";

    public override string Describe() =>
        $"Преподаватель «{FullName}» — {Title} ({ExperienceYears} лет опыта)";

    public override string GetMetricLabel() => "Специализация";
    public override string GetMetricValue() => Expertise;
}

/// <summary>
/// Учебная группа студентов.
/// </summary>
public sealed class StudyGroup : DomainEntity
{
    [JsonInclude]
    public string Name { get; private set; } = string.Empty;

    [JsonInclude]
    public string CuratorId { get; private set; } = string.Empty;

    [JsonInclude]
    public string Description { get; private set; } = string.Empty;

    public StudyGroup()
    {
    }

    public StudyGroup(string id, string name, string curatorId, string description, string? createdAt = null)
        : base(id, createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationDomainException("Название учебной группы не может быть пустым.");
        }

        Name = name.Trim();
        CuratorId = curatorId;
        Description = description.Trim();
    }

    public override string Describe() => $"Учебная группа «{Name}»";
}
