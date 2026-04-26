using System;
using System.Drawing;
using System.Windows.Forms;
using Botanika_Desktop.Theme;

namespace Botanika_Desktop.Controls
{
    // Non-intrusive toast that pops up at the bottom-right of the screen
    // for 3 seconds then disappears. Green for success, terracotta for errors.
    public class ToastNotification : Form
    {
        // Show a success toast — the most common case
        public static void Show(string message)
        {
            ShowToast(message, isError: false);
        }

        // Show an error toast — red/terracotta background
        public static void ShowError(string message)
        {
            ShowToast(message, isError: true);
        }

        // Internal factory — creates, shows, and auto-closes the toast
        private static void ShowToast(string message, bool isError)
        {
            var toast = new ToastNotification(message, isError);
            toast.Show();

            // Auto-dismiss after 3 seconds
            var timer = new Timer { Interval = 3000 };
            timer.Tick += (_, __) =>
            {
                timer.Stop();
                timer.Dispose();
                if (!toast.IsDisposed)
                    toast.Close();
            };
            timer.Start();
        }

        // ─── Constructor ───────────────────────────────────────────────────────

        private ToastNotification(string message, bool isError)
        {
            // Frameless window — we draw our own rounded background
            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.Manual;
            Size            = new Size(320, 56);
            BackColor       = isError ? BotanikaColors.Terracotta : BotanikaColors.Primary;
            TopMost         = true;  // always visible even if another window has focus
            ShowInTaskbar   = false; // don't clutter the taskbar

            // Position at the bottom-right corner of the working screen
            var screen = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(screen.Right - Width - 24, screen.Bottom - Height - 24);

            // Icon label — checkmark or X depending on toast type
            var icon = new Label
            {
                Text      = isError ? "✗" : "✓",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                Size      = new Size(48, 56),
                Location  = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // The actual message text
            var label = new Label
            {
                Text      = message,
                ForeColor = Color.White,
                Font      = BotanikaFonts.Body(10f),
                Size      = new Size(260, 56),
                Location  = new Point(48, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Controls.Add(icon);
            Controls.Add(label);

            // Clicking the toast dismisses it early
            Click       += (_, __) => Close();
            icon.Click  += (_, __) => Close();
            label.Click += (_, __) => Close();
        }
    }
}
