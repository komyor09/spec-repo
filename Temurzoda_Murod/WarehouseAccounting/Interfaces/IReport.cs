namespace WarehouseAccounting.Interfaces;

/// <summary>
/// Любой объект, который умеет формировать текстовый отчёт.
/// </summary>
public interface IReport
{
    string BuildReport();
}