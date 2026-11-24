namespace AutoService.Services.Reports;

public record RevenueRow(string Number, DateTime IssuedAt, string Client, string Car, string PaymentMethod, decimal Total);

public record RevenueReport(DateTime From, DateTime To, IReadOnlyList<RevenueRow> Rows)
{
    public int Count => Rows.Count;
    public decimal Total => Rows.Sum(r => r.Total);
    public decimal AverageCheck => Count == 0 ? 0 : Math.Round(Total / Count, 2);
}

public record MechanicLoadRow(string Mechanic, string Specialization, int WorksCount, decimal WorksSum);

public record CarHistoryRow(string Number, DateTime CreatedAt, string Status, string Problem, string Works, decimal Total);
