using System.Drawing;
using System.Windows.Forms;

namespace Botanika_Desktop.Theme
{
    // One-stop shop for applying the Botanika look to any form or control tree.
    // Call BotanikaTheme.Apply(this) at the end of any form's constructor.
    public static class BotanikaTheme
    {
        // Walks the entire control tree and applies our theme to everything
        public static void Apply(Control root)
        {
            // Set the root form/panel background
            if (root is Form form)
            {
                form.BackColor = BotanikaColors.Offwhite;
                form.Font      = BotanikaFonts.Body(9.5f);
            }

            ApplyToControl(root);
        }

        // Recursively themes every control in the hierarchy
        private static void ApplyToControl(Control ctrl)
        {
            switch (ctrl)
            {
                case Button btn:
                    StyleButton(btn);
                    break;

                case TextBox txt:
                    StyleTextBox(txt);
                    break;

                case ComboBox cmb:
                    StyleComboBox(cmb);
                    break;

                case Label lbl:
                    // Don't touch labels that have already been styled manually
                    if (lbl.ForeColor == SystemColors.ControlText)
                        lbl.ForeColor = BotanikaColors.Charcoal;
                    break;

                case Panel pnl:
                    // Content panels stay offwhite; sidebar panels are styled separately
                    if (pnl.BackColor == SystemColors.Control)
                        pnl.BackColor = BotanikaColors.Offwhite;
                    break;
            }

            // Recurse into children
            foreach (Control child in ctrl.Controls)
                ApplyToControl(child);
        }

        // Botanika button style — green, rounded feel, white text
        public static void StyleButton(Button btn)
        {
            btn.BackColor   = BotanikaColors.Primary;
            btn.ForeColor   = BotanikaColors.White;
            btn.FlatStyle   = FlatStyle.Flat;
            btn.Font        = BotanikaFonts.Body(9.5f, FontStyle.Regular);
            btn.Cursor      = Cursors.Hand;
            btn.FlatAppearance.BorderSize  = 0;
            btn.FlatAppearance.MouseOverBackColor  = BotanikaColors.PrimaryDark;
            btn.FlatAppearance.MouseDownBackColor  = BotanikaColors.PrimaryDark;
            // Apply rounded corners once the button has been laid out
            btn.HandleCreated += (s, e) => ApplyRoundedCorners(btn, 8);
            btn.SizeChanged  += (s, e) => ApplyRoundedCorners(btn, 8);
        }

        // Danger / delete button — terracotta red
        public static void StyleDangerButton(Button btn)
        {
            StyleButton(btn);
            btn.BackColor = BotanikaColors.Terracotta;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(160, 90, 50);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(140, 70, 35);
        }

        // Secondary / outlined button style
        public static void StyleSecondaryButton(Button btn)
        {
            btn.BackColor   = BotanikaColors.SandLight;
            btn.ForeColor   = BotanikaColors.Charcoal;
            btn.FlatStyle   = FlatStyle.Flat;
            btn.Font        = BotanikaFonts.Body(9.5f, FontStyle.Bold);
            btn.Cursor      = Cursors.Hand;
            btn.FlatAppearance.BorderColor = BotanikaColors.SandLight;
            btn.FlatAppearance.BorderSize  = 0;
            btn.FlatAppearance.MouseOverBackColor = BotanikaColors.Sand;
            btn.HandleCreated += (s, e) => ApplyRoundedCorners(btn, 12);
            btn.SizeChanged   += (s, e) => ApplyRoundedCorners(btn, 12);
        }

        // Clean input box — borderline on bottom only feel isn't easy in WinForms,
        // so we settle for a subtle border and clean white background
        // Modern, borderless input with rounded edges
        public static void StyleTextBox(TextBox txt)
        {
            txt.BackColor  = BotanikaColors.White;
            txt.ForeColor  = BotanikaColors.Charcoal;
            txt.BorderStyle = BorderStyle.None;
            txt.Font       = BotanikaFonts.Body(10f);
            txt.AutoSize   = false;
            txt.Height     = 30;
            txt.HandleCreated += (s, e) => ApplyRoundedCorners(txt, 8);
            txt.SizeChanged   += (s, e) => ApplyRoundedCorners(txt, 8);
        }

        // ComboBox to match our modern text boxes
        public static void StyleComboBox(ComboBox cmb)
        {
            cmb.BackColor  = BotanikaColors.White;
            cmb.ForeColor  = BotanikaColors.Charcoal;
            cmb.FlatStyle  = FlatStyle.Flat;
            cmb.Font       = BotanikaFonts.Body(10f);
            cmb.HandleCreated += (s, e) => ApplyRoundedCorners(cmb, 8);
            cmb.SizeChanged   += (s, e) => ApplyRoundedCorners(cmb, 8);
        }

        // Applies rounded corners to all card-like white panels in a control tree.
        // Call after BuildUI to round every panel that looks like a card.
        public static void RoundAllCards(Control root, int radius = 10)
        {
            RoundCardsRecursive(root, radius);
        }

        private static void RoundCardsRecursive(Control ctrl, int radius)
        {
            // Round any solid-background panel that looks like a card
            if (ctrl is Panel pnl && pnl.Width > 40 && pnl.Height > 40
                && (pnl.BackColor == BotanikaColors.White
                    || pnl.BackColor == BotanikaColors.SandLight
                    || pnl.BackColor == BotanikaColors.Offwhite)
                && !(pnl is FlowLayoutPanel))  // skip flow panels
            {
                ApplyRoundedCorners(pnl, radius);
                pnl.SizeChanged += (s, e) => ApplyRoundedCorners(pnl, radius);
            }

            // Also round ListViews
            if (ctrl is ListView lv && lv.Width > 40 && lv.Height > 40)
            {
                ApplyRoundedCorners(lv, radius);
                lv.SizeChanged += (s, e) => ApplyRoundedCorners(lv, radius);
            }

            foreach (Control child in ctrl.Controls)
                RoundCardsRecursive(child, radius);
        }

        // Clips a control to a rounded rectangle — same effect as CSS border-radius.
        // Call this on any Panel/GroupBox/card to get website-style rounded corners.
        public static void ApplyRoundedCorners(Control ctrl, int radius = 12)
        {
            if (ctrl == null || ctrl.Width <= 0 || ctrl.Height <= 0) return;

            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radius * 2;
            int w = ctrl.Width;
            int h = ctrl.Height;

            path.AddArc(0,     0,     d, d, 180, 90);
            path.AddArc(w - d, 0,     d, d, 270, 90);
            path.AddArc(w - d, h - d, d, d,   0, 90);
            path.AddArc(0,     h - d, d, d,  90, 90);
            path.CloseAllFigures();

            ctrl.Region = new System.Drawing.Region(path);
        }
    }
}
