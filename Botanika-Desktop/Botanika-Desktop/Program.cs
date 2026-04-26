using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Botanika_Desktop.Forms;

namespace Botanika_Desktop
{
    internal static class Program
    {
        // The shared app icon — loaded once, reused by all forms.
        public static Icon AppIcon { get; private set; }

        // Entry point — fire up the login form first.
        // Everything else flows from there: successful login opens MainForm,
        // which in turn hosts all the management panels.
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Load the Botanika icon from Assets — replaces the generic VS icon everywhere
            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.ico");
            if (!File.Exists(icoPath))
                icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Assets", "logo.ico");
            if (File.Exists(icoPath))
            {
                try { AppIcon = new Icon(icoPath); }
                catch { /* fall back to default */ }
            }

            // Global exception handler — catch any unhandled exceptions and show a friendly message
            // instead of crashing silently or showing a raw stack trace to the admin.
            Application.ThreadException += (sender, e) =>
            {
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{e.Exception.Message}\n\n" +
                    "Please restart the application. If this keeps happening, check your Firebase connection.",
                    "Botanika Desktop — Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            Application.Run(new LoginForm());
        }
    }
}
