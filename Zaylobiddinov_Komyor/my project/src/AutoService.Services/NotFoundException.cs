using AutoService.Domain.Exceptions;

namespace AutoService.Services;

/// <summary>Запрошенный объект не найден в базе данных.</summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string what, int id) : base($"{what} с кодом {id} не найден(а).") { }
}
