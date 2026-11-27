using Microsoft.AspNetCore.Mvc;
using VetMed.Api.Application.Contracts;
using VetMed.Api.Application.Interfaces;

namespace VetMed.Api.Controllers;

[ApiController]
[Route("api/clinic")]
public sealed class ClinicController : ControllerBase
{
    private readonly IClinicService _clinicService;

    public ClinicController(IClinicService clinicService) => _clinicService = clinicService;

    [HttpGet("state")]
    public IActionResult GetState() => Ok(_clinicService.GetState());

    [HttpPost("reset")]
    public IActionResult ResetState() => Ok(_clinicService.ResetState());

    [HttpGet("oop-summary")]
    public IActionResult GetOopSummary() => Ok(_clinicService.GetPolymorphicEntitySummaries());
}

[ApiController]
[Route("api/owners")]
public sealed class OwnersController : ControllerBase
{
    private readonly IClinicService _clinicService;

    public OwnersController(IClinicService clinicService) => _clinicService = clinicService;

    [HttpPost]
    public IActionResult Create([FromBody] CreateOwnerRequest request)
    {
        var (owner, state) = _clinicService.CreateOwner(request);
        return StatusCode(StatusCodes.Status201Created, new { owner, state });
    }
}

[ApiController]
[Route("api/patients")]
public sealed class PatientsController : ControllerBase
{
    private readonly IClinicService _clinicService;

    public PatientsController(IClinicService clinicService) => _clinicService = clinicService;

    [HttpPost]
    public IActionResult Create([FromBody] CreatePatientRequest request)
    {
        var (pet, state) = _clinicService.CreatePatient(request);
        return StatusCode(StatusCodes.Status201Created, new { pet, state });
    }

    [HttpPatch("{id}")]
    public IActionResult Update(string id, [FromBody] UpdatePatientRequest request)
    {
        var (pet, state) = _clinicService.UpdatePatient(id, request);
        return Ok(new { pet, state });
    }
}

[ApiController]
[Route("api/appointments")]
public sealed class AppointmentsController : ControllerBase
{
    private readonly IClinicService _clinicService;

    public AppointmentsController(IClinicService clinicService) => _clinicService = clinicService;

    [HttpPost]
    public IActionResult Create([FromBody] CreateAppointmentRequest request)
    {
        var (appointment, state) = _clinicService.CreateAppointment(request);
        return StatusCode(StatusCodes.Status201Created, new { appointment, state });
    }

    [HttpPatch("{id}/status")]
    public IActionResult UpdateStatus(string id, [FromBody] UpdateStatusRequest request)
    {
        var (appointment, state) = _clinicService.UpdateAppointmentStatus(id, request.Status);
        return Ok(new { appointment, state });
    }
}

[ApiController]
[Route("api/medical-records")]
public sealed class MedicalRecordsController : ControllerBase
{
    private readonly IClinicService _clinicService;

    public MedicalRecordsController(IClinicService clinicService) => _clinicService = clinicService;

    [HttpPost]
    public IActionResult Create([FromBody] CreateMedicalRecordRequest request)
    {
        var (record, invoice, state) = _clinicService.CreateMedicalRecord(request);
        return invoice is not null
            ? StatusCode(StatusCodes.Status201Created, new { record, invoice, state })
            : StatusCode(StatusCodes.Status201Created, new { record, state });
    }
}

[ApiController]
[Route("api/vaccinations")]
public sealed class VaccinationsController : ControllerBase
{
    private readonly IClinicService _clinicService;

    public VaccinationsController(IClinicService clinicService) => _clinicService = clinicService;

    [HttpPost]
    public IActionResult Create([FromBody] CreateVaccinationRequest request)
    {
        var (vaccination, state) = _clinicService.CreateVaccination(request);
        return StatusCode(StatusCodes.Status201Created, new { vaccination, state });
    }
}

[ApiController]
[Route("api/medications")]
public sealed class MedicationsController : ControllerBase
{
    private readonly IClinicService _clinicService;

    public MedicationsController(IClinicService clinicService) => _clinicService = clinicService;

    [HttpPost]
    public IActionResult Create([FromBody] CreateMedicationRequest request)
    {
        var (medication, state) = _clinicService.CreateMedication(request);
        return StatusCode(StatusCodes.Status201Created, new { medication, state });
    }

    [HttpPatch("{id}/stock")]
    public IActionResult AdjustStock(string id, [FromBody] AdjustStockRequest request)
    {
        var (medication, state) = _clinicService.AdjustMedicationStock(id, request.Delta ?? 0);
        return Ok(new { medication, state });
    }
}

[ApiController]
[Route("api/lab-tests")]
public sealed class LabTestsController : ControllerBase
{
    private readonly IClinicService _clinicService;

    public LabTestsController(IClinicService clinicService) => _clinicService = clinicService;

    [HttpPost]
    public IActionResult Create([FromBody] CreateLabTestRequest request)
    {
        var (labTest, state) = _clinicService.CreateLabTest(request);
        return StatusCode(StatusCodes.Status201Created, new { labTest, state });
    }

    [HttpPatch("{id}")]
    public IActionResult Update(string id, [FromBody] UpdateLabTestRequest request)
    {
        var (labTest, state) = _clinicService.UpdateLabTest(id, request);
        return Ok(new { labTest, state });
    }
}

[ApiController]
[Route("api/inpatient-tasks")]
public sealed class InpatientTasksController : ControllerBase
{
    private readonly IClinicService _clinicService;

    public InpatientTasksController(IClinicService clinicService) => _clinicService = clinicService;

    [HttpPost]
    public IActionResult Create([FromBody] CreateInpatientTaskRequest request)
    {
        var (task, state) = _clinicService.CreateInpatientTask(request);
        return StatusCode(StatusCodes.Status201Created, new { task, state });
    }

    [HttpPatch("{id}/toggle")]
    public IActionResult Toggle(string id)
    {
        var (task, state) = _clinicService.ToggleInpatientTask(id);
        return Ok(new { task, state });
    }
}

[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController : ControllerBase
{
    private readonly IClinicService _clinicService;

    public InvoicesController(IClinicService clinicService) => _clinicService = clinicService;

    [HttpPost]
    public IActionResult Create([FromBody] CreateInvoiceRequest request)
    {
        var (invoice, state) = _clinicService.CreateInvoice(request);
        return StatusCode(StatusCodes.Status201Created, new { invoice, state });
    }

    [HttpPatch("{id}/status")]
    public IActionResult UpdateStatus(string id, [FromBody] UpdateStatusRequest request)
    {
        var (invoice, state) = _clinicService.UpdateInvoiceStatus(id, request.Status);
        return Ok(new { invoice, state });
    }
}
