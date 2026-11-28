using VetMed.Api.Domain.Entities;

namespace VetMed.Api.Application.Contracts;

public sealed record CreateOwnerRequest(
    string? FullName,
    string? Phone,
    string? Email,
    string? Address,
    string? LoyaltyTier);

public sealed record CreatePatientRequest(
    string? OwnerId,
    string? Name,
    string? Species,
    string? Breed,
    string? Color,
    string? Gender,
    bool? Sterilized,
    string? BirthDate,
    double? WeightKg,
    string? BloodType,
    string? MicrochipNumber,
    string? Allergies,
    string? Diet,
    string? InsurancePolicy,
    string? CageNumber,
    string? PhotoUrl,
    string? Status);

public sealed record UpdatePatientRequest(
    double? WeightKg,
    string? Status,
    string? Allergies,
    string? Diet,
    string? CageNumber);

public sealed record CreateAppointmentRequest(
    string? PetId,
    string? VeterinarianId,
    string? Date,
    string? TimeSlot,
    string? ServiceType,
    string? Reason,
    string? Notes);

public sealed record UpdateStatusRequest(string? Status);

public sealed record CreateMedicalRecordRequest(
    string? PetId,
    string? VeterinarianId,
    string? VisitDate,
    string? Complaints,
    string? Diagnosis,
    double? TemperatureC,
    double? WeightKg,
    int? HeartRateBpm,
    string? TreatmentPlan,
    List<PrescriptionItem>? Prescriptions,
    bool? CreateInvoice = false,
    decimal? ConsultationFeeRub = null);

public sealed record CreateVaccinationRequest(
    string? PetId,
    string? Kind,
    string? VaccineName,
    string? AdministeredDate,
    string? NextDueDate,
    string? BatchNumber,
    string? VeterinarianId);

public sealed record CreateMedicationRequest(
    string? Name,
    string? ActiveIngredient,
    string? Manufacturer,
    string? Category,
    string? DosageForm,
    string? Unit,
    int? StockQty,
    int? MinThreshold,
    string? StorageCondition,
    string? BatchNumber,
    bool? PrescriptionRequired,
    string? ExpiryDate,
    decimal? UnitPriceRub);

public sealed record AdjustStockRequest(int? Delta);

public sealed record CreateLabTestRequest(
    string? PetId,
    string? VeterinarianId,
    string? TestType,
    string? SampleType,
    string? ResultSummary);

public sealed record UpdateLabTestRequest(
    string? Status,
    string? ResultSummary,
    bool? IsAbnormal);

public sealed record CreateInpatientTaskRequest(
    string? PetId,
    string? Time,
    string? Procedure,
    string? Notes);

public sealed record CreateInvoiceRequest(
    string? OwnerId,
    string? PetId,
    string? Title,
    decimal? AmountRub);

public sealed record EntitySummaryDto(string Id, string Category, string Summary);
