using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Botanika_Desktop.Export
{
    // Plain CSV export — no dependencies, always works.
    // Values are quoted and inner quotes are escaped — RFC 4180 compliant enough for Excel.
    public static class CsvExporter
    {
        public static void Export<T>(List<T> data, string filePath)
        {
            var props = typeof(T).GetProperties()
                .Where(p => p.CanRead)
                .ToArray();

            var lines = new List<string>();

            // Header row — property names become column headers
            lines.Add(string.Join(",", props.Select(p => QuoteCsvField(p.Name))));

            // Data rows — one line per object
            foreach (var item in data)
            {
                lines.Add(string.Join(",", props.Select(p =>
                    QuoteCsvField(p.GetValue(item)?.ToString() ?? ""))));
            }

            File.WriteAllLines(filePath, lines, Encoding.UTF8);
        }

        // Wraps a value in quotes and escapes any inner quotes by doubling them
        private static string QuoteCsvField(string value)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
