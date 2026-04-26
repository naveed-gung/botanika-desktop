using System.Drawing;

namespace Botanika_Desktop.Theme
{
    // Font helper — we try to use Inter/DM Sans (same as the website),
    // but gracefully fall back to Segoe UI if they aren't installed on the machine.
    // For a proper submission you'd embed the .ttf files as resources.
    public static class BotanikaFonts
    {
        // Headings use Inter — matches the website's heading font
        public static Font Heading(float size, FontStyle style = FontStyle.Regular)
        {
            return TryFont("Inter", size, style)
                ?? new Font("Segoe UI", size, style);
        }

        // Body text uses DM Sans, falling back to Segoe UI
        public static Font Body(float size, FontStyle style = FontStyle.Regular)
        {
            return TryFont("DM Sans", size, style)
                ?? new Font("Segoe UI", size, style);
        }

        // Small utility labels, captions, hints
        public static Font Caption(float size = 8f, FontStyle style = FontStyle.Regular)
        {
            return Body(size, style);
        }

        // Tries to create a font by name — returns null if it's not installed
        // so the callers can fall back gracefully without crashing
        private static Font TryFont(string name, float size, FontStyle style)
        {
            try
            {
                var f = new Font(name, size, style);
                // If GDI silently substituted a different font, name won't match
                if (f.Name == name) return f;
                f.Dispose();
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
