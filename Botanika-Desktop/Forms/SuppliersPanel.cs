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
    // Supplier management — who we buy from and their contact details.
    // Follows the same CRUD + export pattern as the other data panels.
    public class SuppliersPanel : UserControl, IRefreshable, ICrudPanel, ISearchable, IExportable
    {
        private BotanikaListView _listView;
        private TextBox          _searchBox;
        private ComboBox         _categoryFilter;
        private Label            _countLabel;
        private List<Supplier>   _allSuppliers = new List<Supplier>();
        private List<Supplier>   _filtered     = new List<Supplier>();

        public SuppliersPanel()
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
                Text      = "Suppliers",
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
                Size        = new Size(200, 28),
                Location    = new Point(pad, 72),
                Font        = BotanikaFonts.Body(9.5f),
                ForeColor   = BotanikaColors.TextMuted,
                Text        = "Search suppliers..."
            };
            BotanikaTheme.StyleTextBox(_searchBox);
            _searchBox.GotFocus  += (s2, e2) => { if (_searchBox.Text == "Search suppliers...") { _searchBox.Text = ""; _searchBox.ForeColor = BotanikaColors.Charcoal; } };
            _searchBox.LostFocus += (s2, e2) => { if (string.IsNullOrEmpty(_searchBox.Text)) { _searchBox.Text = "Search suppliers..."; _searchBox.ForeColor = BotanikaColors.TextMuted; } };
            _searchBox.TextChanged += (s, e) => ApplyFilter();

            _categoryFilter = new ComboBox
            {
                Size          = new Size(150, 28),
                Location      = new Point(pad + 230, 72),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = BotanikaFonts.Body(9.5f)
            };
            BotanikaTheme.StyleComboBox(_categoryFilter);
            _categoryFilter.Items.AddRange(new object[]
                { "All Categories", "Plants", "Flowers", "Herbs", "Seeds", "Mixed" });
            _categoryFilter.SelectedIndex = 0;
            _categoryFilter.SelectedIndexChanged += (s, e) => ApplyFilter();

            var addBtn = new Button
            {
                Text      = "+ Add Supplier",
                Size      = new Size(130, 32),
                Location  = new Point(pad + 680, 68)
            };
            BotanikaTheme.StyleButton(addBtn);
            addBtn.Click += (s, e) => AddNew();

            var exportBtn = new Button
            {
                Text      = "⬇ Export CSV",
                Size      = new Size(110, 32),
                Location  = new Point(pad + 560, 68)
            };
            BotanikaTheme.StyleSecondaryButton(exportBtn);
            exportBtn.Click += (s, e) => ExportData();

            _listView = new BotanikaListView
            {
                Location = new Point(pad, 112),
                Size     = new Size(900, 300),
                Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _listView.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "Company",        Width = 180 },
                new ColumnHeader { Text = "Contact Person", Width = 140 },
                new ColumnHeader { Text = "Email",          Width = 180 },
                new ColumnHeader { Text = "Category",       Width = 100 },
                new ColumnHeader { Text = "Country",        Width = 100 },
                new ColumnHeader { Text = "Status",         Width = 80  },
                new ColumnHeader { Text = "ID",             Width = 0   },
            });

            var ctx = new ContextMenuStrip();
            ctx.Items.Add("✏ Edit",   null, (s, e) => EditSelected());
            ctx.Items.Add("🗑 Delete", null, (s, e) => DeleteSelected());
            _listView.ContextMenuStrip = ctx;
            _listView.DoubleClick     += (s, e) => EditSelected();

            Controls.AddRange(new Control[]
            {
                header, _countLabel, _searchBox, _categoryFilter, addBtn, exportBtn, _listView
            });
        }

        public async Task RefreshListAsync()
        {
            try
            {
                _countLabel.Text = "Loading...";
                _allSuppliers = await FirebaseService.Instance.GetAllAsync<Supplier>("suppliers");

                // Seed default suppliers if collection is empty
                if (_allSuppliers.Count == 0)
                {
                    await SeedDefaultSuppliersAsync();
                    _allSuppliers = await FirebaseService.Instance.GetAllAsync<Supplier>("suppliers");
                }

                ApplyFilter();
                _listView.AutoFitHeight();
                _countLabel.Text = $"{_allSuppliers.Count} supplier(s)";
            }
            catch (Exception ex)
            {
                _countLabel.Text = "Load failed";
                ToastNotification.ShowError($"Failed to load suppliers: {ex.Message}");
            }
        }

        private async Task SeedDefaultSuppliersAsync()
        {
            var seeds = new[]
            {
                new Supplier { Name = "Green Valley Nursery", ContactPerson = "Hassan Ali", Email = "hassan@greenvalley.com", Phone = "+961-71-234567", Category = "Plants", Country = "Lebanon", Active = true, Notes = "Reliable indoor plant supplier, 2-week delivery" },
                new Supplier { Name = "Bloom & Root Co.", ContactPerson = "Sara Mansour", Email = "sara@bloomroot.com", Phone = "+961-76-543210", Category = "Flowers", Country = "Lebanon", Active = true, Notes = "Premium flower arrangements, seasonal availability" },
                new Supplier { Name = "TerraHerbs International", ContactPerson = "Ahmed Khoury", Email = "ahmed@terraherbs.com", Phone = "+33-1-44556677", Category = "Herbs", Country = "France", Active = true, Notes = "Organic herb seeds, EU certified" },
                new Supplier { Name = "Desert Succulents Ltd", ContactPerson = "Layla Nasser", Email = "layla@desertsucculents.com", Phone = "+971-50-1234567", Category = "Plants", Country = "UAE", Active = true, Notes = "Exotic succulents and cacti, heat-resistant varieties" },
                new Supplier { Name = "Pacific Seeds Corp", ContactPerson = "James Chen", Email = "james@pacificseeds.com", Phone = "+1-415-5551234", Category = "Seeds", Country = "USA", Active = false, Notes = "Currently on hold — shipping delays" },
            };
            foreach (var s in seeds)
            {
                string id = $"sup_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Array.IndexOf(seeds, s)}";
                s.Id = id;
                await FirebaseService.Instance.SaveAsync("suppliers", id, s);
            }
        }

        private void ApplyFilter()
        {
            string search   = _searchBox.Text.ToLowerInvariant();
            string category = _categoryFilter.SelectedItem?.ToString();
            bool   allCats  = category == "All Categories";

            string rawSup = _searchBox.Text == "Search suppliers..." ? "" : _searchBox.Text;
            search = rawSup.ToLowerInvariant();
            _filtered = _allSuppliers.Where(s =>
                (string.IsNullOrEmpty(search) ||
                    (s.Name?.ToLowerInvariant().Contains(search) == true) ||
                    (s.ContactPerson?.ToLowerInvariant().Contains(search) == true))
                && (allCats || s.Category == category)).ToList();

            _listView.Items.Clear();
            foreach (var s in _filtered)
            {
                var item = new ListViewItem(s.Name ?? "");
                item.SubItems.Add(s.ContactPerson ?? "");
                item.SubItems.Add(s.Email ?? "");
                item.SubItems.Add(s.Category ?? "");
                item.SubItems.Add(s.Country ?? "");
                item.SubItems.Add(s.ActiveDisplay);
                item.SubItems.Add(s.Id ?? "");
                item.Tag = s;
                _listView.Items.Add(item);
            }
        }

        private Supplier GetSelected()
        {
            if (_listView.SelectedItems.Count == 0) return null;
            return _listView.SelectedItems[0].Tag as Supplier;
        }

        public void AddNew()
        {
            var dlg = new SupplierEditDialog(null);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            _ = SaveNewAsync(dlg.Supplier);
        }

        private async Task SaveNewAsync(Supplier s)
        {
            try
            {
                string id = $"sup_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                s.Id = id;
                await FirebaseService.Instance.SaveAsync("suppliers", id, s);
                await RefreshListAsync();
                ToastNotification.Show("Supplier added!");
            }
            catch (Exception ex) { ToastNotification.ShowError(ex.Message); }
        }

        public void EditSelected()
        {
            var s = GetSelected();
            if (s == null) { ToastNotification.Show("Select a supplier to edit."); return; }
            var dlg = new SupplierEditDialog(s);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            _ = UpdateAsync(s.Id, dlg.Supplier);
        }

        private async Task UpdateAsync(string id, Supplier s)
        {
            try
            {
                await FirebaseService.Instance.SaveAsync("suppliers", id, s);
                await RefreshListAsync();
                ToastNotification.Show("Supplier updated!");
            }
            catch (Exception ex) { ToastNotification.ShowError(ex.Message); }
        }

        public void DeleteSelected()
        {
            var s = GetSelected();
            if (s == null) return;
            var confirm = MessageBox.Show($"Delete \"{s.Name}\"?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;
            _ = DeleteAsync(s.Id);
        }

        private async Task DeleteAsync(string id)
        {
            try
            {
                await FirebaseService.Instance.DeleteAsync("suppliers", id);
                await RefreshListAsync();
                ToastNotification.Show("Supplier deleted.");
            }
            catch (Exception ex) { ToastNotification.ShowError(ex.Message); }
        }

        public void ExportData()
        {
            var save = new SaveFileDialog
            {
                FileName   = $"suppliers_{DateTime.Now:yyyyMMdd}",
                Filter     = "CSV Files (*.csv)|*.csv",
                DefaultExt = "csv"
            };
            if (save.ShowDialog() != DialogResult.OK) return;
            try
            {
                CsvExporter.Export(_filtered.Count > 0 ? _filtered : _allSuppliers, save.FileName);
                ToastNotification.Show("Suppliers exported!");
            }
            catch (Exception ex) { ToastNotification.ShowError(ex.Message); }
        }

        public new void Refresh() => _ = RefreshListAsync();
        public void FocusSearch()  => _searchBox.Focus();
    }

    // ─── Supplier edit dialog ──────────────────────────────────────────────────

    public class SupplierEditDialog : Form
    {
        public Supplier Supplier { get; private set; }
        private TextBox  _nameBox, _contactBox, _emailBox, _phoneBox, _countryBox, _notesBox;
        private ComboBox _categoryBox;
        private CheckBox _activeCheck;

        public SupplierEditDialog(Supplier existing)
        {
            Supplier = existing ?? new Supplier { Active = true };
            BuildUI();
            if (existing != null) FillFields(existing);
        }

        private void BuildUI()
        {
            Text            = Supplier.Id == null ? "Add Supplier" : "Edit Supplier";
            Size            = new Size(440, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = BotanikaColors.Offwhite;

            int x = 20, lw = 110, fw = 260, y = 16, rh = 32;

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

            _nameBox    = new TextBox(); BotanikaTheme.StyleTextBox(_nameBox);    Row("Company *",  _nameBox);
            _contactBox = new TextBox(); BotanikaTheme.StyleTextBox(_contactBox); Row("Contact",    _contactBox);
            _emailBox   = new TextBox(); BotanikaTheme.StyleTextBox(_emailBox);   Row("Email",      _emailBox);
            _phoneBox   = new TextBox(); BotanikaTheme.StyleTextBox(_phoneBox);   Row("Phone",      _phoneBox);
            _countryBox = new TextBox(); BotanikaTheme.StyleTextBox(_countryBox); Row("Country",    _countryBox);

            _categoryBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            _categoryBox.Items.AddRange(new object[] { "Plants", "Flowers", "Herbs", "Seeds", "Mixed" });
            BotanikaTheme.StyleComboBox(_categoryBox);
            Row("Category", _categoryBox);

            _activeCheck = new CheckBox
            {
                Text = "Active supplier", Font = BotanikaFonts.Body(9.5f),
                Size = new Size(240, 24), Location = new Point(x + lw + 8, y),
                ForeColor = BotanikaColors.Charcoal, Checked = true
            };
            Controls.Add(_activeCheck);
            y += 32;

            _notesBox = new TextBox
            {
                Multiline = true, Size = new Size(fw, 48),
                Location = new Point(x + lw + 8, y), ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle, Font = BotanikaFonts.Body(9.5f)
            };
            Controls.Add(new Label
            {
                Text = "Notes", Font = BotanikaFonts.Body(9f), ForeColor = BotanikaColors.TextLight,
                Size = new Size(lw, 24), Location = new Point(x, y + 4)
            });
            Controls.Add(_notesBox);
            y += 56;

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
                if (string.IsNullOrWhiteSpace(_nameBox.Text))
                { MessageBox.Show("Company name is required."); return; }
                Supplier.Name          = _nameBox.Text.Trim();
                Supplier.ContactPerson = _contactBox.Text.Trim();
                Supplier.Email         = _emailBox.Text.Trim();
                Supplier.Phone         = _phoneBox.Text.Trim();
                Supplier.Country       = _countryBox.Text.Trim();
                Supplier.Category      = _categoryBox.SelectedItem?.ToString() ?? "";
                Supplier.Notes         = _notesBox.Text.Trim();
                Supplier.Active        = _activeCheck.Checked;
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

        private void FillFields(Supplier s)
        {
            _nameBox.Text    = s.Name          ?? "";
            _contactBox.Text = s.ContactPerson ?? "";
            _emailBox.Text   = s.Email         ?? "";
            _phoneBox.Text   = s.Phone         ?? "";
            _countryBox.Text = s.Country       ?? "";
            _notesBox.Text   = s.Notes         ?? "";
            _activeCheck.Checked = s.Active;
            if (!string.IsNullOrEmpty(s.Category))
                _categoryBox.SelectedItem = s.Category;
        }
    }
}
