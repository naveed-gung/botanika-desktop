using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Botanika_Desktop.Theme;

namespace Botanika_Desktop.Controls
{
    // A themed button that matches the Botanika website's button style.
    // Has smooth hover/press color transitions and optional icon support.
    public class BotanikaButton : Button
    {
        // Current background color — we animate this for hover effect
        private Color _currentBackColor;
        private bool  _isHovered;
        private bool  _isPressed;

        // Corner radius — 6px matches the website's border-radius
        private const int CornerRadius = 6;

        public BotanikaButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font      = BotanikaFonts.Body(9.5f);
            ForeColor = BotanikaColors.White;
            BackColor = BotanikaColors.Primary;
            Cursor    = Cursors.Hand;
            Size      = new Size(120, 36);

            _currentBackColor = BotanikaColors.Primary;
        }

        // Override paint so we can draw rounded corners
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Color bg = _isPressed ? BotanikaColors.PrimaryDark
                     : _isHovered ? BotanikaColors.PrimaryDark
                     : BackColor;

            using (var path = RoundedRect(ClientRectangle, CornerRadius))
            using (var brush = new SolidBrush(bg))
            {
                e.Graphics.FillPath(brush, path);
            }

            // Draw button text centered
            TextRenderer.DrawText(
                e.Graphics, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            _isPressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _isPressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _isPressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        // Helper that builds a rounded rectangle GraphicsPath
        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
