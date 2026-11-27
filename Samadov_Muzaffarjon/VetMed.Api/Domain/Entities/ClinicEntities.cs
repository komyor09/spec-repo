using System.Text.Json.Serialization;
using VetMed.Api.Domain.Exceptions;

namespace VetMed.Api.Domain.Entities;

public sealed class Owner : PersonBase
{
    [JsonInclude]
    public string Phone { get; private set; } = string.Empty;

    [JsonInclude]
    public string Email { get; private set; } = string.Empty;

    [JsonInclude]
    public string Address { get; private set; } = string.Empty;

    [JsonInclude]
    public string RegisteredAt { get; private set; } = string.Empty;

    [JsonInclude]
    public string LoyaltyTier { get; private set; } = "Стандарт";

    [JsonIgnore]
    public override string EntityCategory => "Владелец";

    public Owner()
    {
    }

    public Owner(
        string id,
        string fullName,
        string phone,
        string? email,
        string? address,
        string? registeredAt,
        string? loyaltyTier)
        : base(id, fullName)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new DomainValidationException("Укажите ФИО и телефон владельца");
        }

        Phone = phone.Trim();
        Email = (email ?? string.Empty).Trim();
        Address = (address ?? string.Empty).Trim();
        RegisteredAt = string.IsNullOrWhiteSpace(registeredAt)
            ? DateTime.UtcNow.ToString("yyyy-MM-dd")
            : registeredAt.Trim();
        LoyaltyTier = string.IsNullOrWhiteSpace(loyaltyTier) ? "Стандарт" : loyaltyTier.Trim();
    }

    public void UpdateContactInfo(string phone, string? email, string? address)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new DomainValidationException("Телефон владельца не может быть пустым.");
        }

        Phone = phone.Trim();
        Email = (email ?? string.Empty).Trim();
        Address = (address ?? string.Empty).Trim();
    }

    public override string GetRoleDescription() => $"Владелец ({LoyaltyTier})";

    public override string GetDisplaySummary() => $"{FullName} · {Phone} [{LoyaltyTier}]";
}

public sealed class Veterinarian : PersonBase
{
    [JsonInclude]
    public string Specialization { get; private set; } = string.Empty;

    [JsonInclude]
    public string Cabinet { get; private set; } = string.Empty;

    [JsonInclude]
    public int ExperienceYears { get; private set; }

    [JsonInclude]
    public string WorkDays { get; private set; } = string.Empty;

    [JsonInclude]
    public string? AvatarUrl { get; private set; }

    [JsonIgnore]
    public override string EntityCategory => "Ветеринарный врач";

    public Veterinarian()
    {
    }

    public Veterinarian(
        string id,
        string fullName,
        string specialization,
        string cabinet,
        int experienceYears,
        string workDays,
        string? avatarUrl = null)
        : base(id, fullName)
    {
        Specialization = specialization.Trim();
        Cabinet = cabinet.Trim();
        ExperienceYears = Math.Max(0, experienceYears);
        WorkDays = workDays.Trim();
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
    }

    public override string GetRoleDescription() => $"{Specialization} ({Cabinet})";

    public override string GetDisplaySummary() => $"{FullName} — {Specialization}, стаж {ExperienceYears} лет";
}

public sealed class AnimalPatient : ClinicEntity
{
    [JsonInclude]
    public string OwnerId { get; private set; } = string.Empty;

    [JsonInclude]
    public string Name { get; private set; } = string.Empty;

    [JsonInclude]
    public string Species { get; private set; } = string.Empty;

    [JsonInclude]
    public string Breed { get; private set; } = "Метис";

    [JsonInclude]
    public string Color { get; private set; } = "Не указан";

    [JsonInclude]
    public string Gender { get; private set; } = "Самец";

    [JsonInclude]
    public bool Sterilized { get; private set; }

    [JsonInclude]
    public string BirthDate { get; private set; } = "2024-01-01";

    [JsonInclude]
    public double WeightKg { get; private set; }

    [JsonInclude]
    public string BloodType { get; private set; } = "Не типируется";

    [JsonInclude]
    public string MicrochipNumber { get; private set; } = string.Empty;

    [JsonInclude]
    public string Allergies { get; private set; } = "Не выявлены";

    [JsonInclude]
    public string Diet { get; private set; } = "Стандартный рацион";

    [JsonInclude]
    public string? InsurancePolicy { get; private set; }

    [JsonInclude]
    public string? CageNumber { get; private set; }

    [JsonInclude]
    public string? PhotoUrl { get; private set; }

    [JsonInclude]
    public string Status { get; private set; } = "Здоров";

    [JsonIgnore]
    public override string EntityCategory => "Пациент";

    public AnimalPatient()
    {
    }

    public AnimalPatient(
        string id,
        string ownerId,
        string name,
        string species,
        string? breed,
        string? color,
        string? gender,
        bool sterilized,
        string? birthDate,
        double weightKg,
        string? bloodType,
        string? microchipNumber,
        string? allergies,
        string? diet,
        string? insurancePolicy,
        string? cageNumber,
        string? photoUrl,
        string? status)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(ownerId) || string.IsNullOrWhiteSpace(species))
        {
            throw new DomainValidationException("Заполните кличку, вид и владельца питомца");
        }

        OwnerId = ownerId.Trim();
        Name = name.Trim();
        Species = species.Trim();
        Breed = string.IsNullOrWhiteSpace(breed) ? "Метис" : breed.Trim();
        Color = string.IsNullOrWhiteSpace(color) ? "Не указан" : color.Trim();
        Gender = string.IsNullOrWhiteSpace(gender) ? "Самец" : gender.Trim();
        Sterilized = sterilized;
        BirthDate = string.IsNullOrWhiteSpace(birthDate) ? "2024-01-01" : birthDate.Trim();
        WeightKg = weightKg > 0 ? weightKg : 1.0;
        BloodType = string.IsNullOrWhiteSpace(bloodType) ? "Не типируется" : bloodType.Trim();
        MicrochipNumber = string.IsNullOrWhiteSpace(microchipNumber)
            ? $"643098100{Random.Shared.Next(100000, 999999)}"
            : microchipNumber.Trim();
        Allergies = string.IsNullOrWhiteSpace(allergies) ? "Не выявлены" : allergies.Trim();
        Diet = string.IsNullOrWhiteSpace(diet) ? "Стандартный рацион" : diet.Trim();
        InsurancePolicy = string.IsNullOrWhiteSpace(insurancePolicy) ? null : insurancePolicy.Trim();
        CageNumber = string.IsNullOrWhiteSpace(cageNumber) ? null : cageNumber.Trim();
        PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim();
        Status = string.IsNullOrWhiteSpace(status) ? "Здоров" : status.Trim();
    }

    public void UpdateWeight(double weightKg)
    {
        if (weightKg > 0)
        {
            WeightKg = weightKg;
        }
    }

    public void UpdateCareProfile(
        double? weightKg,
        string? status,
        string? allergies,
        string? diet,
        string? cageNumber)
    {
        if (weightKg.HasValue && weightKg.Value > 0)
        {
            WeightKg = weightKg.Value;
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            Status = status.Trim();
        }

        if (allergies is not null)
        {
            Allergies = allergies;
        }

        if (diet is not null)
        {
            Diet = diet;
        }

        if (cageNumber is not null)
        {
            CageNumber = cageNumber;
        }
    }

    public override string GetDisplaySummary() => $"{Name} ({Species}, {Breed}) — {WeightKg:0.##} кг [{Status}]";
}

public sealed class Appointment : ClinicalEvent
{
    [JsonInclude]
    public string Date { get; private set; } = string.Empty;

    [JsonInclude]
    public string TimeSlot { get; private set; } = string.Empty;

    [JsonInclude]
    public string ServiceType { get; private set; } = "Первичный осмотр";

    [JsonInclude]
    public string Reason { get; private set; } = string.Empty;

    [JsonInclude]
    public string Status { get; private set; } = "Запланирован";

    [JsonInclude]
    public string? Notes { get; private set; }

    [JsonIgnore]
    public override string EntityCategory => "Запись на приём";

    public Appointment()
    {
    }

    public Appointment(
        string id,
        string petId,
        string veterinarianId,
        string date,
        string timeSlot,
        string? serviceType,
        string reason,
        string? status = "Запланирован",
        string? notes = null)
        : base(id, petId, veterinarianId)
    {
        if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(timeSlot) || string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainValidationException("Заполните все поля записи на прием");
        }

        Date = date.Trim();
        TimeSlot = timeSlot.Trim();
        ServiceType = string.IsNullOrWhiteSpace(serviceType) ? "Первичный осмотр" : serviceType.Trim();
        Reason = reason.Trim();
        Status = string.IsNullOrWhiteSpace(status) ? "Запланирован" : status.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public void ChangeStatus(string newStatus)
    {
        if (string.IsNullOrWhiteSpace(newStatus))
        {
            throw new DomainValidationException("Не указан новый статус");
        }

        Status = newStatus.Trim();
    }

    public bool IsConflictWith(string veterinarianId, string date, string timeSlot) =>
        Status != "Отменен"
        && string.Equals(VeterinarianId, veterinarianId, StringComparison.Ordinal)
        && string.Equals(Date, date, StringComparison.Ordinal)
        && string.Equals(TimeSlot, timeSlot, StringComparison.Ordinal);

    public override string GetEventTitle() => $"{ServiceType}: {Date} в {TimeSlot}";

    public override string GetDisplaySummary() => $"{GetEventTitle()} [{Status}] — {Reason}";
}

public sealed class PrescriptionItem
{
    public string MedicationId { get; set; } = string.Empty;
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public int DispensedQty { get; set; }
}

public sealed class MedicalRecord : ClinicalEvent
{
    [JsonInclude]
    public string VisitDate { get; private set; } = string.Empty;

    [JsonInclude]
    public string Complaints { get; private set; } = string.Empty;

    [JsonInclude]
    public string Diagnosis { get; private set; } = string.Empty;

    [JsonInclude]
    public double TemperatureC { get; private set; }

    [JsonInclude]
    public double WeightKg { get; private set; }

    [JsonInclude]
    public int HeartRateBpm { get; private set; }

    [JsonInclude]
    public string TreatmentPlan { get; private set; } = string.Empty;

    [JsonInclude]
    public List<PrescriptionItem> Prescriptions { get; private set; } = [];

    [JsonIgnore]
    public override string EntityCategory => "Протокол осмотра";

    public MedicalRecord()
    {
    }

    public MedicalRecord(
        string id,
        string petId,
        string veterinarianId,
        string? visitDate,
        string? complaints,
        string diagnosis,
        double temperatureC,
        double weightKg,
        int heartRateBpm,
        string? treatmentPlan,
        List<PrescriptionItem>? prescriptions)
        : base(id, petId, veterinarianId)
    {
        if (string.IsNullOrWhiteSpace(diagnosis))
        {
            throw new DomainValidationException("Укажите пациента, врача и диагноз");
        }

        VisitDate = string.IsNullOrWhiteSpace(visitDate)
            ? DateTime.UtcNow.ToString("yyyy-MM-dd")
            : visitDate.Trim();
        Complaints = string.IsNullOrWhiteSpace(complaints) ? "Плановый осмотр" : complaints.Trim();
        Diagnosis = diagnosis.Trim();
        TemperatureC = temperatureC > 0 ? temperatureC : 38.5;
        WeightKg = weightKg > 0 ? weightKg : 1.0;
        HeartRateBpm = heartRateBpm > 0 ? heartRateBpm : 95;
        TreatmentPlan = string.IsNullOrWhiteSpace(treatmentPlan)
            ? "Наблюдение в динамике"
            : treatmentPlan.Trim();
        Prescriptions = prescriptions ?? [];
    }

    public override string GetEventTitle() => $"Осмотр {VisitDate}: {Diagnosis}";

    public override string GetDisplaySummary() =>
        $"{GetEventTitle()} (T={TemperatureC:0.0}°C, ЧСС={HeartRateBpm}, вес={WeightKg:0.##} кг)";
}

public sealed class Vaccination : ClinicalEvent
{
    [JsonInclude]
    public string Kind { get; private set; } = "Вакцинация";

    [JsonInclude]
    public string VaccineName { get; private set; } = string.Empty;

    [JsonInclude]
    public string AdministeredDate { get; private set; } = string.Empty;

    [JsonInclude]
    public string NextDueDate { get; private set; } = string.Empty;

    [JsonInclude]
    public string BatchNumber { get; private set; } = string.Empty;

    [JsonIgnore]
    public override string EntityCategory => "Вакцинация и профилактика";

    public Vaccination()
    {
    }

    public Vaccination(
        string id,
        string petId,
        string veterinarianId,
        string? kind,
        string vaccineName,
        string? administeredDate,
        string? nextDueDate,
        string? batchNumber)
        : base(id, petId, veterinarianId)
    {
        if (string.IsNullOrWhiteSpace(vaccineName))
        {
            throw new DomainValidationException("Укажите препарат, пациента и врача");
        }

        Kind = string.IsNullOrWhiteSpace(kind) ? "Вакцинация" : kind.Trim();
        VaccineName = vaccineName.Trim();
        AdministeredDate = string.IsNullOrWhiteSpace(administeredDate)
            ? DateTime.UtcNow.ToString("yyyy-MM-dd")
            : administeredDate.Trim();
        NextDueDate = string.IsNullOrWhiteSpace(nextDueDate)
            ? DateTime.UtcNow.AddDays(365).ToString("yyyy-MM-dd")
            : nextDueDate.Trim();
        BatchNumber = string.IsNullOrWhiteSpace(batchNumber)
            ? $"VAC-{Random.Shared.Next(1000, 9999)}"
            : batchNumber.Trim();
    }

    public override string GetEventTitle() => $"{Kind}: {VaccineName}";

    public override string GetDisplaySummary() =>
        $"{GetEventTitle()} (выполнено {AdministeredDate}, след. {NextDueDate})";
}

public sealed class LabTest : ClinicalEvent
{
    [JsonInclude]
    public string OrderedDate { get; private set; } = string.Empty;

    [JsonInclude]
    public string TestType { get; private set; } = string.Empty;

    [JsonInclude]
    public string SampleType { get; private set; } = "Биоматериал";

    [JsonInclude]
    public string Status { get; private set; } = "Назначен";

    [JsonInclude]
    public string ResultSummary { get; private set; } = string.Empty;

    [JsonInclude]
    public bool IsAbnormal { get; private set; }

    [JsonIgnore]
    public override string EntityCategory => "Лабораторное исследование";

    public LabTest()
    {
    }

    public LabTest(
        string id,
        string petId,
        string veterinarianId,
        string? orderedDate,
        string testType,
        string? sampleType,
        string? status = "Назначен",
        string? resultSummary = null,
        bool isAbnormal = false)
        : base(id, petId, veterinarianId)
    {
        if (string.IsNullOrWhiteSpace(testType))
        {
            throw new DomainValidationException("Укажите пациента, врача и вид исследования");
        }

        OrderedDate = string.IsNullOrWhiteSpace(orderedDate)
            ? DateTime.UtcNow.ToString("yyyy-MM-dd")
            : orderedDate.Trim();
        TestType = testType.Trim();
        SampleType = string.IsNullOrWhiteSpace(sampleType) ? "Биоматериал" : sampleType.Trim();
        Status = string.IsNullOrWhiteSpace(status) ? "Назначен" : status.Trim();
        ResultSummary = string.IsNullOrWhiteSpace(resultSummary)
            ? "Ожидает забора или обработки"
            : resultSummary.Trim();
        IsAbnormal = isAbnormal;
    }

    public void UpdateProgress(string? status, string? resultSummary, bool? isAbnormal)
    {
        if (!string.IsNullOrWhiteSpace(status))
        {
            Status = status.Trim();
        }

        if (resultSummary is not null)
        {
            ResultSummary = resultSummary;
        }

        if (isAbnormal.HasValue)
        {
            IsAbnormal = isAbnormal.Value;
        }
    }

    public override string GetEventTitle() => TestType;

    public override string GetDisplaySummary() =>
        $"{TestType} [{Status}]{(IsAbnormal ? " — отклонение от нормы" : string.Empty)}";
}

public sealed class Medication : ClinicEntity
{
    [JsonInclude]
    public string Name { get; private set; } = string.Empty;

    [JsonInclude]
    public string ActiveIngredient { get; private set; } = string.Empty;

    [JsonInclude]
    public string Manufacturer { get; private set; } = "ВетФарм";

    [JsonInclude]
    public string Category { get; private set; } = string.Empty;

    [JsonInclude]
    public string DosageForm { get; private set; } = "Упаковка";

    [JsonInclude]
    public string Unit { get; private set; } = "уп.";

    [JsonInclude]
    public int StockQty { get; private set; }

    [JsonInclude]
    public int MinThreshold { get; private set; }

    [JsonInclude]
    public string StorageCondition { get; private set; } = "До +25 °C";

    [JsonInclude]
    public string BatchNumber { get; private set; } = string.Empty;

    [JsonInclude]
    public bool PrescriptionRequired { get; private set; }

    [JsonInclude]
    public string ExpiryDate { get; private set; } = "2028-01-01";

    [JsonInclude]
    public decimal UnitPriceRub { get; private set; }

    [JsonIgnore]
    public override string EntityCategory => "Препарат аптеки";

    public Medication()
    {
    }

    public Medication(
        string id,
        string name,
        string activeIngredient,
        string? manufacturer,
        string category,
        string? dosageForm,
        string? unit,
        int stockQty,
        int minThreshold,
        string? storageCondition,
        string? batchNumber,
        bool prescriptionRequired,
        string? expiryDate,
        decimal unitPriceRub)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(activeIngredient) || string.IsNullOrWhiteSpace(category))
        {
            throw new DomainValidationException("Укажите название, действующее вещество и категорию");
        }

        Name = name.Trim();
        ActiveIngredient = activeIngredient.Trim();
        Manufacturer = string.IsNullOrWhiteSpace(manufacturer) ? "ВетФарм" : manufacturer.Trim();
        Category = category.Trim();
        DosageForm = string.IsNullOrWhiteSpace(dosageForm) ? "Упаковка" : dosageForm.Trim();
        Unit = string.IsNullOrWhiteSpace(unit) ? "уп." : unit.Trim();
        StockQty = Math.Max(0, stockQty);
        MinThreshold = Math.Max(1, minThreshold);
        StorageCondition = string.IsNullOrWhiteSpace(storageCondition) ? "До +25 °C" : storageCondition.Trim();
        BatchNumber = string.IsNullOrWhiteSpace(batchNumber)
            ? $"LOT-{Random.Shared.Next(1000, 9999)}"
            : batchNumber.Trim();
        PrescriptionRequired = prescriptionRequired;
        ExpiryDate = string.IsNullOrWhiteSpace(expiryDate) ? "2028-01-01" : expiryDate.Trim();
        UnitPriceRub = Math.Max(0, unitPriceRub);
    }

    public void Dispense(int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        StockQty = Math.Max(0, StockQty - quantity);
    }

    public void AdjustStock(int delta)
    {
        StockQty = Math.Max(0, StockQty + delta);
    }

    public bool IsLowStock() => StockQty <= MinThreshold;

    public override string GetDisplaySummary() =>
        $"{Name} ({Category}) — остаток {StockQty} {Unit}{(IsLowStock() ? " [НИЗКИЙ ОСТАТОК]" : string.Empty)}";
}

public sealed class InpatientTask : ClinicEntity
{
    [JsonInclude]
    public string PetId { get; private set; } = string.Empty;

    [JsonInclude]
    public string Time { get; private set; } = "12:00";

    [JsonInclude]
    public string Procedure { get; private set; } = string.Empty;

    [JsonInclude]
    public string Notes { get; private set; } = string.Empty;

    [JsonInclude]
    public bool Completed { get; private set; }

    [JsonIgnore]
    public override string EntityCategory => "Процедура стационара";

    public InpatientTask()
    {
    }

    public InpatientTask(
        string id,
        string petId,
        string? time,
        string procedure,
        string? notes,
        bool completed = false)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(petId) || string.IsNullOrWhiteSpace(procedure))
        {
            throw new DomainValidationException("Укажите пациента и процедуру");
        }

        PetId = petId.Trim();
        Time = string.IsNullOrWhiteSpace(time) ? "12:00" : time.Trim();
        Procedure = procedure.Trim();
        Notes = (notes ?? string.Empty).Trim();
        Completed = completed;
    }

    public void ToggleCompletion()
    {
        Completed = !Completed;
    }

    public override string GetDisplaySummary() =>
        $"{Time} · {Procedure} ({(Completed ? "Выполнено" : "Ожидает")})";
}

public sealed class Invoice : ClinicEntity
{
    [JsonInclude]
    public string OwnerId { get; private set; } = string.Empty;

    [JsonInclude]
    public string PetId { get; private set; } = string.Empty;

    [JsonInclude]
    public string Date { get; private set; } = string.Empty;

    [JsonInclude]
    public string Title { get; private set; } = string.Empty;

    [JsonInclude]
    public decimal AmountRub { get; private set; }

    [JsonInclude]
    public string Status { get; private set; } = "Ожидает оплаты";

    [JsonIgnore]
    public override string EntityCategory => "Счёт на оплату";

    public Invoice()
    {
    }

    public Invoice(
        string id,
        string ownerId,
        string petId,
        string? date,
        string title,
        decimal amountRub,
        string? status = "Ожидает оплаты")
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(ownerId) || string.IsNullOrWhiteSpace(petId) || string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("Укажите владельца, питомца и описание счета");
        }

        OwnerId = ownerId.Trim();
        PetId = petId.Trim();
        Date = string.IsNullOrWhiteSpace(date)
            ? DateTime.UtcNow.ToString("yyyy-MM-dd")
            : date.Trim();
        Title = title.Trim();
        AmountRub = Math.Max(0, amountRub);
        Status = string.IsNullOrWhiteSpace(status) ? "Ожидает оплаты" : status.Trim();
    }

    public void UpdateStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new DomainValidationException("Не указан статус счета");
        }

        Status = status.Trim();
    }

    public override string GetDisplaySummary() => $"{Title} — {AmountRub:0} ₽ [{Status}]";
}

public sealed class ClinicState
{
    public List<Owner> Owners { get; set; } = [];
    public List<Veterinarian> Veterinarians { get; set; } = [];
    public List<AnimalPatient> Pets { get; set; } = [];
    public List<Appointment> Appointments { get; set; } = [];
    public List<MedicalRecord> MedicalRecords { get; set; } = [];
    public List<Vaccination> Vaccinations { get; set; } = [];
    public List<Medication> Medications { get; set; } = [];
    public List<LabTest> LabTests { get; set; } = [];
    public List<InpatientTask> InpatientTasks { get; set; } = [];
    public List<Invoice> Invoices { get; set; } = [];

    public IEnumerable<ClinicEntity> EnumerateAllEntities()
    {
        foreach (var item in Owners) yield return item;
        foreach (var item in Veterinarians) yield return item;
        foreach (var item in Pets) yield return item;
        foreach (var item in Appointments) yield return item;
        foreach (var item in MedicalRecords) yield return item;
        foreach (var item in Vaccinations) yield return item;
        foreach (var item in Medications) yield return item;
        foreach (var item in LabTests) yield return item;
        foreach (var item in InpatientTasks) yield return item;
        foreach (var item in Invoices) yield return item;
    }
}
