using OpsDashboard.Domain.Enums;

namespace OpsDashboard.Application.Datasets;

public sealed record DatasetUploadRequest(
    int DomainId,
    string DatasetName,
    string SourceType,
    string FilePath);

public sealed record DatasetProfileResult(
    int RowCount,
    int ColumnCount,
    int MissingValues,
    int DuplicateRows,
    int NumericColumns,
    int DateColumns,
    int TextColumns,
    decimal DatasetQualityScore);

public sealed record DatasetListItemDto(
    int Id,
    int DomainId,
    string DomainName,
    string DatasetName,
    string SourceType,
    DateTime UploadDate,
    DatasetStatus Status,
    int RowCount,
    int ColumnCount,
    decimal DatasetQualityScore);

public sealed record DatasetDetailsDto(
    int Id,
    int DomainId,
    string DomainName,
    string DatasetName,
    string SourceType,
    string FilePath,
    DateTime UploadDate,
    DatasetStatus Status,
    int RowCount,
    int ColumnCount,
    int MissingValues,
    int DuplicateRows,
    int NumericColumns,
    int DateColumns,
    int TextColumns,
    decimal DatasetQualityScore);

public sealed record DatasetPreviewDto(
    int Id,
    string DomainName,
    string DatasetName,
    string SourceType,
    DateTime UploadDate,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<DatasetColumnSchemaDto> Schema,
    IReadOnlyList<DatasetColumnStatisticDto> Statistics);

public sealed record DatasetColumnSchemaDto(
    string Name,
    string DetectedType,
    int MissingValues);

public sealed record DatasetColumnStatisticDto(
    string ColumnName,
    string DetectedType,
    decimal? Min,
    decimal? Max,
    decimal? Average,
    decimal? Sum,
    int? DistinctCount);
