using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoToWebinarCLI.Helpers;

public enum OutputFormat
{
    Table,
    Json,
    Csv
}

public static class OutputFormatter
{
    public static void WriteOutput<T>(IEnumerable<T> items, OutputFormat format, string[] columns, JsonSerializerContext? jsonContext = null)
    {
        var itemsList = items.ToList();

        switch (format)
        {
            case OutputFormat.Json:
                WriteJson(itemsList, jsonContext);
                break;
            case OutputFormat.Csv:
                WriteCsv(itemsList, columns);
                break;
            case OutputFormat.Table:
            default:
                WriteTable(itemsList, columns);
                break;
        }
    }

    private static void WriteJson<T>(List<T> items, JsonSerializerContext? jsonContext)
    {
        if (jsonContext != null)
        {
            var json = JsonSerializer.Serialize(items, typeof(List<T>), jsonContext);
            Console.WriteLine(json);
        }
        else
        {
            Console.WriteLine("[]");
        }
    }

    private static void WriteCsv<T>(List<T> items, string[] columns)
    {
        if (items.Count == 0)
            return;

        Console.WriteLine(string.Join(",", columns.Select(EscapeCsvField)));

        foreach (var item in items)
        {
            var values = GetItemValues(item, columns);
            Console.WriteLine(string.Join(",", values.Select(EscapeCsvField)));
        }
    }

    private static void WriteTable<T>(List<T> items, string[] columns)
    {
        if (items.Count == 0)
        {
            Console.WriteLine("No items found.");
            return;
        }

        var columnWidths = new Dictionary<string, int>();
        var itemValues = new List<Dictionary<string, string>>();

        foreach (var column in columns)
        {
            columnWidths[column] = column.Length;
        }

        foreach (var item in items)
        {
            var values = new Dictionary<string, string>();
            var columnValues = GetItemValues(item, columns);

            for (int i = 0; i < columns.Length; i++)
            {
                values[columns[i]] = columnValues[i];
                columnWidths[columns[i]] = Math.Max(columnWidths[columns[i]], columnValues[i].Length);
            }

            itemValues.Add(values);
        }

        var sb = new StringBuilder();

        foreach (var column in columns)
        {
            sb.Append(column.PadRight(columnWidths[column] + 2));
        }
        Console.WriteLine(sb.ToString());

        sb.Clear();
        foreach (var column in columns)
        {
            sb.Append(new string('-', columnWidths[column]));
            sb.Append("  ");
        }
        Console.WriteLine(sb.ToString());

        foreach (var values in itemValues)
        {
            sb.Clear();
            foreach (var column in columns)
            {
                var value = values[column];

                if (value.Length > columnWidths[column])
                {
                    value = value[..(columnWidths[column] - 3)] + "...";
                }

                sb.Append(value.PadRight(columnWidths[column] + 2));
            }
            Console.WriteLine(sb.ToString());
        }
    }

    private static string[] GetItemValues<T>(T item, string[] columns)
    {
        var values = new string[columns.Length];

        if (item == null)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                values[i] = "";
            }
            return values;
        }

        for (int i = 0; i < columns.Length; i++)
        {
            var column = columns[i];
            var value = column switch
            {
                "WebinarKey" when item is Models.Webinar w => w.WebinarKey,
                "Subject" when item is Models.Webinar w => w.Subject,
                "TimeZone" when item is Models.Webinar w => w.TimeZone,
                "InSession" when item is Models.Webinar w => w.InSession.ToString(),
                "RegistrantKey" when item is Models.Registrant r => r.RegistrantKey,
                "FirstName" when item is Models.Registrant r => r.FirstName,
                "LastName" when item is Models.Registrant r => r.LastName,
                "Email" when item is Models.Registrant r => r.Email,
                "Status" when item is Models.Registrant r => r.Status,
                _ => ""
            };

            values[i] = value ?? "";
        }

        return values;
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    public static OutputFormat ParseFormat(string? format)
    {
        if (string.IsNullOrEmpty(format))
            return OutputFormat.Table;

        return format.ToLowerInvariant() switch
        {
            "json" => OutputFormat.Json,
            "csv" => OutputFormat.Csv,
            _ => OutputFormat.Table
        };
    }
}
