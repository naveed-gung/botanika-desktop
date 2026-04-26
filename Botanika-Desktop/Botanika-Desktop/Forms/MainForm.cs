using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Botanika_Desktop.Controls;
using Botanika_Desktop.Firebase;
using Botanika_Desktop.Firebase.Models;
using Botanika_Desktop.Theme;

namespace Botanika_Desktop.Forms
{
    // The main application shell — sidebar on the left, content area on the right.
    // All other panels are swapped into the content area when the user navigates.
    public class MainForm : Form
    {
        // ─── Layout panels ─────────────────────────────────────────────────────
        private Panel _sidebar;
        private Panel _contentPanel;
        private Label _userLabel;
        private Label _statusDot;

        // All sidebar navigation items — we keep references so we can set IsActive
        private List<SidebarItem> _navItems = new List<SidebarItem>();

        // The panel that's currently displayed in the content area
        private UserControl _currentPanel;

        public MainForm()
        {
            InitializeComponent();
            SetupKeyboardShortcuts();

            // Start on the Dashboard by default
            Navigate("Dashboard");

            // Check for low-stock products shortly after load
            this.Shown += async (s, e) => await CheckLowStockAsync();
        }

        private void InitializeComponent()
        {
            Text = "Botanika Admin";
            Size = new Size(1280, 800);
            MinimumSize = new Size(1000, 640);
            BackColor = BotanikaColors.Offwhite;
            StartPosition = FormStartPosition.CenterScreen;

            // Set the custom Botanika icon — replaces the generic VS icon in taskbar + titlebar
            if (Program.AppIcon != null) this.Icon = Program.AppIcon;

            // ── Sidebar ────────────────────────────────────────────────────────
            _sidebar = new Panel
            {
                Size = new Size(220, ClientSize.Height),
                Dock = DockStyle.Left,
                BackColor = BotanikaColors.Charcoal
            };

            BuildSidebar();

            // ── Content area ───────────────────────────────────────────────────
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BotanikaColors.Offwhite,
                Padding = new Padding(0)
            };

            Controls.Add(_contentPanel);
            Controls.Add(_sidebar);  // add sidebar last so it renders on top of content edge
        }

        // Builds all the sidebar contents — logo, nav items, logout button
        private void BuildSidebar()
        {
            int yPos = 0;

            // ── Logo area ──────────────────────────────────────────────────────
            var logoPanel = new Panel
            {
                Size = new Size(220, 80),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(30, 0, 0, 0) // slightly darker than sidebar
            };
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
            if (!System.IO.File.Exists(iconPath)) iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Assets", "logo.png");
            var pb = new PictureBox { Image = System.IO.File.Exists(iconPath) ? Image.FromFile(iconPath) : null, SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(24, 24), Location = new Point(16, 16) };
            logoPanel.Controls.Add(pb);

            var logoLabel = new Label
            {
                Text = "BOTANIKA",
                Font = BotanikaFonts.Heading(14f, FontStyle.Bold),
                ForeColor = BotanikaColors.Primary,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(130, 40),
                Location = new Point(46, 8)
            };
            var adminBadge = new Label
            {
                Text = "ADMIN",
                Font = BotanikaFonts.Caption(7f, FontStyle.Bold),
                ForeColor = BotanikaColors.Primary,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(100, 16),
                Location = new Point(16, 48)
            };
            logoPanel.Controls.Add(logoLabel);
            logoPanel.Controls.Add(adminBadge);
            _sidebar.Controls.Add(logoPanel);
            yPos = 80;

            // ── Logged-in user display ─────────────────────────────────────────
            _userLabel = new Label
            {
                Text = Session.DisplayName,
                Font = BotanikaFonts.Body(9f),
                ForeColor = Color.FromArgb(180, 255, 255, 255),
                Size = new Size(220, 32),
                Location = new Point(0, yPos),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };
            _sidebar.Controls.Add(_userLabel);
            yPos += 32;

            // ── Thin separator ─────────────────────────────────────────────────
            var sep = new Panel
            {
                Size = new Size(180, 1),
                Location = new Point(20, yPos + 4),
                BackColor = Color.FromArgb(40, 255, 255, 255)
            };
            _sidebar.Controls.Add(sep);
            yPos += 16;

            // ── Navigation items ───────────────────────────────────────────────
            // The order here matches the spec — most important sections first
            var navDefs = new[]
            {
                ("dashboard", "Dashboard",  "Dashboard"),
                ("leaf", "Products",   "Products"),
                ("clients", "Clients",    "Clients"),
                ("suppliers", "Suppliers",  "Suppliers"),
                ("orders", "Orders",     "Orders"),
                ("revenue", "Revenue",    "Revenue"),
                ("payments", "Payments",   "Payments"),
                ("chatbot", "Chatbot", "Chatbot"),
            };

            foreach (var (icon, text, section) in navDefs)
            {
                var item = new SidebarItem(icon, text, section)
                {
                    Location = new Point(0, yPos)
                };
                item.NavigateClicked += (s, e) => Navigate(((SidebarItem)s).SectionName);
                _navItems.Add(item);
                _sidebar.Controls.Add(item);
                yPos += 44;
            }

            // ── Live sync indicator ────────────────────────────────────────────
            yPos += 8;
            _statusDot = new Label
            {
                Text = "● Live",
                Font = BotanikaFonts.Caption(8f),
                ForeColor = BotanikaColors.Success,
                Size = new Size(220, 20),
                Location = new Point(0, yPos),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _sidebar.Controls.Add(_statusDot);

            // ── Bottom social icons — horizontal row, icon-only ──────────────
            var socialPanel = new FlowLayoutPanel
            {
                Size = new Size(180, 38),
                Location = new Point(20, _sidebar.Height - 115),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.Transparent,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight
            };

            var socialDefs = new[]
            {
                ("\ud83c\udf10", "https://botanika-754.netlify.app",  "Botanika Website"),
                ("in",  "https://www.linkedin.com/in/naveed-sohail-gung-285645310/", "LinkedIn"),
                ("\u2328",  "https://github.com/naveed-gung/", "GitHub"),
                ("\u25c6",  "https://naveed-gung.dev/",          "Portfolio"),
            };

            foreach (var (icon, url, tip) in socialDefs)
            {
                var btn = new Button
                {
                    Text = icon,
                    Size = new Size(36, 36),
                    Margin = new Padding(0, 0, 6, 0),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(40, 255, 255, 255),
                    ForeColor = Color.FromArgb(200, 255, 255, 255),
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 255, 255, 255);
                var toolTip = new ToolTip();
                toolTip.SetToolTip(btn, tip);
                string target = url;
                btn.Click += (s, e) => System.Diagnostics.Process.Start(target);
                socialPanel.Controls.Add(btn);
            }
            _sidebar.Controls.Add(socialPanel);

            // ── Logout button at the very bottom ───────────────────────────────
            var logoutBtn = new Button
            {
                Text = "Sign Out",
                Size = new Size(180, 36),
                Location = new Point(20, _sidebar.Height - 56),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 255, 255, 255),
                ForeColor = Color.White,
                Font = BotanikaFonts.Body(9.5f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            logoutBtn.FlatAppearance.BorderSize = 0;
            logoutBtn.Click += Logout_Click;
            _sidebar.Controls.Add(logoutBtn);
        }

        // ─── Navigation ────────────────────────────────────────────────────────

        // Swaps the content panel to the requested section.
        // This is the central routing function for the whole app.
        public void Navigate(string sectionName)
        {
            // Update active state on all sidebar items
            foreach (var item in _navItems)
                item.IsActive = item.SectionName == sectionName;

            // Create the appropriate panel for this section
            UserControl panel;
            switch (sectionName)
            {
                case "Dashboard": panel = new DashboardPanel(); break;
                case "Products": panel = new ProductsPanel(); break;
                case "Clients": panel = new ClientsPanel(); break;
                case "Suppliers": panel = new SuppliersPanel(); break;
                case "Orders": panel = new OrdersPanel(); break;
                case "Revenue": panel = new RevenuePanel(); break;
                case "Payments": panel = new PaymentsPanel(); break;
                case "Chatbot": panel = new ChatbotPanel(); break;
                default: panel = new DashboardPanel(); break;
            }

            // Swap in the new panel
            _contentPanel.Controls.Clear();
            _currentPanel?.Dispose();  // clean up the old panel
            _currentPanel = panel;
            panel.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(panel);
            panel.BringToFront();
        }

        // ─── Keyboard Shortcuts ────────────────────────────────────────────────

        // Sets up the global keyboard shortcuts listed in the spec
        private void SetupKeyboardShortcuts()
        {
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // F5 — refresh the current panel if it supports it
            if (e.KeyCode == Keys.F5)
            {
                (_currentPanel as IRefreshable)?.Refresh();
                e.Handled = true;
            }

            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.N: (_currentPanel as ICrudPanel)?.AddNew(); e.Handled = true; break;
                    case Keys.E: (_currentPanel as ICrudPanel)?.EditSelected(); e.Handled = true; break;
                    case Keys.F: (_currentPanel as ISearchable)?.FocusSearch(); e.Handled = true; break;
                }

                // Ctrl+Shift+E — export
                if (e.Shift && e.KeyCode == Keys.E)
                {
                    (_currentPanel as IExportable)?.ExportData();
                    e.Handled = true;
                }
            }

            // Delete key — delete selected item
            if (e.KeyCode == Keys.Delete)
            {
                (_currentPanel as ICrudPanel)?.DeleteSelected();
                e.Handled = false; // allow default handling too
            }
        }

        // ─── Low Stock Alerts ──────────────────────────────────────────────────

        // On startup, warn about any products with 5 or fewer units in stock
        private async System.Threading.Tasks.Task CheckLowStockAsync()
        {
            try
            {
                var products = await FirebaseService.Instance.GetAllAsync<Product>("products");
                var lowStock = products.FindAll(p => p.Stock <= 5 && p.Stock > 0);
                var outOfStock = products.FindAll(p => p.Stock <= 0);

                if (outOfStock.Count > 0)
                    ToastNotification.ShowError($"⚠ {outOfStock.Count} product(s) are out of stock!");

                if (lowStock.Count > 0)
                    ToastNotification.Show($"Low stock alert: {lowStock.Count} product(s) running low.");
            }
            catch
            {
                // Don't crash the app if the stock check fails — it's just a notification
            }
        }

        // ─── Logout ────────────────────────────────────────────────────────────

        private void Logout_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "Sign out of Botanika Admin?", "Confirm Sign Out",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            // Clear the session
            FirebaseService.Instance.SignOut();
            Session.Clear();

            // Signal the hidden LoginForm to show itself again
            this.DialogResult = DialogResult.Retry;
            this.Close();
        }
    }

    // ─── Panel capability interfaces ───────────────────────────────────────────
    // These let the MainForm call panel-specific actions without knowing their exact type.

    public interface IRefreshable { void Refresh(); }
    public interface ICrudPanel { void AddNew(); void EditSelected(); void DeleteSelected(); }
    public interface ISearchable { void FocusSearch(); }
    public interface IExportable { void ExportData(); }
}
