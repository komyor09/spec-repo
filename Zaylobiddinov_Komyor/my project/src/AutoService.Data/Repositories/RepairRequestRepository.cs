using AutoService.Domain.Entities;
using AutoService.Domain.Enums;
using AutoService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoService.Data.Repositories;

public class RepairRequestRepository : Repository<RepairRequest>, IRepairRequestRepository
{
    public RepairRequestRepository(AppDbContext db) : base(db) { }

    /// <summary>Заявка всегда загружается целиком: клиент, авто, работы, запчасти, оплата.</summary>
    protected override IQueryable<RepairRequest> Query =>
        Db.RepairRequests
            .Include(r => r.Client)
            .Include(r => r.Car)
            .Include(r => r.Receptionist)
            .Include(r => r.Payment)
            .Include(r => r.Works).ThenInclude(w => w.Service)
            .Include(r => r.Works).ThenInclude(w => w.Mechanic)
            .Include(r => r.Parts).ThenInclude(p => p.SparePart)
            .AsSplitQuery();

    public IReadOnlyList<RepairRequest> Search(RequestFilter filter)
    {
        var query = Query;

        if (filter.Status is RequestStatus status)
            query = query.Where(r => r.Status == status);

        if (filter.HideClosed)
            query = query.Where(r => r.Status != RequestStatus.Issued && r.Status != RequestStatus.Cancelled);

        var result = query.OrderByDescending(r => r.CreatedAt).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.Text))
        {
            var text = filter.Text.Trim();
            var plate = text.ToUpperInvariant().Replace(" ", "");
            result = result.Where(r =>
                r.Number.Contains(text)
                || r.Client.FullName.Contains(text, StringComparison.OrdinalIgnoreCase)
                || r.Client.Phone.Contains(text)
                || r.Car.PlateNumber.Contains(plate));
        }

        return result.ToList();
    }

    public int CountCreatedOn(DateTime day)
    {
        var from = day.Date;
        var to = from.AddDays(1);
        return Db.RepairRequests.Count(r => r.CreatedAt >= from && r.CreatedAt < to);
    }

    public int CountIssuedForClient(int clientId) =>
        Db.RepairRequests.Count(r => r.ClientId == clientId && r.Status == RequestStatus.Issued);

    public IReadOnlyList<RepairRequest> GetIssuedBetween(DateTime from, DateTime to) =>
        Query.Where(r => r.IssuedAt >= from && r.IssuedAt < to).ToList();

    public IReadOnlyList<RepairRequest> GetByCar(int carId) =>
        Query.Where(r => r.CarId == carId).ToList();
}
