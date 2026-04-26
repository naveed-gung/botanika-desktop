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
    // Payments panel — two tabs: money we've received from customers,
    // and money we still owe to suppliers.
    // Rows are color-coded: green = paid, yellow = pending, red = overdue.
    public class PaymentsPanel : UserControl, IRefreshable, ICrudPanel, IExportable
    {
        private TabControl        _tabs;
        private BotanikaListView  _receivedList;
        private BotanikaListView  _toPayList;
        private Label             _receivedTotal;
        private Label             _toPayTotal;
        private List<Payment>     _allPayments  = new List<Payment>();

        public PaymentsPanel()
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
                Text      = "Payments",
                Font      = BotanikaFonts.Heading(18f, FontStyle.Bold),
                ForeColor = BotanikaColors.Charcoal,
                Location  = new Point(pad, 16),
                AutoSize  = true
            };

            // ── Summary totals ─────────────────────────────────────────────────
            _receivedTotal = new Label
            {
                Text      = "Received: $0.00",
                Font      = BotanikaFonts.Body(10f, FontStyle.Bold),
                ForeColor = BotanikaColors.Primary,
                Location  = new Point(pad, 50),
                AutoSize  = true
            };

            _toPayTotal = new Label
            {
                Text      = "Owed: $0.00",
                Font      = BotanikaFonts.Body(10f, FontStyle.Bold),
                ForeColor = BotanikaColors.Terracotta,
                Location  = new Point(pad + 200, 50),
                AutoSize  = true
            };

            // ── Add and Export buttons ─────────────────────────────────────────
            var addBtn = new Button
            {
                Text      = "+ Add Payment",
                Size      = new Size(120, 32),
                Location  = new Point(pad + 700, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = BotanikaColors.Primary,
                ForeColor = Color.White,
                Font      = BotanikaFonts.Body(9.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            addBtn.FlatAppearance.BorderSize = 0;
            addBtn.Click += (s, e) => AddNew();

            var exportBtn = new Button
            {
                Text      = "⬇ Export CSV",
                Size      = new Size(110, 32),
                Location  = new Point(pad + 580, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = BotanikaColors.SandLight,
                ForeColor = BotanikaColors.Charcoal,
                Font      = BotanikaFonts.Body(9.5f),
                Cursor    = Cursors.Hand
            };
            exportBtn.FlatAppearance.BorderColor = BotanikaColors.Sand;
            exportBtn.FlatAppearance.BorderSize  = 1;
            exportBtn.Click += (s, e) => ExportData();

            // ── Tabbed lists ───────────────────────────────────────────────────
            _tabs = new TabControl
            {
                Location  = new Point(pad, 88),
                Size      = new Size(900, 300),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font      = BotanikaFonts.Body(9.5f)
            };

            var receivedTab = new TabPage("💚 Received");
            var toPayTab    = new TabPage("🔴 To Pay");

            _receivedList = CreatePaymentList();
            _receivedList.Dock = DockStyle.Fill;
            receivedTab.Controls.Add(_receivedList);

            _toPayList = CreatePaymentList();
            _toPayList.Dock = DockStyle.Fill;
            toPayTab.Controls.Add(_toPayList);

            _tabs.TabPages.Add(receivedTab);
            _tabs.TabPages.Add(toPayTab);

            Controls.AddRange(new Control[]
            {
                header, _receivedTotal, _toPayTotal, addBtn, exportBtn, _tabs
            });
        }

        // Creates a payment list with the standard columns
        private BotanikaListView CreatePaymentList()
        {
            var lv = new BotanikaListView();
            lv.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "Party",       Width = 180 },
                new ColumnHeader { Text = "Amount",      Width = 100 },
                new ColumnHeader { Text = "Description", Width = 200 },
                new ColumnHeader { Text = "Due Date",    Width = 110 },
                new ColumnHeader { Text = "Status",      Width = 90  },
                new ColumnHeader { Text = "Reference",   Width = 120 },
                new ColumnHeader { Text = "ID",          Width = 0   },
            });

            var ctx = new ContextMenuStrip();
            ctx.Items.Add("✏ Edit",         null, (s, e) => EditSelected());
            ctx.Items.Add("✓ Mark as Paid", null, (s, e) => MarkAsPaid());
            ctx.Items.Add("🗑 Delete",       null, (s, e) => DeleteSelected());
            lv.ContextMenuStrip = ctx;
            lv.DoubleClick     += (s, e) => EditSelected();

            return lv;
        }

        // ─── Data Loading ──────────────────────────────────────────────────────

        public async Task RefreshListAsync()
        {
            try
            {
                _allPayments = await FirebaseService.Instance.GetAllAsync<Payment>("payments");

                // Seed default payments if collection is empty
                if (_allPayments.Count == 0)
                {
                    await SeedDefaultPaymentsAsync();
                    _allPayments = await FirebaseService.Instance.GetAllAsync<Payment>("payments");
                }

                PopulateLists();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError($"Failed to load payments: {ex.Message}");
            }
        }

        private async Task SeedDefaultPaymentsAsync()
        {
            var seeds = new[]
            {
                new Payment { Direction = "received", Party = "Laila Nasser", Amount = 80.00, Status = "paid", Description = "Order BOT-SEED-1001", DueDate = DateTime.Today.AddDays(-10), PaidDate = DateTime.Today.AddDays(-8), Reference = "TXN-20260410" },
                new Payment { Direction = "received", Party = "Omar Haddad", Amount = 129.00, Status = "paid", Description = "Order BOT-SEED-1002", DueDate = DateTime.Today.AddDays(-5), PaidDate = DateTime.Today.AddDays(-5), Reference = "TXN-20260415" },
                new Payment { Direction = "received", Party = "Sara Khalil", Amount = 82.00, Status = "pending", Description = "Order BOT-SEED-1003", DueDate = DateTime.Today.AddDays(7), Reference = "TXN-20260420" },
                new Payment { Direction = "topay", Party = "Green Valley Nursery", Amount = 450.00, Status = "pending", Description = "Monthly plant supply - April", DueDate = DateTime.Today.AddDays(15), Reference = "INV-GV-0042" },
                new Payment { Direction = "topay", Party = "TerraHerbs International", Amount = 220.00, Status = "overdue", Description = "Herb seeds shipment Q1", DueDate = DateTime.Today.AddDays(-14), Reference = "INV-TH-0018" },
                new Payment { Direction = "topay", Party = "Desert Succulents Ltd", Amount = 310.00, Status = "paid", Description = "Succulent order - March", DueDate = DateTime.Today.AddDays(-30), PaidDate = DateTime.Today.AddDays(-28), Reference = "INV-DS-0033" },
            };
            for (int i = 0; i < seeds.Length; i++)
            {
                string id = $"pay_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{i}";
                seeds[i].Id = id;
                await FirebaseService.Instance.SaveAsync("payments", id, seeds[i]);
            }
        }

        private void PopulateLists()
        {
            var received = _allPayments.Where(p => p.Direction == "received").ToList();
            var toPay    = _allPayments.Where(p => p.Direction == "topay").ToList();

            // Update the summary totals
            double totalReceived = received.Where(p => p.Status == "paid").Sum(p => p.Amount);
            double totalOwed     = toPay.Where(p => p.Status != "paid").Sum(p => p.Amount);
            _receivedTotal.Text  = $"Received: ${totalReceived:F2}";
            _toPayTotal.Text     = $"Owed: ${totalOwed:F2}";

            PopulateList(_receivedList, received);
            PopulateList(_toPayList, toPay);
        }

        private void PopulateList(BotanikaListView lv, List<Payment> payments)
        {
            lv.Items.Clear();
            foreach (var p in payments)
            {
                var item = new ListViewItem(p.Party ?? "");
                item.SubItems.Add(p.AmountDisplay);
                item.SubItems.Add(p.Description ?? "");
                item.SubItems.Add(p.DueDateDisplay);
                item.SubItems.Add(p.Status ?? "pending");
                item.SubItems.Add(p.Reference ?? "");
                item.SubItems.Add(p.Id ?? "");
                item.Tag = p;

                // Color-code by status — one glance tells you what needs attention
                switch (p.Status?.ToLower())
                {
                    case "paid":
                        item.BackColor = Color.FromArgb(30, 76, 175, 80);  // faint green
                        break;
                    case "overdue":
                        item.BackColor = Color.FromArgb(30, 244, 67, 54);  // faint red
                        break;
                    case "pending":
                        item.BackColor = Color.FromArgb(30, 255, 193, 7);  // faint yellow
                        break;
                }

                lv.Items.Add(item);
            }
        }

        // Gets the selected payment from whichever tab is active
        private Payment GetSelected()
        {
            var activeList = _tabs.SelectedIndex == 0 ? _receivedList : _toPayList;
            if (activeList.SelectedItems.Count == 0) return null;
            return activeList.SelectedItems[0].Tag as Payment;
        }

        // ─── CRUD ──────────────────────────────────────────────────────────────

        public void AddNew()
        {
            // Default direction based on which tab is active
            string direction = _tabs.SelectedIndex == 0 ? "received" : "topay";
            var dlg = new PaymentEditDialog(null, direction);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            _ = SaveNewAsync(dlg.Payment);
        }

        private async Task SaveNewAsync(Payment payment)
        {
            try
            {
                string id = $"pay_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                payment.Id = id;
                await FirebaseService.Instance.SaveAsync("payments", id, payment);
                await RefreshListAsync();
                ToastNotification.Show("Payment added!");
            }
            catch (Exception ex) { ToastNotification.ShowError(ex.Message); }
        }

        public void EditSelected()
        {
            var p = GetSelected();
            if (p == null) { ToastNotification.Show("Select a payment to edit."); return; }
            var dlg = new PaymentEditDialog(p, p.Direction);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            _ = UpdateAsync(p.Id, dlg.Payment);
        }

        private async Task UpdateAsync(string id, Payment payment)
        {
            try
            {
                await FirebaseService.Instance.SaveAsync("payments", id, payment);
                await RefreshListAsync();
                ToastNotification.Show("Payment updated!");
            }
            catch (Exception ex) { ToastNotification.ShowError(ex.Message); }
        }

        // Quick action — marks the selected payment as paid right now
        private void MarkAsPaid()
        {
            var p = GetSelected();
            if (p == null) return;
            p.Status   = "paid";
            p.PaidDate = DateTime.UtcNow;
            _ = UpdateAsync(p.Id, p);
        }

        public void DeleteSelected()
        {
            var p = GetSelected();
            if (p == null) return;
            var confirm = MessageBox.Show("Delete this payment record?",
                "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;
            _ = DeleteAsync(p.Id);
        }

        private async Task DeleteAsync(string id)
        {
            try
            {
                await FirebaseService.Instance.DeleteAsync("payments", id);
                await RefreshListAsync();
                ToastNotification.Show("Payment deleted.");
            }
            catch (Exception ex) { ToastNotification.ShowError(ex.Message); }
        }

        public void ExportData()
        {
            var save = new SaveFileDialog
            {
                FileName   = $"payments_{DateTime.Now:yyyyMMdd}",
                Filter     = "CSV Files (*.csv)|*.csv",
                DefaultExt = "csv"
            };
            if (save.ShowDialog() != DialogResult.OK) return;
            try
            {
                CsvExporter.Export(_allPayments, save.FileName);
                ToastNotification.Show("Payments exported!");
            }
            catch (Exception ex) { ToastNotification.ShowError(ex.Message); }
        }

        public new void Refresh() => _ = RefreshListAsync();
    }

    // ─── Payment edit dialog ───────────────────────────────────────────────────

    public class PaymentEditDialog : Form
    {
        public Payment Payment { get; private set; }

        private TextBox   _partyBox, _amountBox, _descBox, _refBox;
        private ComboBox  _statusBox;
        private DateTimePicker _dueDatePicker;

        public PaymentEditDialog(Payment existing, string direction)
        {
            Payment = existing ?? new Payment
            {
                Direction = direction,
                Status    = "pending",
                DueDate   = DateTime.Today.AddDays(30)
            };
            BuildUI(direction);
            if (existing != null) FillFields(existing);
        }

        private void BuildUI(string direction)
        {
            string dirLabel = direction == "received" ? "Received From" : "Pay To";
            Text            = Payment.Id == null ? "Add Payment" : "Edit Payment";
            Size            = new Size(420, 360);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = BotanikaColors.Offwhite;

            int x = 20, lw = 110, fw = 250, y = 16, rh = 32;

            void Row(string label, Control ctrl)
            {
                Controls.Add(new Label
                {
                    Text = label, Font = BotanikaFonts.Body(9f),
                    ForeColor = BotanikaColors.TextLight,
                    Size = new Size(lw, rh), Location = new Point(x, y + 4)
                });
                ctrl.Size = new Size(fw, rh); ctrl.Location = new Point(x + lw + 8, y);
                Controls.Add(ctrl);
                y += rh + 8;
            }

            _partyBox  = new TextBox(); BotanikaTheme.StyleTextBox(_partyBox);  Row(dirLabel + " *", _partyBox);
            _amountBox = new TextBox(); BotanikaTheme.StyleTextBox(_amountBox); Row("Amount ($) *",  _amountBox);
            _descBox   = new TextBox(); BotanikaTheme.StyleTextBox(_descBox);   Row("Description",   _descBox);
            _refBox    = new TextBox(); BotanikaTheme.StyleTextBox(_refBox);    Row("Reference #",   _refBox);

            _statusBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            _statusBox.Items.AddRange(new object[] { "pending", "paid", "overdue" });
            BotanikaTheme.StyleComboBox(_statusBox);
            Row("Status", _statusBox);

            _dueDatePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value  = DateTime.Today.AddDays(30)
            };
            Row("Due Date", _dueDatePicker);

            var saveBtn = new Button
            {
                Text = "Save", Size = new Size(100, 34),
                Location = new Point(x + lw + 8 + 80, y + 8),
                FlatStyle = FlatStyle.Flat, BackColor = BotanikaColors.Primary,
                ForeColor = Color.White, Cursor = Cursors.Hand,
                Font = BotanikaFonts.Body(9.5f, FontStyle.Bold)
            };
            saveBtn.FlatAppearance.BorderSize = 0;
            saveBtn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_partyBox.Text))
                { MessageBox.Show("Party name is required."); return; }
                if (!double.TryParse(_amountBox.Text, out double amount) || amount < 0)
                { MessageBox.Show("Enter a valid amount."); return; }

                Payment.Party       = _partyBox.Text.Trim();
                Payment.Amount      = amount;
                Payment.Description = _descBox.Text.Trim();
                Payment.Reference   = _refBox.Text.Trim();
                Payment.Status      = _statusBox.SelectedItem?.ToString() ?? "pending";
                Payment.DueDate     = _dueDatePicker.Value;

                DialogResult = DialogResult.OK;
                Close();
            };

            var cancelBtn = new Button
            {
                Text = "Cancel", Size = new Size(80, 34),
                Location = new Point(x + lw + 8, y + 8),
                FlatStyle = FlatStyle.Flat, BackColor = BotanikaColors.SandLight,
                ForeColor = BotanikaColors.Charcoal, Cursor = Cursors.Hand
            };
            cancelBtn.FlatAppearance.BorderSize = 0;
            cancelBtn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(saveBtn);
            Controls.Add(cancelBtn);
            ClientSize = new Size(400, y + 60);
            AcceptButton = saveBtn;
            CancelButton = cancelBtn;
        }

        private void FillFields(Payment p)
        {
            _partyBox.Text  = p.Party       ?? "";
            _amountBox.Text = p.Amount.ToString("F2");
            _descBox.Text   = p.Description ?? "";
            _refBox.Text    = p.Reference   ?? "";
            _statusBox.SelectedItem = p.Status ?? "pending";
            _dueDatePicker.Value = p.DueDate > DateTime.MinValue ? p.DueDate : DateTime.Today;
        }
    }
}
