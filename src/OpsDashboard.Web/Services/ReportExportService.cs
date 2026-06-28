using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using OpsDashboard.Application.Reports;

namespace OpsDashboard.Web.Services;

public sealed class ReportExportService : IReportExportService
{
    public byte[] ExportPdf(ExecutiveReportDto report)
    {
        var writer = new SimplePdfWriter();
        writer.AddTitle("The Foundry");
        writer.AddSubtitle("Executive Business Report");
        writer.AddParagraph($"{report.DatasetName} | {report.DomainName} | Generated {report.GeneratedAtUtc:dd MMM yyyy HH:mm} UTC");
        writer.AddParagraph(report.Filters.Summary);
        writer.AddHeading("Dataset Information");
        writer.AddTable(["Field", "Value"], [
            ["Dataset name", report.DatasetName],
            ["Domain", report.DomainName],
            ["File type", report.SourceType],
            ["Upload date", $"{report.UploadDate:dd MMM yyyy HH:mm} UTC"],
            ["Report generation date", $"{report.GeneratedAtUtc:dd MMM yyyy HH:mm} UTC"]
        ]);
        writer.AddHeading("Executive Summary");
        writer.AddTable(["Metric", "Value"], [
            ["Total records", report.Summary.TotalRecords.ToString("N0")],
            ["Total columns", report.Summary.TotalColumns.ToString("N0")],
            ["Data quality score", $"{report.Summary.DataQualityScore:0.##}%"],
            ["Missing values", report.Summary.MissingValues.ToString("N0")],
            ["Duplicate rows", report.Summary.DuplicateRows.ToString("N0")]
        ]);
        writer.AddHeading("KPI Summary");
        writer.AddTable(["KPI", "Value", "Description"], report.Kpis.Select(x => new[] { x.Title, x.Value, x.Description }).ToList());
        writer.AddHeading("Trend Analysis");
        writer.AddList(report.TrendAnalysis);
        writer.AddHeading("Charts");
        writer.AddTable(["Chart", "Rule", "Data points"], report.Charts.Select(x => new[] { x.Title, x.Rule, x.Values.Count.ToString("N0") }).ToList());
        writer.AddHeading("Dimension Performance");
        AddRankings(writer, report);
        writer.AddHeading("Key Findings");
        writer.AddList(report.KeyFindings);
        writer.AddHeading("Executive Recommendations");
        writer.AddList(report.Recommendations);
        return writer.Build();
    }

    public byte[] ExportDocx(ExecutiveReportDto report)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddTextEntry(archive, "[Content_Types].xml", ContentTypesXml);
            AddTextEntry(archive, "_rels/.rels", RelationshipsXml);
            AddTextEntry(archive, "word/_rels/document.xml.rels", DocumentRelationshipsXml);
            AddTextEntry(archive, "word/styles.xml", StylesXml);
            AddTextEntry(archive, "word/settings.xml", SettingsXml);
            AddTextEntry(archive, "docProps/core.xml", BuildCoreXml(report));
            AddTextEntry(archive, "docProps/app.xml", AppXml);
            AddTextEntry(archive, "word/document.xml", BuildDocumentXml(report));
        }

        return stream.ToArray();
    }

    private static void AddRankings(SimplePdfWriter writer, ExecutiveReportDto report)
    {
        if (report.DimensionRankings.Count == 0)
        {
            writer.AddParagraph("No recognized Region, Product, or Department ranking was generated.");
            return;
        }

        foreach (var ranking in report.DimensionRankings)
        {
            writer.AddParagraph($"{ranking.DimensionName} by {ranking.MeasureName}");
            writer.AddTable(["Rank", ranking.DimensionName, ranking.MeasureName], ranking.Rows.Select(x => new[] { x.Rank.ToString(), x.Label, x.Value.ToString("N2") }).ToList());
        }
    }

    private static string BuildDocumentXml(ExecutiveReportDto report)
    {
        var body = new StringBuilder();
        body.Append(Paragraph("The Foundry", "Title"));
        body.Append(Paragraph("Executive Business Report", "Subtitle"));
        body.Append(Paragraph($"{report.DatasetName} | {report.DomainName} | Generated {report.GeneratedAtUtc:dd MMM yyyy HH:mm} UTC", "Meta"));
        body.Append(Paragraph(report.Filters.Summary, "Meta"));
        body.Append(Heading("Dataset Information"));
        body.Append(Table(["Field", "Value"], [
            ["Dataset name", report.DatasetName],
            ["Domain", report.DomainName],
            ["File type", report.SourceType],
            ["Upload date", $"{report.UploadDate:dd MMM yyyy HH:mm} UTC"],
            ["Report generation date", $"{report.GeneratedAtUtc:dd MMM yyyy HH:mm} UTC"]
        ]));
        body.Append(Heading("Executive Summary"));
        body.Append(Table(["Metric", "Value"], [
            ["Total records", report.Summary.TotalRecords.ToString("N0")],
            ["Total columns", report.Summary.TotalColumns.ToString("N0")],
            ["Data quality score", $"{report.Summary.DataQualityScore:0.##}%"],
            ["Missing values", report.Summary.MissingValues.ToString("N0")],
            ["Duplicate rows", report.Summary.DuplicateRows.ToString("N0")]
        ]));
        body.Append(Heading("KPI Summary"));
        body.Append(Table(["KPI", "Value", "Description"], report.Kpis.Select(x => new[] { x.Title, x.Value, x.Description }).ToList()));
        body.Append(Heading("Trend Analysis"));
        body.Append(BulletList(report.TrendAnalysis));
        body.Append(Heading("Charts"));
        body.Append(Table(["Chart", "Rule", "Data points"], report.Charts.Select(x => new[] { x.Title, x.Rule, x.Values.Count.ToString("N0") }).ToList()));
        body.Append(Heading("Dimension Performance"));
        if (report.DimensionRankings.Count == 0)
        {
            body.Append(Paragraph("No recognized Region, Product, or Department ranking was generated.", "Normal"));
        }
        else
        {
            foreach (var ranking in report.DimensionRankings)
            {
                body.Append(Paragraph($"{ranking.DimensionName} by {ranking.MeasureName}", "Heading2"));
                body.Append(Table(["Rank", ranking.DimensionName, ranking.MeasureName], ranking.Rows.Select(x => new[] { x.Rank.ToString(), x.Label, x.Value.ToString("N2") }).ToList()));
            }
        }

        body.Append(Heading("Key Findings"));
        body.Append(BulletList(report.KeyFindings));
        body.Append(Heading("Executive Recommendations"));
        body.Append(BulletList(report.Recommendations));
        body.Append("<w:sectPr><w:pgSz w:w=\"12240\" w:h=\"15840\"/><w:pgMar w:top=\"1080\" w:right=\"1080\" w:bottom=\"1080\" w:left=\"1080\" w:header=\"720\" w:footer=\"720\" w:gutter=\"0\"/></w:sectPr>");

        return $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>{body}</w:body></w:document>""";
    }

    private static string Heading(string text) => Paragraph(text, "Heading1");

    private static string BulletList(IEnumerable<string> items)
    {
        var builder = new StringBuilder();
        foreach (var item in items)
        {
            builder.Append($"""<w:p><w:pPr><w:pStyle w:val="Bullet"/><w:ind w:left="360" w:hanging="180"/></w:pPr><w:r><w:t>{Xml(item)}</w:t></w:r></w:p>""");
        }

        return builder.ToString();
    }

    private static string Table(IReadOnlyList<string> headers, IReadOnlyList<string[]> rows)
    {
        var width = 9360 / headers.Count;
        var builder = new StringBuilder();
        builder.Append($"""<w:tbl><w:tblPr><w:tblW w:w="9360" w:type="dxa"/><w:tblBorders><w:top w:val="single" w:sz="4" w:color="D7DEE8"/><w:left w:val="single" w:sz="4" w:color="D7DEE8"/><w:bottom w:val="single" w:sz="4" w:color="D7DEE8"/><w:right w:val="single" w:sz="4" w:color="D7DEE8"/><w:insideH w:val="single" w:sz="4" w:color="D7DEE8"/><w:insideV w:val="single" w:sz="4" w:color="D7DEE8"/></w:tblBorders></w:tblPr><w:tblGrid>""");
        foreach (var _ in headers)
        {
            builder.Append($"""<w:gridCol w:w="{width}"/>""");
        }
        builder.Append("</w:tblGrid>");
        builder.Append(Row(headers, true, width));
        foreach (var row in rows)
        {
            builder.Append(Row(row, false, width));
        }
        builder.Append("</w:tbl>");
        builder.Append(Paragraph(string.Empty, "Spacing"));
        return builder.ToString();
    }

    private static string Row(IReadOnlyList<string> cells, bool header, int width)
    {
        var builder = new StringBuilder("<w:tr>");
        foreach (var cell in cells)
        {
            var fill = header ? """<w:shd w:fill="EEF4FF"/>""" : string.Empty;
            var bold = header ? "<w:b/>" : string.Empty;
            builder.Append($"""<w:tc><w:tcPr><w:tcW w:w="{width}" w:type="dxa"/>{fill}<w:tcMar><w:top w:w="120" w:type="dxa"/><w:left w:w="120" w:type="dxa"/><w:bottom w:w="120" w:type="dxa"/><w:right w:w="120" w:type="dxa"/></w:tcMar></w:tcPr><w:p><w:r><w:rPr>{bold}</w:rPr><w:t>{Xml(cell)}</w:t></w:r></w:p></w:tc>""");
        }
        builder.Append("</w:tr>");
        return builder.ToString();
    }

    private static string Paragraph(string text, string style)
    {
        return $"""<w:p><w:pPr><w:pStyle w:val="{style}"/></w:pPr><w:r><w:t>{Xml(text)}</w:t></w:r></w:p>""";
    }

    private static string BuildCoreXml(ExecutiveReportDto report)
    {
        return $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:dcmitype="http://purl.org/dc/dcmitype/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"><dc:title>{Xml(report.DatasetName)} Executive Report</dc:title><dc:creator>The Foundry</dc:creator><cp:lastModifiedBy>The Foundry</cp:lastModifiedBy><dcterms:created xsi:type="dcterms:W3CDTF">{report.GeneratedAtUtc:O}</dcterms:created><dcterms:modified xsi:type="dcterms:W3CDTF">{report.GeneratedAtUtc:O}</dcterms:modified></cp:coreProperties>""";
    }

    private static void AddTextEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string Xml(string value) => SecurityElement.Escape(value) ?? string.Empty;

    private const string ContentTypesXml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/><Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/><Override PartName="/word/settings.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.settings+xml"/><Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/><Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/></Types>""";
    private const string RelationshipsXml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/><Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/></Relationships>""";
    private const string DocumentRelationshipsXml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/settings" Target="settings.xml"/></Relationships>""";
    private const string SettingsXml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:settings xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:zoom w:percent="100"/></w:settings>""";
    private const string AppXml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes"><Application>The Foundry</Application></Properties>""";
    private const string StylesXml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/><w:pPr><w:spacing w:after="120" w:line="276" w:lineRule="auto"/></w:pPr><w:rPr><w:rFonts w:ascii="Arial" w:hAnsi="Arial"/><w:sz w:val="20"/><w:color w:val="172033"/></w:rPr></w:style><w:style w:type="paragraph" w:styleId="Title"><w:name w:val="Title"/><w:pPr><w:spacing w:after="80"/></w:pPr><w:rPr><w:rFonts w:ascii="Arial" w:hAnsi="Arial"/><w:b/><w:sz w:val="34"/><w:color w:val="111827"/></w:rPr></w:style><w:style w:type="paragraph" w:styleId="Subtitle"><w:name w:val="Subtitle"/><w:pPr><w:spacing w:after="240"/></w:pPr><w:rPr><w:rFonts w:ascii="Arial" w:hAnsi="Arial"/><w:sz w:val="24"/><w:color w:val="2563EB"/></w:rPr></w:style><w:style w:type="paragraph" w:styleId="Meta"><w:name w:val="Meta"/><w:pPr><w:spacing w:after="240"/></w:pPr><w:rPr><w:rFonts w:ascii="Arial" w:hAnsi="Arial"/><w:sz w:val="18"/><w:color w:val="64748B"/></w:rPr></w:style><w:style w:type="paragraph" w:styleId="Heading1"><w:name w:val="Heading 1"/><w:pPr><w:spacing w:before="260" w:after="120"/><w:outlineLvl w:val="0"/></w:pPr><w:rPr><w:rFonts w:ascii="Arial" w:hAnsi="Arial"/><w:b/><w:sz w:val="24"/><w:color w:val="111827"/></w:rPr></w:style><w:style w:type="paragraph" w:styleId="Heading2"><w:name w:val="Heading 2"/><w:pPr><w:spacing w:before="160" w:after="80"/><w:outlineLvl w:val="1"/></w:pPr><w:rPr><w:rFonts w:ascii="Arial" w:hAnsi="Arial"/><w:b/><w:sz w:val="20"/><w:color w:val="2563EB"/></w:rPr></w:style><w:style w:type="paragraph" w:styleId="Bullet"><w:name w:val="Bullet"/><w:pPr><w:spacing w:after="80"/></w:pPr><w:rPr><w:rFonts w:ascii="Arial" w:hAnsi="Arial"/><w:sz w:val="20"/></w:rPr></w:style><w:style w:type="paragraph" w:styleId="Spacing"><w:name w:val="Spacing"/><w:pPr><w:spacing w:after="120"/></w:pPr></w:style></w:styles>""";

    private sealed class SimplePdfWriter
    {
        private readonly List<string> _commands = [];
        private readonly List<int> _pageObjectIds = [];
        private int _pageNumber = 1;
        private decimal _y = 760m;

        public void AddTitle(string text) => AddText(text, 24, true);
        public void AddSubtitle(string text) => AddText(text, 16, false);
        public void AddHeading(string text) { EnsureSpace(42); AddText(text, 15, true); }
        public void AddParagraph(string text) => AddWrappedText(text, 10, 70);
        public void AddList(IEnumerable<string> items)
        {
            foreach (var item in items)
            {
                AddWrappedText($"- {item}", 10, 82);
            }
        }

        public void AddTable(IReadOnlyList<string> headers, IReadOnlyList<string[]> rows)
        {
            EnsureSpace(44);
            AddWrappedText(string.Join(" | ", headers), 9, 95, true);
            foreach (var row in rows)
            {
                AddWrappedText(string.Join(" | ", row), 8, 100);
            }
            _y -= 8;
        }

        public byte[] Build()
        {
            AddFooter();
            var objects = new List<string>();
            objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
            objects.Add($"<< /Type /Pages /Kids [{string.Join(' ', _pageObjectIds.Select(id => $"{id} 0 R"))}] /Count {_pageObjectIds.Count} >>");
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

            var contentObjects = new List<string>();
            foreach (var pageId in _pageObjectIds)
            {
                var contentId = pageId + 1;
                var stream = _commands[(pageId - 4) / 2];
                objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentId} 0 R >>");
                contentObjects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream");
                objects.Add(contentObjects.Last());
            }

            var builder = new StringBuilder("%PDF-1.4\n");
            var offsets = new List<int> { 0 };
            for (var i = 0; i < objects.Count; i++)
            {
                offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
                builder.Append(CultureInfo.InvariantCulture, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            var xref = Encoding.ASCII.GetByteCount(builder.ToString());
            builder.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
            foreach (var offset in offsets.Skip(1))
            {
                builder.Append(CultureInfo.InvariantCulture, $"{offset:0000000000} 00000 n \n");
            }
            builder.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        private void AddText(string text, int size, bool bold)
        {
            EnsureSpace(size + 12);
            var escaped = EscapePdf(text);
            _commands[^1] += $"BT /F1 {size} Tf 54 {_y:0.##} Td ({escaped}) Tj ET\n";
            _y -= size + (bold ? 8 : 6);
        }

        private void AddWrappedText(string text, int size, int width, bool bold = false)
        {
            foreach (var line in Wrap(text, width))
            {
                AddText(line, size, bold);
            }
            _y -= 2;
        }

        private void EnsureSpace(decimal needed)
        {
            if (_commands.Count == 0)
            {
                NewPage();
            }
            else if (_y - needed < 54)
            {
                AddFooter();
                NewPage();
            }
        }

        private void NewPage()
        {
            _pageObjectIds.Add(4 + (_commands.Count * 2));
            _commands.Add(string.Empty);
            _y = 760m;
        }

        private void AddFooter()
        {
            if (_commands.Count == 0)
            {
                return;
            }

            _commands[^1] += $"BT /F1 8 Tf 54 28 Td (The Foundry Executive Report) Tj ET\n";
            _commands[^1] += $"BT /F1 8 Tf 520 28 Td (Page {_pageNumber}) Tj ET\n";
            _pageNumber++;
        }

        private static IEnumerable<string> Wrap(string text, int width)
        {
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = new StringBuilder();
            foreach (var word in words)
            {
                if (line.Length + word.Length + 1 > width)
                {
                    yield return line.ToString();
                    line.Clear();
                }
                if (line.Length > 0)
                {
                    line.Append(' ');
                }
                line.Append(word);
            }
            if (line.Length > 0)
            {
                yield return line.ToString();
            }
        }

        private static string EscapePdf(string value)
        {
            return value.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);
        }
    }
}
