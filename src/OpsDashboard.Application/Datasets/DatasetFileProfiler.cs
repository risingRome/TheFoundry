using OpsDashboard.Application.Abstractions;

namespace OpsDashboard.Application.Datasets;

public sealed class DatasetFileProfiler(IEnumerable<IDatasetProfileReader> readers) : IDatasetFileProfiler
{
    public async Task<DatasetProfileResult> ProfileAsync(string filePath, string sourceType, CancellationToken cancellationToken = default)
    {
        var reader = readers.FirstOrDefault(x => x.CanRead(sourceType));
        if (reader is null)
        {
            throw new NotSupportedException($"Source type '{sourceType}' is not supported.");
        }

        var rows = await reader.ReadAsync(filePath, cancellationToken);
        return Calculate(rows);
    }

    private static DatasetProfileResult Calculate(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        if (rows.Count == 0)
        {
            return new DatasetProfileResult(0, 0, 0, 0, 0, 0, 0, 0);
        }

        var columnCount = rows.Max(x => x.Count);
        var dataRows = rows.Count > 1 ? rows.Skip(1).ToList() : rows.ToList();
        var rowCount = dataRows.Count;
        var normalizedRows = dataRows
            .Select(row => Enumerable.Range(0, columnCount).Select(i => Normalize(GetCell(row, i))).ToArray())
            .Where(row => row.Any(cell => cell.Length > 0))
            .ToList();

        rowCount = normalizedRows.Count;
        var missingValues = normalizedRows.Sum(row => row.Count(cell => cell.Length == 0));
        var duplicateRows = normalizedRows
            .GroupBy(row => string.Join('\u001f', row), StringComparer.OrdinalIgnoreCase)
            .Sum(group => Math.Max(0, group.Count() - 1));

        var numericColumns = 0;
        var dateColumns = 0;
        var textColumns = 0;

        for (var column = 0; column < columnCount; column++)
        {
            var values = normalizedRows.Select(row => row[column]).Where(value => value.Length > 0).ToList();
            if (values.Count == 0)
            {
                continue;
            }

            if (values.All(value => decimal.TryParse(value, out _)))
            {
                numericColumns++;
            }
            else if (values.All(value => DateTime.TryParse(value, out _)))
            {
                dateColumns++;
            }
            else
            {
                textColumns++;
            }
        }

        var totalCells = Math.Max(1, rowCount * Math.Max(1, columnCount));
        var missingPenalty = (decimal)missingValues / totalCells * 55m;
        var duplicatePenalty = rowCount == 0 ? 0m : (decimal)duplicateRows / rowCount * 35m;
        var qualityScore = Math.Clamp(100m - missingPenalty - duplicatePenalty, 0m, 100m);

        return new DatasetProfileResult(
            rowCount,
            columnCount,
            missingValues,
            duplicateRows,
            numericColumns,
            dateColumns,
            textColumns,
            Math.Round(qualityScore, 2));
    }

    private static string GetCell(IReadOnlyList<string> row, int index)
    {
        return index < row.Count ? row[index] : string.Empty;
    }

    private static string Normalize(string value)
    {
        return value.Trim();
    }
}
