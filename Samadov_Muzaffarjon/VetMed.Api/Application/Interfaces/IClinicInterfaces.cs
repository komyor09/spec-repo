using VetMed.Api.Application.Contracts;
using VetMed.Api.Domain.Entities;

namespace VetMed.Api.Application.Interfaces;

public interface IClinicSeedProvider
{
    ClinicState CreateInitialState();
}

public interface IClinicRepository
{
    ClinicState LoadState();
    void SaveState(ClinicState state);
    ClinicState ResetState();
}

public interface IScheduleValidator
{
    void EnsureSlotAvailable(ClinicState state, string veterinarianId, string date, string timeSlot);
}

public interface IPharmacyService
{
    void DispensePrescriptions(ClinicState state, IEnumerable<PrescriptionItem> prescriptions);
    Medication AdjustStock(ClinicState state, string medicationId, int delta);
}

public interface IBillingService
{
    Invoice CreateExamInvoice(
        ClinicState state,
        AnimalPatient pet,
        MedicalRecord record,
        decimal consultationFeeRub);
}

public interface IClinicService
{
    ClinicState GetState();
    ClinicState ResetState();
    IReadOnlyList<EntitySummaryDto> GetPolymorphicEntitySummaries();
    (Owner Owner, ClinicState State) CreateOwner(CreateOwnerRequest request);
    (AnimalPatient Pet, ClinicState State) CreatePatient(CreatePatientRequest request);
    (AnimalPatient Pet, ClinicState State) UpdatePatient(string id, UpdatePatientRequest request);
    (Appointment Appointment, ClinicState State) CreateAppointment(CreateAppointmentRequest request);
    (Appointment Appointment, ClinicState State) UpdateAppointmentStatus(string id, string? status);
    (MedicalRecord Record, Invoice? Invoice, ClinicState State) CreateMedicalRecord(CreateMedicalRecordRequest request);
    (Vaccination Vaccination, ClinicState State) CreateVaccination(CreateVaccinationRequest request);
    (Medication Medication, ClinicState State) CreateMedication(CreateMedicationRequest request);
    (Medication Medication, ClinicState State) AdjustMedicationStock(string id, int delta);
    (LabTest LabTest, ClinicState State) CreateLabTest(CreateLabTestRequest request);
    (LabTest LabTest, ClinicState State) UpdateLabTest(string id, UpdateLabTestRequest request);
    (InpatientTask Task, ClinicState State) CreateInpatientTask(CreateInpatientTaskRequest request);
    (InpatientTask Task, ClinicState State) ToggleInpatientTask(string id);
    (Invoice Invoice, ClinicState State) CreateInvoice(CreateInvoiceRequest request);
    (Invoice Invoice, ClinicState State) UpdateInvoiceStatus(string id, string? status);
}
