using System.Drawing;

namespace Botanika_Desktop.Theme
{
    // All the colors straight from the Botanika website CSS variables.
    // If you ever tweak the website palette, update these to match!
    public static class BotanikaColors
    {
        // The signature sage-green — used for buttons, highlights, active states
        public static readonly Color Primary      = ColorTranslator.FromHtml("#8A9A5B");

        // A darker shade of the primary, nice for hover states
        public static readonly Color PrimaryDark  = ColorTranslator.FromHtml("#6F7D48");

        // Lighter variant — good for subtle highlights
        public static readonly Color PrimaryLight = ColorTranslator.FromHtml("#9DAD6E");

        // Deep charcoal green — sidebar background, headings
        public static readonly Color Charcoal     = ColorTranslator.FromHtml("#2C3E2F");

        // Warm almost-white — main content backgrounds
        public static readonly Color Offwhite     = ColorTranslator.FromHtml("#F9F7F3");

        // Warm sand — cards, alternating table rows (heavier)
        public static readonly Color Sand         = ColorTranslator.FromHtml("#E6DFD3");

        // Lighter sand — alternating table rows (light version)
        public static readonly Color SandLight    = ColorTranslator.FromHtml("#F0EBE3");

        // Terracotta accent — used for errors, warnings, delete actions
        public static readonly Color Terracotta   = ColorTranslator.FromHtml("#C17C54");

        // Muted grey text — captions, placeholders, disabled labels
        public static readonly Color TextMuted    = ColorTranslator.FromHtml("#9A9A9A");

        // Slightly darker muted — secondary body text
        public static readonly Color TextLight    = ColorTranslator.FromHtml("#6B6B6B");

        // Semi-transparent border color (rgba #8A9A5B at 20% opacity)
        public static readonly Color Border       = Color.FromArgb(51, 138, 154, 91);

        // Plain white — text on dark backgrounds
        public static readonly Color White        = Color.White;

        // Success green — payment "paid" rows
        public static readonly Color Success      = ColorTranslator.FromHtml("#4CAF50");

        // Warning amber — payment "pending" rows
        public static readonly Color Warning      = ColorTranslator.FromHtml("#FFC107");

        // Danger red — payment "overdue" rows
        public static readonly Color Danger       = ColorTranslator.FromHtml("#F44336");
    }
}
