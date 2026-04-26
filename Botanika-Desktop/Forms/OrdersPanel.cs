using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Botanika_Desktop.Controls;
using Botanika_Desktop.Export;
using Botanika_Desktop.Firebase;
using Botanika_Desktop.Firebase.Models;
using Botanika_Desktop.Theme;

namespace Botanika_Desktop.Forms
{
    // Orders panel — read-only view of all orders placed on the website.
    // You can update the order status here (confirmed → shipped → delivered).
    // Orders are created by customers on the web, not in this app.
    public class OrdersPanel : UserControl, IRefreshable, ISearchable, IExportable
    {
        private BotanikaListView _listView;
        private TextBox          _searchBox;
        private ComboBox         _statusFilter;
        private Label            _countLabel;
        private List<Order>      _allOrders = new List<Order>();
        private List<Order>      _filtered  = new List<Order>();

        public OrdersPanel()
        {
            BackColor = BotanikaColors.Offwhite;
            Dock      = DockStyle.Fill;
            BuildUI();
            BotanikaTheme.RoundAllCards(this);
            _ = RefreshListAsync();
        }

        private void BuildUI()
        {
            int pad = 24;

            var header = new Label
            {
                Text      = "Orders",
                Font      = BotanikaFonts.Heading(18f, FontStyle.Bold),
                ForeColor = BotanikaColors.Charcoal,
                Location  = new Point(pad, 16),
                AutoSize  = true
            };

            var note = new Label
            {
                Text      = "Orders are placed by customers on the website. You can update order status here.",
                Font      = BotanikaFonts.Body(9f),
                ForeColor = BotanikaColors.TextMuted,
                Location  = new Point(pad, 46),
                AutoSize  = true
            };

            _countLabel = new Label
            {
                Text      = "Loading...",
                Font      = BotanikaFonts.Body(9f),
                ForeColor = BotanikaColors.TextMuted,
                Location  = new Point(pad, 64),
                AutoSize  = true
            };

            _searchBox = new TextBox
            {
                Size        = new Size(200, 28),
                Location    = new Point(pad, 90),
                Font        = BotanikaFonts.Body(9.5f),
                ForeColor   = BotanikaColors.TextMuted,
                Text        = "Search orders..."
            };
            BotanikaTheme.StyleTextBox(_searchBox);
            _searchBox.GotFocus  += (s2, e2) => { if (_searchBox.Text == "Search orders...") { _searchBox.Text = ""; _searchBox.ForeColor = BotanikaColors.Charcoal; } };
            _searchBox.LostFocus += (s2, e2) => { if (string.IsNullOrEmpty(_searchBox.Text)) { _searchBox.Text = "Search orders..."; _searchBox.ForeColor = BotanikaColors.TextMuted; } };
            _searchBox.TextChanged += (s, e) => ApplyFilter();

            _statusFilter = new ComboBox
            {
                Size          = new Size(140, 28),
                Location      = new Point(pad + 230, 90),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = BotanikaFonts.Body(9.5f)
            };
            BotanikaTheme.StyleComboBox(_statusFilter);
            _statusFilter.Items.AddRange(new object[]
                { "All Statuses", "pending", "confirmed", "shipped", "delivered", "cancelled" });
            _statusFilter.SelectedIndex = 0;
            _statusFilter.SelectedIndexChanged += (s, e) => ApplyFilter();

            var exportBtn = new Button
            {
                Text      = "⬇ Export CSV",
                Size      = new Size(110, 32),
                Location  = new Point(pad + 700, 86)
            };
            BotanikaTheme.StyleSecondaryButton(exportBtn);
            exportBtn.Click += (s, e) => ExportData();

            _listView = new BotanikaListView
            {
                Location = new Point(pad, 130),
                Size     = new Size(900, 300),
                Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _listView.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "Order #",   Width = 110 },
                new ColumnHeader { Text = "Customer",  Width = 180 },
                new ColumnHeader { Text = "Date",      Width = 110 },
                new ColumnHeader { Text = "Items",     Width = 80  },
                new ColumnHeader { Text = "Total",     Width = 100 },
                new ColumnHeader { Text = "Status",    Width = 110 },
                new ColumnHeader { Text = "ID",        Width = 0   },
            });

            // Right-click to update status
            var ctx = new ContextMenuStrip();
            ctx.Items.Add("Mark as Confirmed",  null, (s, e) => UpdateStatus("confirmed"));
            ctx.Items.Add("Mark as Shipped",    null, (s, e) => UpdateStatus("shipped"));
            ctx.Items.Add("Mark as Delivered",  null, (s, e) => UpdateStatus("delivered"));
            ctx.Items.Add("Mark as Cancelled",  null, (s, e) => UpdateStatus("cancelled"));
            _listView.ContextMenuStrip = ctx;

            Controls.AddRange(new Control[]
            {
                header, note, _countLabel, _searchBox, _statusFilter, exportBtn, _listView
            });
        }

        public async Task RefreshListAsync()
        {
            try
            {
                _countLabel.Text = "Loading...";
                _allOrders = await FirebaseService.Instance.GetAllAsync<Order>("orders");
                // Sort most recent first by default
                _allOrders = _allOrders.OrderByDescending(o => o.OrderDate).ToList();
                ApplyFilter();
                _listView.AutoFitHeight();
                _countLabel.Text = $"{_allOrders.Count} order(s)";
            }
            catch (Exception ex)
            {
                _countLabel.Text = "Load failed";
                ToastNotification.ShowError($"Failed to load orders: {ex.Message}");
            }
        }

        private void ApplyFilter()
        {
            string rawOrd = _searchBox.Text == "Search orders..." ? "" : _searchBox.Text;
            string search = rawOrd.ToLowerInvariant();
            string status = _statusFilter.SelectedItem?.ToString();
            bool   allSt  = status == "All Statuses";

            _filtered = _allOrders.Where(o =>
                (string.IsNullOrEmpty(search) ||
                    (o.OrderNumber?.ToLowerInvariant().Contains(search) == true) ||
                    (o.CustomerName?.ToLowerInvariant().Contains(search) == true))
                && (allSt || o.Status == status)).ToList();

            _listView.Items.Clear();
            foreach (var o in _filtered)
            {
                var item = new ListViewItem(o.OrderNumber ?? o.Id);
                item.SubItems.Add(o.CustomerName ?? "");
                item.SubItems.Add(o.OrderDateDisplay);
                item.SubItems.Add(o.ItemCountDisplay);
                item.SubItems.Add(o.TotalDisplay);
                item.SubItems.Add(o.Status ?? "pending");
                item.SubItems.Add(o.Id ?? "");
                item.Tag = o;

                // Color-code rows by status so the admin can spot issues at a glance
                switch (o.Status?.ToLower())
                {
                    case "delivered":  item.ForeColor = BotanikaColors.Primary;    break;
                    case "cancelled":  item.ForeColor = BotanikaColors.Terracotta; break;
                    case "shipped":    item.ForeColor = BotanikaColors.PrimaryDark; break;
                }

                _listView.Items.Add(item);
            }
        }

        private Order GetSelected()
        {
            if (_listView.SelectedItems.Count == 0) return null;
            return _listView.SelectedItems[0].Tag as Order;
        }

        // Updates the status of the selected order in Firestore
        private void UpdateStatus(string newStatus)
        {
            var order = GetSelected();
            if (order == null) { ToastNotification.Show("Select an order first."); return; }

            _ = UpdateStatusAsync(order, newStatus);
        }

        private async Task UpdateStatusAsync(Order order, string newStatus)
        {
            try
            {
                order.Status = newStatus;
                await FirebaseService.Instance.SaveAsync("orders", order.Id, order);
                ApplyFilter();  // don't need a full reload, just re-render
                ToastNotification.Show($"Order marked as {newStatus}.");
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError($"Failed to update: {ex.Message}");
            }
        }

        public void ExportData()
        {
            var save = new SaveFileDialog
            {
                FileName   = $"orders_{DateTime.Now:yyyyMMdd}",
                Filter     = "CSV Files (*.csv)|*.csv",
                DefaultExt = "csv"
            };
            if (save.ShowDialog() != DialogResult.OK) return;
            try
            {
                CsvExporter.Export(_filtered.Count > 0 ? _filtered : _allOrders, save.FileName);
                ToastNotification.Show("Orders exported!");
            }
            catch (Exception ex) { ToastNotification.ShowError(ex.Message); }
        }

        public new void Refresh() => _ = RefreshListAsync();
        public void FocusSearch()  => _searchBox.Focus();
    }
}
