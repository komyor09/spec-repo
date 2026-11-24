using AutoService.Services;
using AutoService.Services.Reports;

namespace AutoService.WinForms;

/// <summary>Набор сервисов, который получают формы (вместо четырёх параметров конструктора).</summary>
public sealed record AppServices(
    ClientService Clients,
    CatalogService Catalog,
    RepairRequestService Requests,
    ReportService Reports);
