using AutoService.Domain.Enums;
using AutoService.Domain.Interfaces;

namespace AutoService.Services.Reports;

/// <summary>Отчёты для руководителя. Суммы считаются в памяти из объектов модели.</summary>
public class ReportService
{
    private readonly IUnitOfWorkFactory _uowFactory;

    public ReportService(IUnitOfWorkFactory uowFactory) => _uowFactory = uowFactory;

    public RevenueReport Revenue(DateTime from, DateTime to)
    {
        using var uow = _uowFactory.Create();
        var rows = uow.Requests.GetIssuedBetween(from.Date, to.Date.AddDays(1))
            .OrderBy(r => r.IssuedAt)
            .Select(r => new RevenueRow(r.Number, r.IssuedAt!.Value, r.Client.FullName, r.Car.ToString(),
                                        r.Payment?.Method ?? "—", r.Total))
            .ToList();
        return new RevenueReport(from.Date, to.Date, rows);
    }

    public IReadOnlyList<MechanicLoadRow> MechanicLoad(DateTime from, DateTime to)
    {
        using var uow = _uowFactory.Create();
        return uow.Requests.GetIssuedBetween(from.Date, to.Date.AddDays(1))
            .SelectMany(r => r.Works)
            .GroupBy(w => w.Mechanic)
            .Select(g => new MechanicLoadRow(g.Key.FullName, g.Key.Specialization.ToRussian(), g.Count(), g.Sum(w => w.Price)))
            .OrderByDescending(r => r.WorksSum)
            .ToList();
    }

    public IReadOnlyList<CarHistoryRow> CarHistory(int carId)
    {
        using var uow = _uowFactory.Create();
        return uow.Requests.GetByCar(carId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new CarHistoryRow(r.Number, r.CreatedAt, r.Status.ToRussian(), r.Problem,
                                           string.Join(", ", r.Works.Select(w => w.Service.Name)), r.Total))
            .ToList();
    }
}
