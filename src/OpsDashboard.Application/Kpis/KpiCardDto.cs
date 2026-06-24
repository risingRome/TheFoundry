namespace OpsDashboard.Application.Kpis;

public sealed record KpiCardDto(string Name, decimal Value, string Unit, decimal Target, KpiStatus Status);
