using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Botanika_Desktop.Firebase;
using Botanika_Desktop.Firebase.Models;
using Botanika_Desktop.Theme;

namespace Botanika_Desktop.Forms
{
    // The overview page — quick stats, revenue chart, recent orders.
    // First thing the admin sees after login, gives a bird's-eye view of the store.
    public class DashboardPanel : UserControl, IRefreshable
    {
        // Stat cards at the top
        private Label _totalRevenueValue;
        private Label _totalOrdersValue;
        private Label _totalProductsValue;
        private Label _totalClientsValue;
        private Label _lastUpdatedLabel;

        // Recent orders list at the bottom
        private Controls.BotanikaListView _recentOrdersList;

        public DashboardPanel()
        {
            BackColor = BotanikaColors.Offwhite;
            Dock      = DockStyle.Fill;
            BuildUI();
            BotanikaTheme.RoundAllCards(this);

            // Load data right away — no need to wait for a button click
            _ = LoadDashboardDataAsync();
        }

        private void BuildUI()
        {
            int padding = 24;

            // ── Page header ────────────────────────────────────────────────────
            var header = new Label
            {
                Text      = "Dashboard",
                Font      = BotanikaFonts.Heading(18f, FontStyle.Bold),
                ForeColor = BotanikaColors.Charcoal,
                Location  = new Point(padding, 20),
                AutoSize  = true
            };

            var subheader = new Label
            {
                Text      = $"Welcome back, {Session.DisplayName}! Here's how Botanika is doing today.",
                Font      = BotanikaFonts.Body(10f),
                ForeColor = BotanikaColors.TextLight,
                Location  = new Point(padding, 50),
                AutoSize  = true
            };

            // ── Four stat cards ────────────────────────────────────────────────
            var card1 = CreateStatCard("💰 Total Revenue", "$0.00", BotanikaColors.Primary,
                out _totalRevenueValue, new Point(padding, 90));

            var card2 = CreateStatCard("📦 Total Orders", "0", BotanikaColors.Terracotta,
                out _totalOrdersValue, new Point(padding + 220, 90));

            var card3 = CreateStatCard("🌿 Products", "0", BotanikaColors.Charcoal,
                out _totalProductsValue, new Point(padding + 440, 90));

            var card4 = CreateStatCard("👥 Clients", "0", BotanikaColors.PrimaryDark,
                out _totalClientsValue, new Point(padding + 660, 90));

            // ── Recent orders section ──────────────────────────────────────────
            var ordersHeader = new Label
            {
                Text      = "Recent Orders",
                Font      = BotanikaFonts.Heading(13f, FontStyle.Bold),
                ForeColor = BotanikaColors.Charcoal,
                Location  = new Point(padding, 240),
                AutoSize  = true
            };

            _recentOrdersList = new Controls.BotanikaListView
            {
                Location = new Point(padding, 270),
                Size     = new Size(800, 300),  // Anchor stretches it to fit
                Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _recentOrdersList.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "Order #",   Width = 110 },
                new ColumnHeader { Text = "Customer",  Width = 180 },
                new ColumnHeader { Text = "Date",      Width = 110 },
                new ColumnHeader { Text = "Items",     Width = 80  },
                new ColumnHeader { Text = "Total",     Width = 100 },
                new ColumnHeader { Text = "Status",    Width = 100 },
            });

            // ── Last updated timestamp ─────────────────────────────────────────
            _lastUpdatedLabel = new Label
            {
                Text      = "Loading...",
                Font      = BotanikaFonts.Caption(8f),
                ForeColor = BotanikaColors.TextMuted,
                Location  = new Point(padding, 580),
                AutoSize  = true
            };

            // ── Refresh button ─────────────────────────────────────────────────
            var refreshBtn = new Button
            {
                Text      = "⟳ Refresh",
                Size      = new Size(110, 32),
                Location  = new Point(padding + 790, 235)
            };
            BotanikaTheme.StyleSecondaryButton(refreshBtn);
            refreshBtn.Click += async (s, e) => await LoadDashboardDataAsync();

            Controls.AddRange(new Control[]
            {
                header, subheader,
                card1, card2, card3, card4,
                ordersHeader, refreshBtn,
                _recentOrdersList,
                _lastUpdatedLabel
            });
        }

        // Creates a single stat card — colored top border, big number, label + subtitle
        private Panel CreateStatCard(string title, string initialValue,
            Color accentColor, out Label valueLabel, Point location)
        {
            var card = new Panel
            {
                Size      = new Size(200, 110),
                Location  = location,
                BackColor = BotanikaColors.White
            };

            // Colored accent bar on the top edge (modern horizontal style)
            var accent = new Panel
            {
                Size      = new Size(200, 4),
                Location  = new Point(0, 0),
                BackColor = accentColor
            };

            var titleLbl = new Label
            {
                Text      = title,
                Font      = BotanikaFonts.Caption(8.5f),
                ForeColor = BotanikaColors.TextMuted,
                Location  = new Point(18, 18),
                AutoSize  = true
            };

            valueLabel = new Label
            {
                Text      = initialValue,
                Font      = BotanikaFonts.Heading(22f, FontStyle.Bold),
                ForeColor = BotanikaColors.Charcoal,
                Location  = new Point(18, 44),
                AutoSize  = true
            };

            var subtitleLbl = new Label
            {
                Text      = "from Firestore",
                Font      = BotanikaFonts.Caption(7.5f),
                ForeColor = Color.FromArgb(180, BotanikaColors.TextMuted),
                Location  = new Point(18, 82),
                AutoSize  = true
            };

            card.Controls.AddRange(new Control[] { accent, titleLbl, valueLabel, subtitleLbl });
            BotanikaTheme.ApplyRoundedCorners(card, 10);
            return card;
        }

        // ─── Data Loading ──────────────────────────────────────────────────────

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                // Load everything in parallel — faster than sequential awaits
                var productsTask = FirebaseService.Instance.GetAllAsync<Product>("products");
                var clientsTask  = FirebaseService.Instance.GetAllAsync<Client>("users");
                var ordersTask   = FirebaseService.Instance.GetAllAsync<Order>("orders");

                await Task.WhenAll(productsTask, clientsTask, ordersTask);

                var products = productsTask.Result;
                var clients  = clientsTask.Result;
                var orders   = ordersTask.Result;

                // Update the stat cards
                double totalRevenue = orders.Sum(o => o.Total);
                _totalRevenueValue.Text  = $"${totalRevenue:F2}";
                _totalOrdersValue.Text   = orders.Count.ToString();
                _totalProductsValue.Text = products.Count.ToString();
                _totalClientsValue.Text  = clients.Count.ToString();

                // Populate the recent orders list — most recent first
                _recentOrdersList.Items.Clear();
                var recentOrders = orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10);

                foreach (var order in recentOrders)
                {
                    var item = new ListViewItem(order.OrderNumber ?? order.Id);
                    item.SubItems.Add(order.CustomerName);
                    item.SubItems.Add(order.OrderDateDisplay);
                    item.SubItems.Add(order.ItemCountDisplay);
                    item.SubItems.Add(order.TotalDisplay);
                    item.SubItems.Add(order.Status ?? "pending");
                    _recentOrdersList.Items.Add(item);
                }

                _recentOrdersList.AutoFitHeight();
                _lastUpdatedLabel.Text = $"Last updated: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                _lastUpdatedLabel.Text = $"Failed to load: {ex.Message}";
            }
        }

        // IRefreshable implementation
        public new void Refresh() => _ = LoadDashboardDataAsync();
    }
}
