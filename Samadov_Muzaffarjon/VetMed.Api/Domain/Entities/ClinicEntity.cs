using System.Text.Json.Serialization;
using VetMed.Api.Domain.Exceptions;

namespace VetMed.Api.Domain.Entities;

/// <summary>
/// Абстрактный базовый класс любой сущности клиники.
/// Демонстрирует абстракцию, инкапсуляцию идентификатора и полиморфное представление.
/// </summary>
public abstract class ClinicEntity
{
    [JsonInclude]
    public string Id { get; protected set; } = string.Empty;

    [JsonIgnore]
    public abstract string EntityCategory { get; }

    protected ClinicEntity()
    {
    }

    protected ClinicEntity(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new DomainValidationException("Идентификатор сущности не может быть пустым.");
        }

        Id = id.Trim();
    }

    public abstract string GetDisplaySummary();
}

/// <summary>
/// Абстрактный базовый класс участника процесса (владелец или ветеринарный врач).
/// </summary>
public abstract class PersonBase : ClinicEntity
{
    [JsonInclude]
    public string FullName { get; protected set; } = string.Empty;

    protected PersonBase()
    {
    }

    protected PersonBase(string id, string fullName)
        : base(id)
    {
        SetFullName(fullName);
    }

    protected void SetFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainValidationException("ФИО участника не может быть пустым.");
        }

        FullName = fullName.Trim();
    }

    public abstract string GetRoleDescription();
}

/// <summary>
/// Абстрактное клиническое событие, связывающее пациента и лечащего врача.
/// </summary>
public abstract class ClinicalEvent : ClinicEntity
{
    [JsonInclude]
    public string PetId { get; protected set; } = string.Empty;

    [JsonInclude]
    public string VeterinarianId { get; protected set; } = string.Empty;

    protected ClinicalEvent()
    {
    }

    protected ClinicalEvent(string id, string petId, string veterinarianId)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(petId))
        {
            throw new DomainValidationException("Не указан идентификатор пациента.");
        }

        if (string.IsNullOrWhiteSpace(veterinarianId))
        {
            throw new DomainValidationException("Не указан идентификатор врача.");
        }

        PetId = petId.Trim();
        VeterinarianId = veterinarianId.Trim();
    }

    public abstract string GetEventTitle();
}
