using OpsDashboard.Domain.Enums;

namespace OpsDashboard.Application.Domains;

public sealed record DomainFormDto(string Name, string? Description);

public sealed record DomainListItemDto(
    int Id,
    string Name,
    string? Description,
    DomainStatus Status,
    DateTime CreatedDate,
    int DatasetCount);

public sealed record DomainDetailsDto(
    int Id,
    string Name,
    string? Description,
    DomainStatus Status,
    DateTime CreatedDate,
    IReadOnlyList<DomainDatasetDto> Datasets);

public sealed record DomainDatasetDto(
    int Id,
    string DatasetName,
    string SourceType,
    DateTime UploadDate,
    int RowCount,
    int ColumnCount,
    decimal DatasetQualityScore);
