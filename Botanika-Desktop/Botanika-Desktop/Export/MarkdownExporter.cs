using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Botanika_Desktop.Export
{
    // Markdown table export — clean and readable, great for README files or docs.
    // Opens nicely in any markdown viewer or GitHub.
    public static class MarkdownExporter
    {
        public static void Export<T>(List<T> data, string filePath, string title)
        {
            var props = typeof(T).GetProperties()
                .Where(p => p.CanRead)
                .ToArray();

            var sb = new StringBuilder();

            // Document title and timestamp
            sb.AppendLine($"# {title}");
            sb.AppendLine($"_Exported: {DateTime.Now:yyyy-MM-dd HH:mm}_");
            sb.AppendLine();
            sb.AppendLine($"**Total records:** {data.Count}");
            sb.AppendLine();

            // Column header row
            sb.AppendLine("| " + string.Join(" | ", props.Select(p => p.Name)) + " |");

            // Separator row — three dashes per column (markdown table spec)
            sb.AppendLine("| " + string.Join(" | ", props.Select(_ => "---")) + " |");

            // One data row per item — pipe characters in values must be escaped
            foreach (var item in data)
            {
                var values = props.Select(p =>
                    (p.GetValue(item)?.ToString() ?? "").Replace("|", "\\|"));
                sb.AppendLine("| " + string.Join(" | ", values) + " |");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}
