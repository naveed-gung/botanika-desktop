using System.Drawing;
using System.Windows.Forms;
using Botanika_Desktop.Theme;

namespace Botanika_Desktop.Controls
{
    // A custom styled ListView that replaces the default WinForms ugly grid.
    // Handles alternating row colors, themed headers, proper selection highlight,
    // and auto-resizing to fit content (no endless empty whitespace).
    public class BotanikaListView : ListView
    {
        public BotanikaListView()
        {
            // These settings are needed for owner-draw to work properly
            FullRowSelect = true;
            GridLines     = false;
            View          = View.Details;
            OwnerDraw     = true;  // we draw everything ourselves
            Font          = BotanikaFonts.Body(11.5f);
            BackColor     = BotanikaColors.White;
            BorderStyle   = BorderStyle.None;
            ShowItemToolTips = true;

            // Use native double buffering to prevent hover glitches
            // (WinForms SetStyle causes the text to disappear on hover)
        }

        private bool _initialized;
        protected override void OnHandleCreated(System.EventArgs e)
        {
            base.OnHandleCreated(e);
            
            // Enable native double buffering properly using Reflection
            System.Reflection.PropertyInfo aProp = typeof(Control).GetProperty("DoubleBuffered", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            aProp?.SetValue(this, true, null);
            
            if (!_initialized)
            {
                _initialized = true;
                // Move down slightly
                Top += 25;
                // Scale columns by 30% to make the table wider and more spacious
                foreach (ColumnHeader col in Columns)
                {
                    if (col.Width > 0) col.Width = (int)(col.Width * 1.35);
                }
            }
        }

        protected override void OnResize(System.EventArgs e)
        {
            base.OnResize(e);
            AdjustColumns();
        }

        public void AdjustColumns()
        {
            // Removed proportional scaling to keep columns tight.
            // We now rely on AutoFitWidth to shrink the table itself.
        }

        // Auto-size the list to fit its content — no endless whitespace
        public void AutoFitHeight(int minHeight = 80, int maxHeight = 600)
        {
            if (Items.Count == 0)
            {
                Height = minHeight;
                return;
            }

            // Set row height using a dummy image list (taller rows)
            var dummyImageList = new ImageList { ImageSize = new Size(1, 36) };
            SmallImageList = dummyImageList;
            
            // Each row is ~36px, header is ~40px
            int contentHeight = 40 + (Items.Count * 36) + 4;
            Height = System.Math.Max(minHeight, System.Math.Min(contentHeight, maxHeight));
            
            // Also fit width to columns to prevent the gray infinite space
            int totalW = 0;
            foreach (ColumnHeader col in Columns) totalW += col.Width;
            if (totalW > 0)
            {
                // Unhook right anchor so it can shrink
                Anchor = AnchorStyles.Top | AnchorStyles.Left;
                Width = totalW + 4; 
            }
        }

        // Draw the column header row — dark charcoal background with white text
        protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
        {
            // Dark green header background — matches the website's table headers
            using (var brush = new SolidBrush(BotanikaColors.Charcoal))
                e.Graphics.FillRectangle(brush, e.Bounds);

            // Add a subtle right border between columns
            using (var borderPen = new Pen(Color.FromArgb(80, 255, 255, 255)))
                e.Graphics.DrawLine(borderPen,
                    e.Bounds.Right - 1, e.Bounds.Top,
                    e.Bounds.Right - 1, e.Bounds.Bottom);

            // Draw the header text
            TextRenderer.DrawText(
                e.Graphics,
                e.Header?.Text ?? "",
                BotanikaFonts.Body(11f, FontStyle.Bold),
                Rectangle.Inflate(e.Bounds, -10, 0),
                Color.White,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        // Draw the main item (first sub-item) — controls background color
        protected override void OnDrawItem(DrawListViewItemEventArgs e)
        {
            // Selected row gets the primary green, otherwise alternate between white and sand
            Color bg = e.Item.Selected
                ? BotanikaColors.Primary
                : (e.ItemIndex % 2 == 0 ? BotanikaColors.White : BotanikaColors.SandLight);

            using (var brush = new SolidBrush(bg))
                e.Graphics.FillRectangle(brush, e.Bounds);
        }

        // Draw each cell's text content
        protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
        {
            // First, fill the cell background to prevent black artifacts
            Color bg = e.Item.Selected
                ? BotanikaColors.Primary
                : (e.ItemIndex % 2 == 0 ? BotanikaColors.White : BotanikaColors.SandLight);
            using (var brush = new SolidBrush(bg))
                e.Graphics.FillRectangle(brush, e.Bounds);

            // White text when selected, charcoal otherwise
            Color fg = e.Item.Selected ? Color.White : BotanikaColors.Charcoal;

            TextRenderer.DrawText(
                e.Graphics,
                e.SubItem?.Text ?? "",
                BotanikaFonts.Body(11.5f),
                Rectangle.Inflate(e.Bounds, -10, 0),
                fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }
}
