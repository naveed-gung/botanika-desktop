using System;
using System.Windows.Forms;
using Botanika_Desktop.Forms;

namespace Botanika_Desktop
{
    internal static class Program
    {
        // Entry point — fire up the login form first.
        // Everything else flows from there: successful login opens MainForm,
        // which in turn hosts all the management panels.
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

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
