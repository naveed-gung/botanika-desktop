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
            Font          = BotanikaFonts.Body(9.5f);
            BackColor     = BotanikaColors.White;
            BorderStyle   = BorderStyle.None;
            ShowItemToolTips = true;

            // Use native double buffering to prevent hover glitches
            // (WinForms SetStyle causes the text to disappear on hover)
        }

        protected override void OnHandleCreated(System.EventArgs e)
        {
            base.OnHandleCreated(e);
            // LVM_SETEXTENDEDLISTVIEWSTYLE = 0x1036, LVS_EX_DOUBLEBUFFER = 0x10000
            System.Windows.Forms.Message m = System.Windows.Forms.Message.Create(Handle, 0x1036, (System.IntPtr)0x10000, (System.IntPtr)0x10000);
            DefWndProc(ref m);
        }

        protected override void OnResize(System.EventArgs e)
        {
            base.OnResize(e);
            AdjustColumns();
        }

        public void AdjustColumns()
        {
            if (Columns.Count == 0) return;
            
            int fixedWidth = 0;
            // Sum all columns except the first one
            for (int i = 1; i < Columns.Count; i++)
            {
                fixedWidth += Columns[i].Width;
            }
            
            int newWidth = ClientSize.Width - fixedWidth - SystemInformation.VerticalScrollBarWidth - 2;
            if (newWidth > 100)
            {
                Columns[0].Width = newWidth;
            }
        }

        // Auto-size the list to fit its content — no endless whitespace
        public void AutoFitHeight(int minHeight = 80, int maxHeight = 600)
        {
            AdjustColumns();
            if (Items.Count == 0)
            {
                Height = minHeight;
                return;
            }

            // Each row is roughly 24px, header is ~28px
            int contentHeight = 28 + (Items.Count * 24) + 4;
            Height = System.Math.Max(minHeight, System.Math.Min(contentHeight, maxHeight));
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
                BotanikaFonts.Body(9f, FontStyle.Bold),
                Rectangle.Inflate(e.Bounds, -4, 0),
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
                BotanikaFonts.Body(9.5f),
                Rectangle.Inflate(e.Bounds, -4, 0),
                fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }
}
