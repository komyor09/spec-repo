using AutoService.Domain.Entities;

namespace AutoService.Domain.Interfaces;

public interface IClientRepository : IRepository<Client>
{
    /// <summary>Поиск по ФИО, телефону или госномеру автомобиля.</summary>
    IReadOnlyList<Client> Search(string text);
    bool PlateExists(string plateNumber);
}
