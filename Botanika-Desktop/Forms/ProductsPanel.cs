using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
    // The main product management panel — full CRUD + all 5 export formats + import.
    // This is the most feature-rich panel in the app and the core required feature.
    public class ProductsPanel : UserControl, IRefreshable, ICrudPanel, ISearchable, IExportable
    {
        private BotanikaListView _listView;
        private TextBox          _searchBox;
        private ComboBox         _categoryFilter;
        private Label            _countLabel;

        // Local cache — we reload from Firestore on every mutating operation
        private List<Product> _allProducts = new List<Product>();
        private List<Product> _filtered    = new List<Product>();

        public ProductsPanel()
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

            // ── Page header ────────────────────────────────────────────────────
            var header = new Label
            {
                Text     = "Products",
                Font     = BotanikaFonts.Heading(18f, FontStyle.Bold),
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

            // ── Search box ─────────────────────────────────────────────────────
            _searchBox = new TextBox
            {
                Size        = new Size(220, 28),
                Location    = new Point(pad, 72),
                Font        = BotanikaFonts.Body(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor   = BotanikaColors.TextMuted,
                Text        = "Search products..."
            };
            // Simulate placeholder behaviour (PlaceholderText not available in .NET 4.7.2)
            _searchBox.GotFocus  += (s, e) => { if (_searchBox.Text == "Search products...") { _searchBox.Text = ""; _searchBox.ForeColor = BotanikaColors.Charcoal; } };
            _searchBox.LostFocus += (s, e) => { if (string.IsNullOrEmpty(_searchBox.Text)) { _searchBox.Text = "Search products..."; _searchBox.ForeColor = BotanikaColors.TextMuted; } };
            _searchBox.TextChanged += (s, e) => ApplyFilters();

            // ── Category filter dropdown ───────────────────────────────────────
            _categoryFilter = new ComboBox
            {
                Size         = new Size(150, 28),
                Location     = new Point(pad + 230, 72),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font         = BotanikaFonts.Body(9.5f)
            };
            _categoryFilter.Items.AddRange(new object[]
                { "All Categories", "Tropical", "Succulents", "Herbs", "Flowers", "Trees", "Other" });
            _categoryFilter.SelectedIndex = 0;
            _categoryFilter.SelectedIndexChanged += (s, e) => ApplyFilters();

            // ── Toolbar buttons — right-aligned ────────────────────────────────
            var addBtn = new Button
            {
                Text      = "+ Add Product",
                Size      = new Size(120, 32),
                Location  = new Point(pad + 700, 68),
                Font      = BotanikaFonts.Body(9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = BotanikaColors.Primary,
                ForeColor = Color.White,
                Cursor    = Cursors.Hand
            };
            addBtn.FlatAppearance.BorderSize = 0;
            addBtn.Click += (s, e) => AddNew();

            var importBtn = new Button
            {
                Text      = "⬆ Import",
                Size      = new Size(90, 32),
                Location  = new Point(pad + 600, 68),
                Font      = BotanikaFonts.Body(9.5f),
                FlatStyle = FlatStyle.Flat,
                BackColor = BotanikaColors.SandLight,
                ForeColor = BotanikaColors.Charcoal,
                Cursor    = Cursors.Hand
            };
            importBtn.FlatAppearance.BorderColor = BotanikaColors.Sand;
            importBtn.FlatAppearance.BorderSize  = 1;
            importBtn.Click += (s, e) => ImportProducts();

            // Export dropdown button — shows all 5 format options
            var exportBtn = new Button
            {
                Text      = "⬇ Export ▾",
                Size      = new Size(100, 32),
                Location  = new Point(pad + 490, 68),
                Font      = BotanikaFonts.Body(9.5f),
                FlatStyle = FlatStyle.Flat,
                BackColor = BotanikaColors.SandLight,
                ForeColor = BotanikaColors.Charcoal,
                Cursor    = Cursors.Hand
            };
            exportBtn.FlatAppearance.BorderColor = BotanikaColors.Sand;
            exportBtn.FlatAppearance.BorderSize  = 1;
            exportBtn.Click += (s, e) => ShowExportMenu(exportBtn);

            // ── ListView ───────────────────────────────────────────────────────
            _listView = new BotanikaListView
            {
                Location = new Point(pad, 112),
                Size     = new Size(900, 300),
                Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _listView.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "Name",     Width = 200 },
                new ColumnHeader { Text = "Category", Width = 120 },
                new ColumnHeader { Text = "Price",    Width = 90  },
                new ColumnHeader { Text = "Stock",    Width = 90  },
                new ColumnHeader { Text = "Featured", Width = 80  },
                new ColumnHeader { Text = "ID",       Width = 0   }, // hidden — we need it for edit/delete
            });

            // Right-click context menu on list rows
            var ctxMenu = new ContextMenuStrip();
            ctxMenu.Items.Add("✏ Edit",   null, (s, e) => EditSelected());
            ctxMenu.Items.Add("🗑 Delete", null, (s, e) => DeleteSelected());
            _listView.ContextMenuStrip = ctxMenu;

            // Double-click to edit
            _listView.DoubleClick += (s, e) => EditSelected();

            Controls.AddRange(new Control[]
            {
                header, _countLabel,
                _searchBox, _categoryFilter,
                exportBtn, importBtn, addBtn,
                _listView
            });
        }

        // ─── Data Loading ──────────────────────────────────────────────────────

        // Reloads all products from Firestore and refreshes the list
        public async Task RefreshListAsync()
        {
            try
            {
                _countLabel.Text = "Loading...";
                _allProducts = await FirebaseService.Instance.GetAllAsync<Product>("products");
                ApplyFilters();
                _listView.AutoFitHeight();
                _countLabel.Text = $"{_allProducts.Count} product(s)";
            }
            catch (Exception ex)
            {
                _countLabel.Text = "Failed to load products";
                ToastNotification.ShowError($"Load failed: {ex.Message}");
            }
        }

        // Applies the current search text and category filter to the local cache
        private void ApplyFilters()
        {
            string rawSearch = _searchBox.Text;
            string search   = (rawSearch == "Search products..." ? "" : rawSearch).ToLowerInvariant();
            string category = _categoryFilter.SelectedItem?.ToString();
            bool   allCats  = category == "All Categories" || string.IsNullOrEmpty(category);

            _filtered = _allProducts
                .Where(p =>
                    (string.IsNullOrEmpty(search) || p.Name?.ToLowerInvariant().Contains(search) == true)
                    && (allCats || p.Category == category))
                .ToList();

            PopulateListView(_filtered);
        }

        private void PopulateListView(List<Product> products)
        {
            _listView.Items.Clear();
            foreach (var p in products)
            {
                var item = new ListViewItem(p.Name ?? "(unnamed)");
                item.SubItems.Add(p.Category ?? "");
                item.SubItems.Add($"${p.Price:F2}");
                item.SubItems.Add(p.StockStatus);
                item.SubItems.Add(p.FeaturedDisplay);
                item.SubItems.Add(p.Id ?? "");  // hidden ID column for operations
                item.Tag = p; // store the full object so we don't have to look it up
                _listView.Items.Add(item);
            }
        }

        // Gets the currently selected product, or null if nothing is selected
        private Product GetSelectedProduct()
        {
            if (_listView.SelectedItems.Count == 0) return null;
            return _listView.SelectedItems[0].Tag as Product;
        }

        // ─── CRUD Operations ───────────────────────────────────────────────────

        public void AddNew()
        {
            var dialog = new ProductEditDialog(null);
            if (dialog.ShowDialog() != DialogResult.OK) return;

            _ = SaveNewProductAsync(dialog.Product);
        }

        private async Task SaveNewProductAsync(Product product)
        {
            try
            {
                // Generate a unique ID the same way the website does it
                string id = $"prod_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                product.Id       = id;
                product.CreatedAt = DateTime.UtcNow;

                await FirebaseService.Instance.SaveAsync("products", id, product);
                await RefreshListAsync();
                ToastNotification.Show("Product added successfully!");
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError($"Failed to add product: {ex.Message}");
            }
        }

        public void EditSelected()
        {
            var product = GetSelectedProduct();
            if (product == null)
            {
                ToastNotification.Show("Please select a product to edit.");
                return;
            }

            var dialog = new ProductEditDialog(product);
            if (dialog.ShowDialog() != DialogResult.OK) return;

            _ = UpdateProductAsync(product.Id, dialog.Product);
        }

        private async Task UpdateProductAsync(string id, Product product)
        {
            try
            {
                await FirebaseService.Instance.SaveAsync("products", id, product);
                await RefreshListAsync();
                ToastNotification.Show("Product updated!");
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError($"Failed to update: {ex.Message}");
            }
        }

        public void DeleteSelected()
        {
            var product = GetSelectedProduct();
            if (product == null) return;

            var confirm = MessageBox.Show(
                $"Delete \"{product.Name}\"? This cannot be undone.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            _ = DeleteProductAsync(product.Id);
        }

        private async Task DeleteProductAsync(string id)
        {
            try
            {
                await FirebaseService.Instance.DeleteAsync("products", id);
                await RefreshListAsync();
                ToastNotification.Show("Product deleted.");
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError($"Failed to delete: {ex.Message}");
            }
        }

        // ─── Export ────────────────────────────────────────────────────────────

        // Shows the export format picker dropdown
        private void ShowExportMenu(Control anchor)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Excel (.xlsx)", null, (s, e) => ExportAs("xlsx"));
            menu.Items.Add("PDF (.pdf)",    null, (s, e) => ExportAs("pdf"));
            menu.Items.Add("Word (.docx)",  null, (s, e) => ExportAs("docx"));
            menu.Items.Add("Markdown (.md)", null, (s, e) => ExportAs("md"));
            menu.Items.Add("CSV (.csv)",    null, (s, e) => ExportAs("csv"));
            menu.Show(anchor, new Point(0, anchor.Height));
        }

        public void ExportData() => ShowExportMenu(_listView);

        private void ExportAs(string format)
        {
            var save = new SaveFileDialog
            {
                FileName    = $"products_{DateTime.Now:yyyyMMdd}",
                Filter      = GetFilterForFormat(format),
                DefaultExt  = format
            };

            if (save.ShowDialog() != DialogResult.OK) return;

            try
            {
                // Export the filtered list — what you see is what you export
                var data = _filtered.Count > 0 ? _filtered : _allProducts;

                switch (format)
                {
                    case "csv":  CsvExporter.Export(data, save.FileName);                          break;
                    case "md":   MarkdownExporter.Export(data, save.FileName, "Botanika Products"); break;
                    // Excel and Word/PDF need the optional packages — show a message if not available
                    default:
                        ExportFallbackToJson(data, save.FileName);
                        break;
                }

                ToastNotification.Show($"Exported to {format.ToUpperInvariant()} successfully!");
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError($"Export failed: {ex.Message}");
            }
        }

        // JSON fallback for formats that need extra NuGet packages
        private void ExportFallbackToJson(List<Product> data, string filePath)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath + ".json", json);
            ToastNotification.Show("Exported as JSON (add ClosedXML/QuestPDF packages for Excel/PDF).");
        }

        private string GetFilterForFormat(string format)
        {
            switch (format)
            {
                case "xlsx": return "Excel Files (*.xlsx)|*.xlsx";
                case "pdf":  return "PDF Files (*.pdf)|*.pdf";
                case "docx": return "Word Files (*.docx)|*.docx";
                case "md":   return "Markdown Files (*.md)|*.md";
                case "csv":  return "CSV Files (*.csv)|*.csv";
                default:     return "All Files (*.*)|*.*";
            }
        }

        // ─── Import ────────────────────────────────────────────────────────────

        private void ImportProducts()
        {
            var open = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title  = "Import Products from CSV"
            };

            if (open.ShowDialog() != DialogResult.OK) return;

            try
            {
                var rows = ImportHandler.ImportFromCsv(open.FileName);
                if (rows.Count == 0)
                {
                    ToastNotification.ShowError("No data found in file.");
                    return;
                }

                // Show a preview before committing
                var preview = MessageBox.Show(
                    $"Found {rows.Count} row(s) to import.\nProceed?",
                    "Import Preview",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (preview != DialogResult.Yes) return;

                _ = DoImportAsync(rows);
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError($"Import failed: {ex.Message}");
            }
        }

        private async Task DoImportAsync(List<System.Collections.Generic.Dictionary<string, string>> rows)
        {
            int success = 0;
            foreach (var row in rows)
            {
                try
                {
                    // Try to map CSV columns to our Product model
                    var product = new Product
                    {
                        Name        = DictGet(row, "Name"),
                        Category    = DictGet(row, "Category"),
                        Description = DictGet(row, "Description"),
                        CareLevel   = DictGet(row, "CareLevel"),
                        ImageUrl    = DictGet(row, "ImageUrl"),
                    };

                    double price; if (double.TryParse(DictGet(row, "Price"), out price)) product.Price = price;
                    int    stock; if (int.TryParse(DictGet(row, "Stock"),   out stock)) product.Stock = stock;
                    bool   feat;  if (bool.TryParse(DictGet(row, "Featured"), out feat)) product.Featured = feat;

                    // Use existing ID from CSV or generate a new one
                    string id = DictGet(row, "Id")
                                ?? $"prod_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{success}";
                    product.Id = id;

                    await FirebaseService.Instance.SaveAsync("products", id, product);
                    success++;
                }
                catch { /* skip bad rows silently */ }
            }

            await RefreshListAsync();
            ToastNotification.Show($"Imported {success}/{rows.Count} products.");
        }

        // ─── Interface implementations ─────────────────────────────────────────
        public new void Refresh() => _ = RefreshListAsync();
        public void FocusSearch()  => _searchBox.Focus();

        // .NET 4.7.2 doesn't have Dictionary.GetValueOrDefault — use this instead
        private static string DictGet(System.Collections.Generic.Dictionary<string, string> d, string key)
            => d.ContainsKey(key) ? d[key] : null;
    }
}
