using AutoService.Domain.Entities;
using AutoService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoService.Data.Repositories;

public class ClientRepository : Repository<Client>, IClientRepository
{
    public ClientRepository(AppDbContext db) : base(db) { }

    protected override IQueryable<Client> Query =>
        Db.Clients.Include(c => c.Cars).OrderBy(c => c.FullName);

    /// <summary>
    /// Поиск выполняется в памяти: SQLite сравнивает кириллицу с учётом регистра,
    /// а для небольшого автосервиса список клиентов невелик.
    /// </summary>
    public IReadOnlyList<Client> Search(string text)
    {
        var plate = text.ToUpperInvariant().Replace(" ", "");
        return Query.AsEnumerable()
            .Where(c => c.FullName.Contains(text, StringComparison.OrdinalIgnoreCase)
                        || c.Phone.Contains(text)
                        || c.Cars.Any(car => car.PlateNumber.Contains(plate)))
            .ToList();
    }

    public bool PlateExists(string plateNumber) => Db.Cars.Any(c => c.PlateNumber == plateNumber);
}
