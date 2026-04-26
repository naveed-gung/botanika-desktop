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
    // Client management — shows all customers, lets you add/edit/delete them.
    // Follows the exact same pattern as ProductsPanel, just with different columns.
    public class ClientsPanel : UserControl, IRefreshable, ICrudPanel, ISearchable, IExportable
    {
        private BotanikaListView _listView;
        private TextBox          _searchBox;
        private Label            _countLabel;
        private List<Client>     _allClients  = new List<Client>();
        private List<Client>     _filtered    = new List<Client>();

        public ClientsPanel()
        {
            BackColor = BotanikaColors.Offwhite;
            Dock      = DockStyle.Fill;
            BuildUI();
            _ = RefreshListAsync();
        }

        private void BuildUI()
        {
            int pad = 24;

            var header = new Label
            {
                Text      = "Clients",
                Font      = BotanikaFonts.Heading(18f, FontStyle.Bold),
                ForeColor = BotanikaColors.Charcoal,
                Location  = new Point(pad, 16),
                AutoSize  = true
            };

            _countLabel = new Label
            {
                Text      = "Loading...",
                Font      = BotanikaFonts.Body(9f),
                ForeColor = BotanikaColors.TextMuted,
                Location  = new Point(pad, 46),
                AutoSize  = true
            };

            _searchBox = new TextBox
            {
                Size        = new Size(220, 28),
                Location    = new Point(pad, 72),
                Font        = BotanikaFonts.Body(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor   = BotanikaColors.TextMuted,
                Text        = "Search clients..."
            };
            _searchBox.GotFocus  += (s, e) => { if (_searchBox.Text == "Search clients...") { _searchBox.Text = ""; _searchBox.ForeColor = BotanikaColors.Charcoal; } };
            _searchBox.LostFocus += (s, e) => { if (string.IsNullOrEmpty(_searchBox.Text)) { _searchBox.Text = "Search clients..."; _searchBox.ForeColor = BotanikaColors.TextMuted; } };
            _searchBox.TextChanged += (s, e) => ApplyFilter();

            // Add client button
            var addBtn = new Button
            {
                Text      = "+ Add Client",
                Size      = new Size(110, 32),
                Location  = new Point(pad + 700, 68),
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
                Location  = new Point(pad + 580, 68),
                FlatStyle = FlatStyle.Flat,
                BackColor = BotanikaColors.SandLight,
                ForeColor = BotanikaColors.Charcoal,
                Font      = BotanikaFonts.Body(9.5f),
                Cursor    = Cursors.Hand
            };
            exportBtn.FlatAppearance.BorderColor = BotanikaColors.Sand;
            exportBtn.FlatAppearance.BorderSize  = 1;
            exportBtn.Click += (s, e) => ExportData();

            _listView = new BotanikaListView
            {
                Location = new Point(pad, 112),
                Size     = new Size(900, 500),
                Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            _listView.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "Name",        Width = 180 },
                new ColumnHeader { Text = "Email",       Width = 200 },
                new ColumnHeader { Text = "Phone",       Width = 130 },
                new ColumnHeader { Text = "Orders",      Width = 70  },
                new ColumnHeader { Text = "Total Spent", Width = 110 },
                new ColumnHeader { Text = "Member Since",Width = 110 },
                new ColumnHeader { Text = "ID",          Width = 0   },
            });

            var ctx = new ContextMenuStrip();
            ctx.Items.Add("✏ Edit",   null, (s, e) => EditSelected());
            ctx.Items.Add("🗑 Delete", null, (s, e) => DeleteSelected());
            _listView.ContextMenuStrip = ctx;
            _listView.DoubleClick     += (s, e) => EditSelected();

            Controls.AddRange(new Control[]
            {
                header, _countLabel, _searchBox, addBtn, exportBtn, _listView
            });
        }

        public async Task RefreshListAsync()
        {
            try
            {
                _countLabel.Text = "Loading...";
                _allClients = await FirebaseService.Instance.GetAllAsync<Client>("clients");
                ApplyFilter();
                _countLabel.Text = $"{_allClients.Count} client(s)";
            }
            catch (Exception ex)
            {
                _countLabel.Text = "Load failed";
                ToastNotification.ShowError($"Failed to load clients: {ex.Message}");
            }
        }

        private void ApplyFilter()
        {
            string raw = _searchBox.Text == "Search clients..." ? "" : _searchBox.Text;
            string s = raw.ToLowerInvariant();
            _filtered = string.IsNullOrEmpty(s)
                ? _allClients
                : _allClients.Where(c =>
                    (c.Name?.ToLowerInvariant().Contains(s) == true) ||
                    (c.Email?.ToLowerInvariant().Contains(s) == true)).ToList();

            _listView.Items.Clear();
            foreach (var c in _filtered)
            {
                var item = new ListViewItem(c.Name ?? "");
                item.SubItems.Add(c.Email ?? "");
                item.SubItems.Add(c.Phone ?? "");
                item.SubItems.Add(c.OrderCount.ToString());
                item.SubItems.Add(c.TotalSpentDisplay);
                item.SubItems.Add(c.MemberSince);
                item.SubItems.Add(c.Id ?? "");
                item.Tag = c;
                _listView.Items.Add(item);
            }
        }

        private Client GetSelected()
        {
            if (_listView.SelectedItems.Count == 0) return null;
            return _listView.SelectedItems[0].Tag as Client;
        }

        public void AddNew()
        {
            var dialog = new ClientEditDialog(null);
            if (dialog.ShowDialog() != DialogResult.OK) return;
            _ = SaveNewAsync(dialog.Client);
        }

        private async Task SaveNewAsync(Client client)
        {
            try
            {
                string id = $"client_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                client.Id        = id;
                client.CreatedAt = DateTime.UtcNow;
                await FirebaseService.Instance.SaveAsync("clients", id, client);
                await RefreshListAsync();
                ToastNotification.Show("Client added!");
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError($"Failed to add client: {ex.Message}");
            }
        }

        public void EditSelected()
        {
            var client = GetSelected();
            if (client == null) { ToastNotification.Show("Select a client to edit."); return; }

            var dialog = new ClientEditDialog(client);
            if (dialog.ShowDialog() != DialogResult.OK) return;
            _ = UpdateAsync(client.Id, dialog.Client);
        }

        private async Task UpdateAsync(string id, Client client)
        {
            try
            {
                await FirebaseService.Instance.SaveAsync("clients", id, client);
                await RefreshListAsync();
                ToastNotification.Show("Client updated!");
            }
            catch (Exception ex) { ToastNotification.ShowError(ex.Message); }
        }

        public void DeleteSelected()
        {
            var client = GetSelected();
            if (client == null) return;

            var confirm = MessageBox.Show($"Delete client \"{client.Name}\"?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            _ = DeleteAsync(client.Id);
        }

        private async Task DeleteAsync(string id)
        {
            try
            {
                await FirebaseService.Instance.DeleteAsync("clients", id);
                await RefreshListAsync();
                ToastNotification.Show("Client deleted.");
            }
            catch (Exception ex) { ToastNotification.ShowError(ex.Message); }
        }

        public void ExportData()
        {
            var save = new SaveFileDialog
            {
                FileName   = $"clients_{DateTime.Now:yyyyMMdd}",
                Filter     = "CSV Files (*.csv)|*.csv",
                DefaultExt = "csv"
            };
            if (save.ShowDialog() != DialogResult.OK) return;

            try
            {
                CsvExporter.Export(_filtered.Count > 0 ? _filtered : _allClients, save.FileName);
                ToastNotification.Show("Clients exported to CSV!");
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError($"Export failed: {ex.Message}");
            }
        }

        public new void Refresh() => _ = RefreshListAsync();
        public void FocusSearch()  => _searchBox.Focus();
    }

    // ─── Client edit dialog ────────────────────────────────────────────────────

    public class ClientEditDialog : Form
    {
        public Client Client { get; private set; }

        private TextBox _nameBox, _emailBox, _phoneBox, _addressBox, _notesBox;

        public ClientEditDialog(Client existing)
        {
            Client = existing ?? new Client();
            BuildUI();
            if (existing != null) FillFields(existing);
        }

        private void BuildUI()
        {
            Text            = Client.Id == null ? "Add Client" : "Edit Client";
            Size            = new Size(440, 380);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = BotanikaColors.Offwhite;

            int x = 20, lw = 100, fw = 270, y = 16, rh = 32;

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

            _nameBox    = new TextBox(); BotanikaTheme.StyleTextBox(_nameBox);    Row("Name *",    _nameBox);
            _emailBox   = new TextBox(); BotanikaTheme.StyleTextBox(_emailBox);   Row("Email",     _emailBox);
            _phoneBox   = new TextBox(); BotanikaTheme.StyleTextBox(_phoneBox);   Row("Phone",     _phoneBox);
            _addressBox = new TextBox(); BotanikaTheme.StyleTextBox(_addressBox); Row("Address",   _addressBox);
            _notesBox   = new TextBox
            {
                Multiline = true, Size = new Size(fw, 56),
                Location  = new Point(x + lw + 8, y), ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle, Font = BotanikaFonts.Body(9.5f)
            };
            Controls.Add(new Label
            {
                Text = "Notes", Font = BotanikaFonts.Body(9f), ForeColor = BotanikaColors.TextLight,
                Size = new Size(lw, 24), Location = new Point(x, y + 4)
            });
            Controls.Add(_notesBox);
            y += 64;

            var saveBtn = new Button
            {
                Text = "Save", Size = new Size(100, 34),
                Location = new Point(x + lw + 8 + 90, y + 8),
                FlatStyle = FlatStyle.Flat, BackColor = BotanikaColors.Primary,
                ForeColor = Color.White, Cursor = Cursors.Hand,
                Font = BotanikaFonts.Body(9.5f, FontStyle.Bold)
            };
            saveBtn.FlatAppearance.BorderSize = 0;
            saveBtn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_nameBox.Text))
                { MessageBox.Show("Name is required."); return; }
                Client.Name    = _nameBox.Text.Trim();
                Client.Email   = _emailBox.Text.Trim();
                Client.Phone   = _phoneBox.Text.Trim();
                Client.Address = _addressBox.Text.Trim();
                Client.Notes   = _notesBox.Text.Trim();
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
            ClientSize = new Size(420, y + 60);
            AcceptButton = saveBtn;
            CancelButton = cancelBtn;
        }

        private void FillFields(Client c)
        {
            _nameBox.Text    = c.Name    ?? "";
            _emailBox.Text   = c.Email   ?? "";
            _phoneBox.Text   = c.Phone   ?? "";
            _addressBox.Text = c.Address ?? "";
            _notesBox.Text   = c.Notes   ?? "";
        }
    }
}
