using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Botanika_Desktop.Export
{
    // Handles importing data back from CSV or Excel files.
    // Returns a list of row dictionaries — callers decide what to do with them.
    public static class ImportHandler
    {
        // Parses a CSV file into a list of dictionaries (column → value).
        // The first row must be the header row.
        public static List<Dictionary<string, string>> ImportFromCsv(string filePath)
        {
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            if (lines.Length < 2)
                return new List<Dictionary<string, string>>();

            var headers = ParseCsvLine(lines[0]);
            var result  = new List<Dictionary<string, string>>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var values = ParseCsvLine(lines[i]);
                var row    = new Dictionary<string, string>();

                for (int j = 0; j < headers.Count && j < values.Count; j++)
                    row[headers[j]] = values[j];

                result.Add(row);
            }

            return result;
        }

        // Parses one CSV line into a list of field values,
        // handling quoted fields with embedded commas or newlines.
        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var sb     = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote inside a quoted field — add one quote
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // Field boundary — save current field and start fresh
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            // Don't forget the last field
            fields.Add(sb.ToString());
            return fields;
        }
    }
}
