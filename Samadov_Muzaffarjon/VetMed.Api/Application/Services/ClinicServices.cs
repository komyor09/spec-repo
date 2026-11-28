using VetMed.Api.Application.Contracts;
using VetMed.Api.Application.Interfaces;
using VetMed.Api.Domain.Entities;
using VetMed.Api.Domain.Exceptions;

namespace VetMed.Api.Application.Services;

public sealed class ClinicScheduleValidator : IScheduleValidator
{
    public void EnsureSlotAvailable(ClinicState state, string veterinarianId, string date, string timeSlot)
    {
        var hasConflict = state.Appointments.Any(a => a.IsConflictWith(veterinarianId, date, timeSlot));
        if (hasConflict)
        {
            throw new ScheduleConflictException("Выбранное время у данного врача уже занято");
        }
    }
}

public sealed class PharmacyService : IPharmacyService
{
    public void DispensePrescriptions(ClinicState state, IEnumerable<PrescriptionItem> prescriptions)
    {
        foreach (var rx in prescriptions)
        {
            if (rx.DispensedQty <= 0)
            {
                continue;
            }

            var medication = state.Medications.FirstOrDefault(m => m.Id == rx.MedicationId);
            medication?.Dispense(rx.DispensedQty);
        }
    }

    public Medication AdjustStock(ClinicState state, string medicationId, int delta)
    {
        var medication = state.Medications.FirstOrDefault(m => m.Id == medicationId)
            ?? throw new EntityNotFoundException("Препарат не найден");

        medication.AdjustStock(delta);
        return medication;
    }
}

public sealed class ClinicBillingService : IBillingService
{
    public Invoice CreateExamInvoice(
        ClinicState state,
        AnimalPatient pet,
        MedicalRecord record,
        decimal consultationFeeRub)
    {
        var owner = state.Owners.FirstOrDefault(o => o.Id == pet.OwnerId);
        var discountRate = owner?.LoyaltyTier switch
        {
            "VIP-полис" => 0.10m,
            "Забота+" => 0.05m,
            _ => 0m
        };

        decimal medsTotal = 0m;
        foreach (var rx in record.Prescriptions)
        {
            if (rx.DispensedQty <= 0)
            {
                continue;
            }

            var med = state.Medications.FirstOrDefault(m => m.Id == rx.MedicationId);
            if (med is not null)
            {
                medsTotal += med.UnitPriceRub * rx.DispensedQty;
            }
        }

        var fee = Math.Max(0m, consultationFeeRub);
        var finalAmount = Math.Round((fee + medsTotal) * (1m - discountRate), 0, MidpointRounding.AwayFromZero);

        var rxSuffix = record.Prescriptions.Count > 0
            ? $" + препараты ({record.Prescriptions.Count} поз.)"
            : string.Empty;
        var discountSuffix = discountRate > 0m
            ? $" · скидка {(int)(discountRate * 100)}% ({owner?.LoyaltyTier})"
            : string.Empty;

        var invoice = new Invoice(
            id: $"inv-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            ownerId: pet.OwnerId,
            petId: pet.Id,
            date: record.VisitDate,
            title: $"Осмотр: {record.Diagnosis}{rxSuffix}{discountSuffix}",
            amountRub: finalAmount,
            status: "Ожидает оплаты");

        state.Invoices.Insert(0, invoice);
        return invoice;
    }
}

public sealed class ClinicService : IClinicService
{
    private readonly IClinicRepository _repository;
    private readonly IScheduleValidator _scheduleValidator;
    private readonly IPharmacyService _pharmacyService;
    private readonly IBillingService _billingService;

    public ClinicService(
        IClinicRepository repository,
        IScheduleValidator scheduleValidator,
        IPharmacyService pharmacyService,
        IBillingService billingService)
    {
        _repository = repository;
        _scheduleValidator = scheduleValidator;
        _pharmacyService = pharmacyService;
        _billingService = billingService;
    }

    public ClinicState GetState() => _repository.LoadState();

    public ClinicState ResetState() => _repository.ResetState();

    public IReadOnlyList<EntitySummaryDto> GetPolymorphicEntitySummaries()
    {
        var state = _repository.LoadState();
        return state.EnumerateAllEntities()
            .Select(entity => new EntitySummaryDto(
                entity.Id,
                entity.EntityCategory,
                entity.GetDisplaySummary()))
            .ToList();
    }

    public (Owner Owner, ClinicState State) CreateOwner(CreateOwnerRequest request)
    {
        var state = _repository.LoadState();
        var owner = new Owner(
            id: $"own-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            fullName: request.FullName ?? string.Empty,
            phone: request.Phone ?? string.Empty,
            email: request.Email,
            address: request.Address,
            registeredAt: DateTime.UtcNow.ToString("yyyy-MM-dd"),
            loyaltyTier: request.LoyaltyTier);

        state.Owners.Insert(0, owner);
        _repository.SaveState(state);
        return (owner, state);
    }

    public (AnimalPatient Pet, ClinicState State) CreatePatient(CreatePatientRequest request)
    {
        var state = _repository.LoadState();
        var pet = new AnimalPatient(
            id: $"pet-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            ownerId: request.OwnerId ?? string.Empty,
            name: request.Name ?? string.Empty,
            species: request.Species ?? string.Empty,
            breed: request.Breed,
            color: request.Color,
            gender: request.Gender,
            sterilized: request.Sterilized ?? false,
            birthDate: request.BirthDate,
            weightKg: request.WeightKg ?? 1.0,
            bloodType: request.BloodType,
            microchipNumber: request.MicrochipNumber,
            allergies: request.Allergies,
            diet: request.Diet,
            insurancePolicy: request.InsurancePolicy,
            cageNumber: request.CageNumber,
            photoUrl: request.PhotoUrl,
            status: request.Status);

        state.Pets.Insert(0, pet);
        _repository.SaveState(state);
        return (pet, state);
    }

    public (AnimalPatient Pet, ClinicState State) UpdatePatient(string id, UpdatePatientRequest request)
    {
        var state = _repository.LoadState();
        var pet = state.Pets.FirstOrDefault(p => p.Id == id)
            ?? throw new EntityNotFoundException("Пациент не найден");

        pet.UpdateCareProfile(
            request.WeightKg,
            request.Status,
            request.Allergies,
            request.Diet,
            request.CageNumber);

        _repository.SaveState(state);
        return (pet, state);
    }

    public (Appointment Appointment, ClinicState State) CreateAppointment(CreateAppointmentRequest request)
    {
        var state = _repository.LoadState();
        var appointment = new Appointment(
            id: $"apt-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            petId: request.PetId ?? string.Empty,
            veterinarianId: request.VeterinarianId ?? string.Empty,
            date: request.Date ?? string.Empty,
            timeSlot: request.TimeSlot ?? string.Empty,
            serviceType: request.ServiceType,
            reason: request.Reason ?? string.Empty,
            status: "Запланирован",
            notes: request.Notes);

        _scheduleValidator.EnsureSlotAvailable(
            state,
            appointment.VeterinarianId,
            appointment.Date,
            appointment.TimeSlot);

        state.Appointments.Insert(0, appointment);
        _repository.SaveState(state);
        return (appointment, state);
    }

    public (Appointment Appointment, ClinicState State) UpdateAppointmentStatus(string id, string? status)
    {
        var state = _repository.LoadState();
        var appointment = state.Appointments.FirstOrDefault(a => a.Id == id)
            ?? throw new EntityNotFoundException("Запись не найдена");

        appointment.ChangeStatus(status ?? string.Empty);
        _repository.SaveState(state);
        return (appointment, state);
    }

    public (MedicalRecord Record, Invoice? Invoice, ClinicState State) CreateMedicalRecord(CreateMedicalRecordRequest request)
    {
        var state = _repository.LoadState();
        var pet = state.Pets.FirstOrDefault(p => p.Id == request.PetId)
            ?? throw new EntityNotFoundException("Пациент не найден");

        var prescriptions = request.Prescriptions ?? [];
        var weight = request.WeightKg is > 0 ? request.WeightKg.Value : pet.WeightKg;

        var record = new MedicalRecord(
            id: $"rec-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            petId: pet.Id,
            veterinarianId: request.VeterinarianId ?? string.Empty,
            visitDate: request.VisitDate,
            complaints: request.Complaints,
            diagnosis: request.Diagnosis ?? string.Empty,
            temperatureC: request.TemperatureC ?? 38.5,
            weightKg: weight,
            heartRateBpm: request.HeartRateBpm ?? 95,
            treatmentPlan: request.TreatmentPlan,
            prescriptions: prescriptions);

        _pharmacyService.DispensePrescriptions(state, prescriptions);
        pet.UpdateWeight(weight);

        state.MedicalRecords.Insert(0, record);

        Invoice? invoice = null;
        if (request.CreateInvoice == true)
        {
            invoice = _billingService.CreateExamInvoice(
                state,
                pet,
                record,
                request.ConsultationFeeRub ?? 0m);
        }

        _repository.SaveState(state);
        return (record, invoice, state);
    }

    public (Vaccination Vaccination, ClinicState State) CreateVaccination(CreateVaccinationRequest request)
    {
        var state = _repository.LoadState();
        var vaccination = new Vaccination(
            id: $"vac-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            petId: request.PetId ?? string.Empty,
            veterinarianId: request.VeterinarianId ?? string.Empty,
            kind: request.Kind,
            vaccineName: request.VaccineName ?? string.Empty,
            administeredDate: request.AdministeredDate,
            nextDueDate: request.NextDueDate,
            batchNumber: request.BatchNumber);

        state.Vaccinations.Insert(0, vaccination);
        _repository.SaveState(state);
        return (vaccination, state);
    }

    public (Medication Medication, ClinicState State) CreateMedication(CreateMedicationRequest request)
    {
        var state = _repository.LoadState();
        var medication = new Medication(
            id: $"med-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            name: request.Name ?? string.Empty,
            activeIngredient: request.ActiveIngredient ?? string.Empty,
            manufacturer: request.Manufacturer,
            category: request.Category ?? string.Empty,
            dosageForm: request.DosageForm,
            unit: request.Unit,
            stockQty: request.StockQty ?? 0,
            minThreshold: request.MinThreshold ?? 5,
            storageCondition: request.StorageCondition,
            batchNumber: request.BatchNumber,
            prescriptionRequired: request.PrescriptionRequired ?? false,
            expiryDate: request.ExpiryDate,
            unitPriceRub: request.UnitPriceRub ?? 0m);

        state.Medications.Insert(0, medication);
        _repository.SaveState(state);
        return (medication, state);
    }

    public (Medication Medication, ClinicState State) AdjustMedicationStock(string id, int delta)
    {
        var state = _repository.LoadState();
        var medication = _pharmacyService.AdjustStock(state, id, delta);
        _repository.SaveState(state);
        return (medication, state);
    }

    public (LabTest LabTest, ClinicState State) CreateLabTest(CreateLabTestRequest request)
    {
        var state = _repository.LoadState();
        var labTest = new LabTest(
            id: $"lab-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            petId: request.PetId ?? string.Empty,
            veterinarianId: request.VeterinarianId ?? string.Empty,
            orderedDate: DateTime.UtcNow.ToString("yyyy-MM-dd"),
            testType: request.TestType ?? string.Empty,
            sampleType: request.SampleType,
            status: "Назначен",
            resultSummary: request.ResultSummary,
            isAbnormal: false);

        state.LabTests.Insert(0, labTest);
        _repository.SaveState(state);
        return (labTest, state);
    }

    public (LabTest LabTest, ClinicState State) UpdateLabTest(string id, UpdateLabTestRequest request)
    {
        var state = _repository.LoadState();
        var labTest = state.LabTests.FirstOrDefault(l => l.Id == id)
            ?? throw new EntityNotFoundException("Исследование не найдено");

        labTest.UpdateProgress(request.Status, request.ResultSummary, request.IsAbnormal);
        _repository.SaveState(state);
        return (labTest, state);
    }

    public (InpatientTask Task, ClinicState State) CreateInpatientTask(CreateInpatientTaskRequest request)
    {
        var state = _repository.LoadState();
        var task = new InpatientTask(
            id: $"inp-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            petId: request.PetId ?? string.Empty,
            time: request.Time,
            procedure: request.Procedure ?? string.Empty,
            notes: request.Notes,
            completed: false);

        state.InpatientTasks.Insert(0, task);
        _repository.SaveState(state);
        return (task, state);
    }

    public (InpatientTask Task, ClinicState State) ToggleInpatientTask(string id)
    {
        var state = _repository.LoadState();
        var task = state.InpatientTasks.FirstOrDefault(t => t.Id == id)
            ?? throw new EntityNotFoundException("Задача не найдена");

        task.ToggleCompletion();
        _repository.SaveState(state);
        return (task, state);
    }

    public (Invoice Invoice, ClinicState State) CreateInvoice(CreateInvoiceRequest request)
    {
        var state = _repository.LoadState();
        var invoice = new Invoice(
            id: $"inv-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            ownerId: request.OwnerId ?? string.Empty,
            petId: request.PetId ?? string.Empty,
            date: DateTime.UtcNow.ToString("yyyy-MM-dd"),
            title: request.Title ?? string.Empty,
            amountRub: request.AmountRub ?? 0m,
            status: "Ожидает оплаты");

        state.Invoices.Insert(0, invoice);
        _repository.SaveState(state);
        return (invoice, state);
    }

    public (Invoice Invoice, ClinicState State) UpdateInvoiceStatus(string id, string? status)
    {
        var state = _repository.LoadState();
        var invoice = state.Invoices.FirstOrDefault(i => i.Id == id)
            ?? throw new EntityNotFoundException("Счет не найден");

        invoice.UpdateStatus(status ?? string.Empty);
        _repository.SaveState(state);
        return (invoice, state);
    }
}
