using OpsDashboard.Domain.Enums;

namespace OpsDashboard.Application.DataRefresh;

public sealed record RefreshCenterDatasetDto(
    int DatasetId,
    string DatasetName,
    string DomainName,
    DateTime? LastRefreshTimeUtc,
    DataRefreshStatus? LastRefreshStatus,
    int? RecordsProcessed,
    string? LastMessage);

public sealed record DatasetRefreshHistoryDto(
    int DatasetId,
    string DatasetName,
    string DomainName,
    IReadOnlyList<DataRefreshJobDto> Jobs);

public sealed record DataRefreshJobDto(
    int Id,
    int DatasetId,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    DataRefreshStatus Status,
    int RecordsProcessed,
    string? TriggeredByUserId,
    string? TriggeredByEmail,
    string? Message);
