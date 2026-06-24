using Microsoft.EntityFrameworkCore;
using OpsDashboard.Application.Abstractions;

namespace OpsDashboard.Application.Datasets;

public sealed class DatasetExplorerService(
    IOpsDashboardDbContext db,
    IEnumerable<IDatasetProfileReader> readers) : IDatasetExplorerService
{
    private const string DateType = "Date";
    private const string NumericType = "Numeric";
    private const string TextType = "Text";
    private const string BooleanType = "Boolean";

    public async Task<DatasetPreviewDto?> GetPreviewAsync(int id, int rowLimit = 100, CancellationToken cancellationToken = default)
    {
        var dataset = await db.Datasets
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.DatasetName,
                x.SourceType,
                x.FilePath,
                x.UploadDate,
                DomainName = x.Domain!.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dataset is null || !File.Exists(dataset.FilePath))
        {
            return null;
        }

        var reader = readers.FirstOrDefault(x => x.CanRead(dataset.SourceType));
        if (reader is null)
        {
            throw new NotSupportedException($"Source type '{dataset.SourceType}' is not supported.");
        }

        var rows = await reader.ReadAsync(dataset.FilePath, cancellationToken);
        if (rows.Count == 0)
        {
            return new DatasetPreviewDto(
                dataset.Id,
                dataset.DomainName,
                dataset.DatasetName,
                dataset.SourceType,
                dataset.UploadDate,
                Array.Empty<string>(),
                Array.Empty<IReadOnlyList<string>>(),
                Array.Empty<DatasetColumnSchemaDto>(),
                Array.Empty<DatasetColumnStatisticDto>());
        }

        var columnCount = rows.Max(x => x.Count);
        var columns = BuildColumns(rows[0], columnCount);
        var dataRows = rows
            .Skip(1)
            .Select(row => NormalizeRow(row, columnCount))
            .Where(row => row.Any(value => value.Length > 0))
            .ToList();

        var schema = BuildSchema(columns, dataRows);
        var statistics = BuildStatistics(schema, dataRows);

        return new DatasetPreviewDto(
            dataset.Id,
            dataset.DomainName,
            dataset.DatasetName,
            dataset.SourceType,
            dataset.UploadDate,
            columns,
            dataRows.Take(Math.Clamp(rowLimit, 1, 100)).ToList(),
            schema,
            statistics);
    }

    private static IReadOnlyList<string> BuildColumns(IReadOnlyList<string> headerRow, int columnCount)
    {
        return Enumerable.Range(0, columnCount)
            .Select(index =>
            {
                var header = index < headerRow.Count ? headerRow[index].Trim() : string.Empty;
                return string.IsNullOrWhiteSpace(header) ? $"Column {index + 1}" : header;
            })
            .ToList();
    }

    private static IReadOnlyList<string> NormalizeRow(IReadOnlyList<string> row, int columnCount)
    {
        return Enumerable.Range(0, columnCount)
            .Select(index => index < row.Count ? row[index].Trim() : string.Empty)
            .ToList();
    }

    private static IReadOnlyList<DatasetColumnSchemaDto> BuildSchema(
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        return columns
            .Select((column, index) =>
            {
                var values = rows.Select(row => row[index]).ToList();
                var populatedValues = values.Where(value => value.Length > 0).ToList();
                return new DatasetColumnSchemaDto(
                    column,
                    DetectType(populatedValues),
                    values.Count(value => value.Length == 0));
            })
            .ToList();
    }

    private static IReadOnlyList<DatasetColumnStatisticDto> BuildStatistics(
        IReadOnlyList<DatasetColumnSchemaDto> schema,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        return schema
            .Select((column, index) =>
            {
                var values = rows.Select(row => row[index]).Where(value => value.Length > 0).ToList();
                if (column.DetectedType == NumericType)
                {
                    var numbers = values.Select(decimal.Parse).ToList();
                    return new DatasetColumnStatisticDto(
                        column.Name,
                        column.DetectedType,
                        numbers.Min(),
                        numbers.Max(),
                        Math.Round(numbers.Average(), 2),
                        numbers.Sum(),
                        null);
                }

                if (column.DetectedType == TextType)
                {
                    return new DatasetColumnStatisticDto(
                        column.Name,
                        column.DetectedType,
                        null,
                        null,
                        null,
                        null,
                        values.Distinct(StringComparer.OrdinalIgnoreCase).Count());
                }

                return new DatasetColumnStatisticDto(column.Name, column.DetectedType, null, null, null, null, null);
            })
            .ToList();
    }

    private static string DetectType(IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return TextType;
        }

        if (values.All(IsBoolean))
        {
            return BooleanType;
        }

        if (values.All(value => decimal.TryParse(value, out _)))
        {
            return NumericType;
        }

        if (values.All(value => DateTime.TryParse(value, out _)))
        {
            return DateType;
        }

        return TextType;
    }

    private static bool IsBoolean(string value)
    {
        return bool.TryParse(value, out _)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "no", StringComparison.OrdinalIgnoreCase);
    }
}
