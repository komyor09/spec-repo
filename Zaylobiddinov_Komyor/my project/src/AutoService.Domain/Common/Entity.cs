namespace AutoService.Domain.Common;

/// <summary>
/// Базовый класс всех сущностей, которые хранятся в базе данных.
/// Идентификатор назначается базой данных, поэтому снаружи его изменить нельзя.
/// </summary>
public abstract class Entity
{
    public int Id { get; protected set; }
}
