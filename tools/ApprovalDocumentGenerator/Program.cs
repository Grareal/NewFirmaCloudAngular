using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

if (args.Length != 2)
{
    Console.Error.WriteLine("Uso: ApprovalDocumentGenerator <entrada.md> <salida.pdf>");
    return 2;
}

var input = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
if (!File.Exists(input))
{
    Console.Error.WriteLine($"No existe el archivo de entrada: {input}");
    return 3;
}

var lines = File.ReadAllLines(input);
Directory.CreateDirectory(Path.GetDirectoryName(output)!);

Document.Create(document => document.Page(page =>
{
    page.Size(PageSizes.Letter);
    page.MarginHorizontal(34);
    page.MarginVertical(28);
    page.DefaultTextStyle(style => style.FontFamily("Arial").FontSize(8.6f).FontColor("#333D48"));

    page.Header().PaddingBottom(10).Row(row =>
    {
        row.RelativeItem().Column(column =>
        {
            column.Item().Text("FIRMA OPERA CLOUD").FontSize(11).Bold().FontColor("#003594");
            column.Item().Text("Acta de aprobación e inicio de proyecto").FontSize(7.5f).FontColor("#68717B");
        });
        row.ConstantItem(112).AlignRight().Text("TI-FC-2026-001  |  v1.0").FontSize(7.5f).FontColor("#68717B");
    });

    page.Content().Column(column => RenderMarkdown(column, lines));

    page.Footer().PaddingTop(8).BorderTop(0.5f).BorderColor("#D7E0EA").Row(row =>
    {
        row.RelativeItem().Text("Firma OPERA Cloud — Documento pendiente de aprobación").FontSize(7).FontColor("#68717B");
        row.ConstantItem(90).AlignRight().Text(text =>
        {
            text.Span("Página ").FontSize(7);
            text.CurrentPageNumber().FontSize(7);
            text.Span(" de ").FontSize(7);
            text.TotalPages().FontSize(7);
        });
    });
})).GeneratePdf(output);

Console.WriteLine(output);
return 0;

static void RenderMarkdown(ColumnDescriptor column, IReadOnlyList<string> lines)
{
    for (var index = 0; index < lines.Count; index++)
    {
        var raw = lines[index].TrimEnd();
        if (string.IsNullOrWhiteSpace(raw)) continue;

        if (raw.StartsWith("|", StringComparison.Ordinal))
        {
            var tableLines = new List<string>();
            while (index < lines.Count && lines[index].TrimStart().StartsWith("|", StringComparison.Ordinal))
            {
                tableLines.Add(lines[index].Trim());
                index++;
            }
            index--;
            RenderTable(column, tableLines);
            continue;
        }

        if (raw.StartsWith("# ", StringComparison.Ordinal))
        {
            column.Item().PaddingTop(8).PaddingBottom(5).Background("#003594").Padding(12)
                .Text(Clean(raw[2..])).FontSize(19).Bold().FontColor(Colors.White);
            continue;
        }
        if (raw.StartsWith("## ", StringComparison.Ordinal))
        {
            column.Item().EnsureSpace(40).PaddingTop(12).PaddingBottom(4)
                .Text(Clean(raw[3..])).FontSize(13).Bold().FontColor("#003594");
            column.Item().Height(1).Background("#0095C8");
            continue;
        }
        if (raw.StartsWith("### ", StringComparison.Ordinal))
        {
            column.Item().PaddingTop(8).PaddingBottom(2)
                .Text(Clean(raw[4..])).FontSize(10).Bold().FontColor("#00266F");
            continue;
        }
        if (raw.StartsWith("- ", StringComparison.Ordinal))
        {
            column.Item().PaddingLeft(9).PaddingVertical(1).Row(row =>
            {
                row.ConstantItem(12).Text("•").FontColor("#0095C8");
                row.RelativeItem().Text(Clean(raw[2..])).LineHeight(1.25f);
            });
            continue;
        }
        if (IsNumbered(raw, out var number, out var value))
        {
            column.Item().PaddingLeft(5).PaddingVertical(1).Row(row =>
            {
                row.ConstantItem(22).Text(number + ".").Bold().FontColor("#003594");
                row.RelativeItem().Text(Clean(value)).LineHeight(1.25f);
            });
            continue;
        }

        column.Item().PaddingVertical(2.5f).Text(Clean(raw)).LineHeight(1.3f);
    }
}

static void RenderTable(ColumnDescriptor column, IReadOnlyList<string> lines)
{
    var parsed = lines.Select(ParseRow).Where(row => row.Count > 0).ToList();
    if (parsed.Count < 2) return;
    if (parsed.Count > 1 && parsed[1].All(cell => cell.All(character => character is '-' or ':' or ' ')))
        parsed.RemoveAt(1);
    var columns = parsed.Max(row => row.Count);

    column.Item().PaddingVertical(6).Table(table =>
    {
        table.ColumnsDefinition(definition =>
        {
            for (var i = 0; i < columns; i++) definition.RelativeColumn();
        });
        table.Header(header =>
        {
            foreach (var cell in parsed[0])
                header.Cell().Background("#003594").Border(0.5f).BorderColor("#D7E0EA").Padding(4)
                    .Text(Clean(cell)).FontSize(7.2f).Bold().FontColor(Colors.White);
        });
        foreach (var row in parsed.Skip(1))
        {
            for (var i = 0; i < columns; i++)
            {
                var value = i < row.Count ? row[i] : string.Empty;
                table.Cell().Border(0.5f).BorderColor("#D7E0EA").Padding(4)
                    .Text(Clean(value)).FontSize(7.1f).LineHeight(1.18f);
            }
        }
    });
}

static List<string> ParseRow(string value) => value.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToList();

static string Clean(string value) => value
    .Replace("**", string.Empty, StringComparison.Ordinal)
    .Replace("`", string.Empty, StringComparison.Ordinal);

static bool IsNumbered(string value, out string number, out string content)
{
    var dot = value.IndexOf('.', StringComparison.Ordinal);
    number = dot > 0 ? value[..dot] : string.Empty;
    content = dot >= 0 && dot + 1 < value.Length ? value[(dot + 1)..].TrimStart() : string.Empty;
    return dot is > 0 and < 4 && number.All(char.IsDigit) && content.Length > 0;
}
