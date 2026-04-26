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
    // Revenue overview — aggregates all order data into monthly summaries.
    // Shows a bar chart (using a custom drawn panel since we can't add DataVisualization easily),
    // plus the four key financial KPI cards at the top.
    public class RevenuePanel : UserControl, IRefreshable
    {
        private Label         _totalRevenueLabel;
        private Label         _monthRevenueLabel;
        private Label         _avgOrderLabel;
        private Label         _topProductLabel;
        private Panel         _chartArea;
        private Label         _statusLabel;

        // Monthly revenue data for the chart
        private Dictionary<string, double> _monthlyRevenue = new Dictionary<string, double>();

        public RevenuePanel()
        {
            BackColor = BotanikaColors.Offwhite;
            Dock      = DockStyle.Fill;
            BuildUI();
            BotanikaTheme.RoundAllCards(this);
            _ = LoadDataAsync();
        }

        private void BuildUI()
        {
            int pad = 24;

            var header = new Label
            {
                Text      = "Revenue",
                Font      = BotanikaFonts.Heading(18f, FontStyle.Bold),
                ForeColor = BotanikaColors.Charcoal,
                Location  = new Point(pad, 16),
                AutoSize  = true
            };

            var subheader = new Label
            {
                Text      = "Financial overview and monthly breakdown",
                Font      = BotanikaFonts.Body(9f),
                ForeColor = BotanikaColors.TextMuted,
                Location  = new Point(pad, 44),
                AutoSize  = true
            };

            // ── KPI cards row ──────────────────────────────────────────────────
            _totalRevenueLabel = CreateKpiCard("Total Revenue", "$0.00",
                BotanikaColors.Primary, new Point(pad, 76));

            _monthRevenueLabel = CreateKpiCard("This Month", "$0.00",
                BotanikaColors.PrimaryDark, new Point(pad + 200, 76));

            _avgOrderLabel = CreateKpiCard("Avg. Order Value", "$0.00",
                BotanikaColors.Terracotta, new Point(pad + 400, 76));

            _topProductLabel = CreateKpiCard("Top Product", "—",
                BotanikaColors.Charcoal, new Point(pad + 600, 76));

            // ── Chart heading ──────────────────────────────────────────────────
            var chartTitle = new Label
            {
                Text      = "Monthly Revenue (last 12 months)",
                Font      = BotanikaFonts.Heading(12f, FontStyle.Bold),
                ForeColor = BotanikaColors.Charcoal,
                Location  = new Point(pad, 200),
                AutoSize  = true
            };

            // ── Custom drawn bar chart ─────────────────────────────────────────
            // We draw the chart ourselves since DataVisualization needs an extra package
            _chartArea = new Panel
            {
                Location  = new Point(pad, 230),
                Size      = new Size(800, 260),
                BackColor = BotanikaColors.White,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _chartArea.Paint += ChartArea_Paint;

            // ── Status / error label ───────────────────────────────────────────
            _statusLabel = new Label
            {
                Text      = "Loading revenue data...",
                Font      = BotanikaFonts.Body(9f),
                ForeColor = BotanikaColors.TextMuted,
                Location  = new Point(pad, 500),
                AutoSize  = true
            };

            var refreshBtn = new Button
            {
                Text      = "⟳ Refresh Data",
                Size      = new Size(130, 32),
                Location  = new Point(pad + 690, 200)
            };
            BotanikaTheme.StyleSecondaryButton(refreshBtn);
            refreshBtn.Click += async (s, e) => await LoadDataAsync();

            Controls.AddRange(new Control[]
            {
                header, subheader, refreshBtn,
                chartTitle, _chartArea, _statusLabel
            });
        }

        // Creates a KPI metric card — returns the value Label so we can update it later
        private Label CreateKpiCard(string title, string initialValue,
            Color accentColor, Point location)
        {
            var card = new Panel
            {
                Size      = new Size(185, 100),
                Location  = location,
                BackColor = BotanikaColors.White
            };

            // Top accent bar (horizontal, modern style)
            card.Controls.Add(new Panel
            {
                Size      = new Size(185, 4),
                Location  = new Point(0, 0),
                BackColor = accentColor
            });

            card.Controls.Add(new Label
            {
                Text      = title,
                Font      = BotanikaFonts.Caption(8f),
                ForeColor = BotanikaColors.TextMuted,
                Location  = new Point(14, 14),
                AutoSize  = true
            });

            var valueLabel = new Label
            {
                Text      = initialValue,
                Font      = BotanikaFonts.Heading(18f, FontStyle.Bold),
                ForeColor = BotanikaColors.Charcoal,
                Location  = new Point(14, 40),
                AutoSize  = true
            };
            card.Controls.Add(valueLabel);
            Controls.Add(card);

            return valueLabel;
        }

        // ─── Data Loading ──────────────────────────────────────────────────────

        private async Task LoadDataAsync()
        {
            _statusLabel.Text = "Loading...";
            try
            {
                var orders = await FirebaseService.Instance.GetAllAsync<Order>("orders");

                if (orders.Count == 0)
                {
                    _statusLabel.Text = "No orders found.";
                    return;
                }

                // ── KPI calculations ───────────────────────────────────────────
                double total   = orders.Sum(o => o.Total);
                double thisMonth = orders
                    .Where(o => o.OrderDate.Year == DateTime.Now.Year &&
                                o.OrderDate.Month == DateTime.Now.Month)
                    .Sum(o => o.Total);
                double avgOrder = orders.Count > 0 ? total / orders.Count : 0;

                // Find the best-selling product by counting order item appearances
                var productSales = new Dictionary<string, int>();
                foreach (var order in orders)
                    foreach (var item in order.Items ?? new List<OrderItem>())
                    {
                        if (!productSales.ContainsKey(item.ProductName ?? ""))
                            productSales[item.ProductName ?? ""] = 0;
                        productSales[item.ProductName ?? ""] += item.Quantity;
                    }
                string topProduct = productSales.Count > 0
                    ? productSales.OrderByDescending(kv => kv.Value).First().Key
                    : "N/A";

                _totalRevenueLabel.Text = $"${total:F2}";
                _monthRevenueLabel.Text = $"${thisMonth:F2}";
                _avgOrderLabel.Text     = $"${avgOrder:F2}";
                _topProductLabel.Text   = topProduct;

                // ── Monthly revenue data for the chart ─────────────────────────
                // Group orders by "yyyy-MM" and sum the totals
                // TakeLast not available in .NET 4.7.2 — reverse, take 12, reverse back
                var grouped = orders
                    .GroupBy(o => o.OrderDate.ToString("yyyy-MM"))
                    .OrderBy(g => g.Key)
                    .ToList();
                int skip = Math.Max(0, grouped.Count - 12);
                _monthlyRevenue = grouped
                    .Skip(skip)
                    .ToDictionary(g => g.Key, g => g.Sum(o => o.Total));

                // Trigger a redraw of the chart area
                _chartArea.Invalidate();
                _statusLabel.Text = $"Last updated: {DateTime.Now:HH:mm:ss}  ·  {orders.Count} orders total";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Failed to load: {ex.Message}";
            }
        }

        // ─── Custom Bar Chart Drawing ──────────────────────────────────────────

        // Draws a simple but clean bar chart directly onto the panel using GDI+.
        // Each bar represents one month's revenue.
        private void ChartArea_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BotanikaColors.White);

            if (_monthlyRevenue.Count == 0)
            {
                // Nothing to draw yet
                TextRenderer.DrawText(g, "No data available", BotanikaFonts.Body(10f),
                    _chartArea.ClientRectangle, BotanikaColors.TextMuted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            int chartPad  = 40;
            int chartW    = _chartArea.Width  - chartPad * 2;
            int chartH    = _chartArea.Height - chartPad * 2;
            double maxVal = _monthlyRevenue.Values.Max();
            if (maxVal <= 0) maxVal = 1; // avoid divide-by-zero

            var months = _monthlyRevenue.Keys.ToList();
            int   barCount = months.Count;
            float barW     = (float)chartW / barCount * 0.65f;
            float barGap   = (float)chartW / barCount;

            // Draw horizontal grid lines
            using (var gridPen = new Pen(Color.FromArgb(30, 0, 0, 0)))
            {
                for (int i = 1; i <= 4; i++)
                {
                    int yLine = chartPad + (int)(chartH * (1 - i / 4.0));
                    g.DrawLine(gridPen, chartPad, yLine, chartPad + chartW, yLine);

                    // Grid value labels
                    string val = $"${maxVal * i / 4:F0}";
                    TextRenderer.DrawText(g, val, BotanikaFonts.Caption(7f),
                        new Rectangle(0, yLine - 8, chartPad - 2, 16),
                        BotanikaColors.TextMuted,
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
                }
            }

            // Draw each bar
            for (int i = 0; i < barCount; i++)
            {
                double value  = _monthlyRevenue[months[i]];
                float  barH   = (float)(value / maxVal * chartH);
                float  barX   = chartPad + i * barGap + (barGap - barW) / 2;
                float  barY   = chartPad + chartH - barH;

                // Main bar fill — primary green
                using (var brush = new SolidBrush(BotanikaColors.Primary))
                    g.FillRectangle(brush, barX, barY, barW, barH);

                // Subtle top highlight
                using (var highlight = new SolidBrush(Color.FromArgb(60, 255, 255, 255)))
                    g.FillRectangle(highlight, barX, barY, barW, 4);

                // Month label below the bar
                string monthLabel = months[i].Length >= 7 ? months[i].Substring(5) : months[i];
                TextRenderer.DrawText(g, monthLabel, BotanikaFonts.Caption(7.5f),
                    new Rectangle((int)barX, chartPad + chartH + 4, (int)barW, 16),
                    BotanikaColors.TextLight, TextFormatFlags.HorizontalCenter);

                // Value above the bar (only if there's room)
                if (barH > 20)
                {
                    TextRenderer.DrawText(g, $"${value:F0}", BotanikaFonts.Caption(7f),
                        new Rectangle((int)barX, (int)barY - 14, (int)barW, 14),
                        BotanikaColors.Charcoal, TextFormatFlags.HorizontalCenter);
                }
            }

            // Draw the bottom axis line
            using (var axisPen = new Pen(BotanikaColors.Sand, 1))
                g.DrawLine(axisPen, chartPad, chartPad + chartH, chartPad + chartW, chartPad + chartH);
        }

        public new void Refresh() => _ = LoadDataAsync();
    }
}
