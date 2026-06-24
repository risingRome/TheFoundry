using System.IO.Compression;
using System.Xml.Linq;
using OpsDashboard.Application.Abstractions;

namespace OpsDashboard.Infrastructure.Datasets;

public sealed class XlsxDatasetProfileReader : IDatasetProfileReader
{
    private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

    public bool CanRead(string sourceType)
    {
        return string.Equals(sourceType, "Excel", StringComparison.OrdinalIgnoreCase)
            || string.Equals(sourceType, "XLSX", StringComparison.OrdinalIgnoreCase);
    }

    public Task<IReadOnlyList<IReadOnlyList<string>>> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var archive = ZipFile.OpenRead(filePath);
        var sharedStrings = ReadSharedStrings(archive);
        var worksheetEntry = ResolveFirstWorksheetEntry(archive);
        if (worksheetEntry is null)
        {
            return Task.FromResult<IReadOnlyList<IReadOnlyList<string>>>(Array.Empty<IReadOnlyList<string>>());
        }

        using var stream = worksheetEntry.Open();
        var document = XDocument.Load(stream);
        var rows = document
            .Descendants(SpreadsheetNs + "row")
            .Select(row => ReadRow(row, sharedStrings))
            .Where(row => row.Count > 0)
            .ToList();

        return Task.FromResult<IReadOnlyList<IReadOnlyList<string>>>(rows);
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return Array.Empty<string>();
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        return document
            .Descendants(SpreadsheetNs + "si")
            .Select(item => string.Concat(item.Descendants(SpreadsheetNs + "t").Select(text => text.Value)))
            .ToList();
    }

    private static ZipArchiveEntry? ResolveFirstWorksheetEntry(ZipArchive archive)
    {
        var workbookEntry = archive.GetEntry("xl/workbook.xml");
        var relationshipsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
        if (workbookEntry is null || relationshipsEntry is null)
        {
            return archive.GetEntry("xl/worksheets/sheet1.xml");
        }

        using var workbookStream = workbookEntry.Open();
        using var relationshipsStream = relationshipsEntry.Open();
        var workbook = XDocument.Load(workbookStream);
        var relationships = XDocument.Load(relationshipsStream);
        var firstSheet = workbook.Descendants(SpreadsheetNs + "sheet").FirstOrDefault();
        var relationshipId = firstSheet?.Attribute(RelationshipNs + "id")?.Value;
        if (relationshipId is null)
        {
            return archive.GetEntry("xl/worksheets/sheet1.xml");
        }

        var target = relationships
            .Descendants(PackageRelationshipNs + "Relationship")
            .FirstOrDefault(x => x.Attribute("Id")?.Value == relationshipId)
            ?.Attribute("Target")
            ?.Value;

        if (string.IsNullOrWhiteSpace(target))
        {
            return archive.GetEntry("xl/worksheets/sheet1.xml");
        }

        var path = target.StartsWith("worksheets/", StringComparison.OrdinalIgnoreCase)
            ? $"xl/{target}"
            : target.TrimStart('/');

        return archive.GetEntry(path.Replace('\\', '/'));
    }

    private static IReadOnlyList<string> ReadRow(XElement row, IReadOnlyList<string> sharedStrings)
    {
        var values = new SortedDictionary<int, string>();
        foreach (var cell in row.Elements(SpreadsheetNs + "c"))
        {
            var reference = cell.Attribute("r")?.Value;
            var columnIndex = GetColumnIndex(reference);
            if (columnIndex < 0)
            {
                continue;
            }

            values[columnIndex] = ReadCellValue(cell, sharedStrings);
        }

        if (values.Count == 0)
        {
            return Array.Empty<string>();
        }

        var max = values.Keys.Max();
        return Enumerable.Range(0, max + 1)
            .Select(index => values.TryGetValue(index, out var value) ? value : string.Empty)
            .ToList();
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var type = cell.Attribute("t")?.Value;
        var rawValue = cell.Element(SpreadsheetNs + "v")?.Value ?? cell.Element(SpreadsheetNs + "is")?.Value ?? string.Empty;
        if (type == "s" && int.TryParse(rawValue, out var index) && index >= 0 && index < sharedStrings.Count)
        {
            return sharedStrings[index];
        }

        return rawValue;
    }

    private static int GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return -1;
        }

        var columnLetters = new string(cellReference.TakeWhile(char.IsLetter).ToArray());
        if (columnLetters.Length == 0)
        {
            return -1;
        }

        var index = 0;
        foreach (var letter in columnLetters.ToUpperInvariant())
        {
            index = index * 26 + letter - 'A' + 1;
        }

        return index - 1;
    }
}
