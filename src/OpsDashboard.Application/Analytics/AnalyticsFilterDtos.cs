namespace OpsDashboard.Application.Analytics;

public sealed record AnalyticsFilterDto(
    DateOnly? From,
    DateOnly? To,
    string? Department,
    string? Region,
    string? Employee,
    string? Product,
    string? Category)
{
    public static AnalyticsFilterDto Empty { get; } = new(null, null, null, null, null, null, null);
}

public sealed record AnalyticsFilterOptionDto(
    string Key,
    string Label,
    string ColumnName,
    IReadOnlyList<string> Values,
    string? SelectedValue);

public sealed record AnalyticsFilterStateDto(
    DateOnly? From,
    DateOnly? To,
    IReadOnlyList<AnalyticsFilterOptionDto> Options,
    string Summary,
    bool HasActiveFilters);
