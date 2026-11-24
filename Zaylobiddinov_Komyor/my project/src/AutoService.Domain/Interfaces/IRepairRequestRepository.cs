using AutoService.Domain.Entities;

namespace AutoService.Domain.Interfaces;

public interface IRepairRequestRepository : IRepository<RepairRequest>
{
    IReadOnlyList<RepairRequest> Search(RequestFilter filter);
    int CountCreatedOn(DateTime day);
    int CountIssuedForClient(int clientId);
    IReadOnlyList<RepairRequest> GetIssuedBetween(DateTime from, DateTime to);
    IReadOnlyList<RepairRequest> GetByCar(int carId);
}
