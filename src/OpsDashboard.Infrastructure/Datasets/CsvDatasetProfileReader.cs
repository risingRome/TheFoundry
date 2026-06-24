using System.Text;
using OpsDashboard.Application.Abstractions;

namespace OpsDashboard.Infrastructure.Datasets;

public sealed class CsvDatasetProfileReader : IDatasetProfileReader
{
    public bool CanRead(string sourceType)
    {
        return string.Equals(sourceType, "CSV", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<IReadOnlyList<string>>> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var rows = new List<IReadOnlyList<string>>();
        await using var stream = File.OpenRead(filePath);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(ParseCsvLine(line));
        }

        return rows;
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var current = line[i];
            if (current == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (current == ',' && !inQuotes)
            {
                values.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(current);
            }
        }

        values.Add(value.ToString());
        return values;
    }
}
