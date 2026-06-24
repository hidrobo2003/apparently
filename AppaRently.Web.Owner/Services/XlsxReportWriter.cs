using System.IO.Compression;
using System.Net.Mime;
using System.Text;

namespace AppaRently.Web.Owner.Services;

internal static class XlsxReportWriter
{
    public static byte[] Write(string sheetName, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", BuildContentTypesXml());
            WriteEntry(archive, "_rels/.rels", BuildRootRelsXml());
            WriteEntry(archive, "xl/workbook.xml", BuildWorkbookXml(sheetName));
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelsXml());
            WriteEntry(archive, "xl/styles.xml", BuildStylesXml());
            WriteEntry(archive, "xl/worksheets/sheet1.xml", BuildSheetXml(headers, rows));
        }

        return stream.ToArray();
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static string BuildContentTypesXml() =>
        """
<?xml version="1.0" encoding="utf-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml" />
  <Default Extension="xml" ContentType="application/xml" />
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml" />
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml" />
  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml" />
</Types>
""";

    private static string BuildRootRelsXml() => """
<?xml version="1.0" encoding="utf-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml" />
</Relationships>
""";

    private static string BuildWorkbookXml(string sheetName) => $"""
<?xml version="1.0" encoding="utf-8"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
    <sheet name="{Escape(sheetName)}" sheetId="1" r:id="rId1" />
  </sheets>
</workbook> 
""";

    private static string BuildWorkbookRelsXml() => """
<?xml version="1.0" encoding="utf-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml" />
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml" />
</Relationships>
""";

    private static string BuildStylesXml() => """
<?xml version="1.0" encoding="utf-8"?>
<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <fonts count="2">
    <font>
      <sz val="11" />
      <color theme="1" />
      <name val="Aptos" />
      <family val="2" />
    </font>
    <font>
      <b />
      <sz val="11" />
      <color theme="1" />
      <name val="Aptos" />
      <family val="2" />
    </font>
  </fonts>
  <fills count="1">
    <fill>
      <patternFill patternType="none" />
    </fill>
  </fills>
  <borders count="1">
    <border>
      <left />
      <right />
      <top />
      <bottom />
      <diagonal />
    </border>
  </borders>
  <cellStyleXfs count="1">
    <xf numFmtId="0" fontId="0" fillId="0" borderId="0" />
  </cellStyleXfs>
  <cellXfs count="2">
    <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" />
    <xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1" />
  </cellXfs>
</styleSheet>
""";

    private static string BuildSheetXml(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        var sheet = new StringBuilder();
        sheet.Append("""
<?xml version="1.0" encoding="utf-8"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <sheetData>
""");

        sheet.AppendLine();
        sheet.Append("    <row r=\"1\">");
        for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
        {
            var cellReference = $"{GetColumnName(columnIndex + 1)}1";
            sheet.Append($"""<c r="{cellReference}" t="inlineStr" s="1"><is><t>{Escape(headers[columnIndex])}</t></is></c>""");
        }
        sheet.AppendLine("</row>");

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var rowNumber = rowIndex + 2;
            var row = rows[rowIndex];
            sheet.Append($"""    <row r="{rowNumber}">""");

            for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                var cellReference = $"{GetColumnName(columnIndex + 1)}{rowNumber}";
                var cellValue = row[columnIndex];
                sheet.Append(BuildCellXml(cellReference, cellValue));
            }

            sheet.AppendLine("</row>");
        }

        sheet.AppendLine("  </sheetData>");
        sheet.Append("</worksheet>");
        return sheet.ToString();
    }

    private static string BuildCellXml(string reference, object? value)
    {
        if (value is null)
        {
            return $"""<c r="{reference}" t="inlineStr"><is><t></t></is></c>""";
        }

        return value switch
        {
            bool booleanValue => $"""<c r="{reference}" t="inlineStr"><is><t>{Escape(booleanValue ? "Yes" : "No")}</t></is></c>""",
            DateTime dateTime => $"""<c r="{reference}" t="inlineStr"><is><t>{Escape(dateTime.ToString("yyyy-MM-dd HH:mm"))}</t></is></c>""",
            DateOnly dateOnly => $"""<c r="{reference}" t="inlineStr"><is><t>{Escape(dateOnly.ToString("yyyy-MM-dd"))}</t></is></c>""",
            decimal decimalValue => $"""<c r="{reference}"><v>{decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}</v></c>""",
            double doubleValue => $"""<c r="{reference}"><v>{doubleValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}</v></c>""",
            float floatValue => $"""<c r="{reference}"><v>{floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}</v></c>""",
            int intValue => $"""<c r="{reference}"><v>{intValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}</v></c>""",
            long longValue => $"""<c r="{reference}"><v>{longValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}</v></c>""",
            _ => $"""<c r="{reference}" t="inlineStr"><is><t>{Escape(value.ToString() ?? string.Empty)}</t></is></c>"""
        };
    }

    private static string GetColumnName(int columnNumber)
    {
        var columnName = string.Empty;
        while (columnNumber > 0)
        {
            var modulo = (columnNumber - 1) % 26;
            columnName = Convert.ToChar(65 + modulo) + columnName;
            columnNumber = (columnNumber - modulo) / 26;
        }

        return columnName;
    }

    private static string Escape(string value) =>
        System.Security.SecurityElement.Escape(value) ?? string.Empty;
}
