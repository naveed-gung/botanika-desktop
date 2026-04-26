using System;
using System.Drawing;
using System.Windows.Forms;
using Botanika_Desktop.Firebase.Models;
using Botanika_Desktop.Theme;

namespace Botanika_Desktop.Forms
{
    // Add/Edit dialog for a single product.
    // Pre-fills all fields when editing, leaves them blank when adding.
    public class ProductEditDialog : Form
    {
        // The product that was built or edited — read this after ShowDialog() == OK
        public Product Product { get; private set; }

        // ─── Form controls ─────────────────────────────────────────────────────
        private TextBox   _nameBox;
        private ComboBox  _categoryBox;
        private TextBox   _priceBox;
        private TextBox   _stockBox;
        private CheckBox  _featuredCheck;
        private TextBox   _descBox;
        private TextBox   _imageUrlBox;
        private ComboBox  _careLevelBox;
        private Button    _saveBtn;
        private Button    _cancelBtn;

        public ProductEditDialog(Product existing)
        {
            Product = existing ?? new Product();
            BuildUI();

            // Pre-fill all fields if we're editing
            if (existing != null)
                FillFields(existing);
        }

        private void BuildUI()
        {
            Text            = Product.Id == null ? "Add Product" : "Edit Product";
            Size            = new Size(480, 520);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = BotanikaColors.Offwhite;

            int x = 24, labelW = 120, fieldW = 300, rowH = 32, y = 20;

            // Helper to add a labeled field row
            void AddRow(string labelText, Control field)
            {
                var lbl = new Label
                {
                    Text      = labelText,
                    Font      = BotanikaFonts.Body(9f),
                    ForeColor = BotanikaColors.TextLight,
                    Size      = new Size(labelW, rowH),
                    Location  = new Point(x, y + 4)
                };
                field.Size     = new Size(fieldW, rowH);
                field.Location = new Point(x + labelW + 8, y);
                Controls.Add(lbl);
                Controls.Add(field);
                y += rowH + 8;
            }

            // ── Product Name ───────────────────────────────────────────────────
            _nameBox = new TextBox { Font = BotanikaFonts.Body(10f) };
            BotanikaTheme.StyleTextBox(_nameBox);
            AddRow("Product Name *", _nameBox);

            // ── Category ───────────────────────────────────────────────────────
            _categoryBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            _categoryBox.Items.AddRange(new object[]
                { "Tropical", "Succulents", "Herbs", "Flowers", "Trees", "Other" });
            BotanikaTheme.StyleComboBox(_categoryBox);
            AddRow("Category", _categoryBox);

            // ── Price ──────────────────────────────────────────────────────────
            _priceBox = new TextBox { Font = BotanikaFonts.Body(10f) };
            BotanikaTheme.StyleTextBox(_priceBox);
            AddRow("Price ($) *", _priceBox);

            // ── Stock ──────────────────────────────────────────────────────────
            _stockBox = new TextBox { Font = BotanikaFonts.Body(10f) };
            BotanikaTheme.StyleTextBox(_stockBox);
            AddRow("Stock Qty *", _stockBox);

            // ── Care Level ─────────────────────────────────────────────────────
            _careLevelBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            _careLevelBox.Items.AddRange(new object[] { "Easy", "Medium", "Hard" });
            BotanikaTheme.StyleComboBox(_careLevelBox);
            AddRow("Care Level", _careLevelBox);

            // ── Featured checkbox ──────────────────────────────────────────────
            _featuredCheck = new CheckBox
            {
                Text   = "Show in Featured section on homepage",
                Font   = BotanikaFonts.Body(9.5f),
                Size   = new Size(340, 24),
                Location = new Point(x + labelW + 8, y),
                ForeColor = BotanikaColors.Charcoal
            };
            Controls.Add(_featuredCheck);
            y += 32;

            // ── Image URL ──────────────────────────────────────────────────────
            _imageUrlBox = new TextBox { Font = BotanikaFonts.Body(10f) };
            BotanikaTheme.StyleTextBox(_imageUrlBox);
            AddRow("Image URL", _imageUrlBox);

            // ── Description (multiline) ────────────────────────────────────────
            var descLabel = new Label
            {
                Text      = "Description",
                Font      = BotanikaFonts.Body(9f),
                ForeColor = BotanikaColors.TextLight,
                Size      = new Size(labelW, 24),
                Location  = new Point(x, y + 4)
            };
            _descBox = new TextBox
            {
                Multiline   = true,
                Font        = BotanikaFonts.Body(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Size        = new Size(fieldW, 72),
                Location    = new Point(x + labelW + 8, y),
                ScrollBars  = ScrollBars.Vertical
            };
            Controls.Add(descLabel);
            Controls.Add(_descBox);
            y += 80;

            // ── Action buttons ─────────────────────────────────────────────────
            y += 8;
            _cancelBtn = new Button
            {
                Text      = "Cancel",
                Size      = new Size(90, 34),
                Location  = new Point(x + labelW + 8, y),
                FlatStyle = FlatStyle.Flat,
                BackColor = BotanikaColors.SandLight,
                ForeColor = BotanikaColors.Charcoal,
                Cursor    = Cursors.Hand
            };
            _cancelBtn.FlatAppearance.BorderSize = 0;
            _cancelBtn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            _saveBtn = new Button
            {
                Text      = "Save Product",
                Size      = new Size(120, 34),
                Location  = new Point(x + labelW + 8 + 100, y),
                FlatStyle = FlatStyle.Flat,
                BackColor = BotanikaColors.Primary,
                ForeColor = Color.White,
                Cursor    = Cursors.Hand,
                Font      = BotanikaFonts.Body(9.5f, FontStyle.Bold)
            };
            _saveBtn.FlatAppearance.BorderSize = 0;
            _saveBtn.Click += SaveButton_Click;

            Controls.Add(_cancelBtn);
            Controls.Add(_saveBtn);

            // Adjust form height to fit everything
            ClientSize = new Size(460, y + 60);

            // Allow Enter key to save
            AcceptButton = _saveBtn;
            CancelButton = _cancelBtn;
        }

        // Pre-fills the form fields with data from an existing product
        private void FillFields(Product p)
        {
            _nameBox.Text     = p.Name ?? "";
            _priceBox.Text    = p.Price.ToString("F2");
            _stockBox.Text    = p.Stock.ToString();
            _descBox.Text     = p.Description ?? "";
            _imageUrlBox.Text = p.ImageUrl ?? "";
            _featuredCheck.Checked = p.Featured;

            // Select matching category in dropdown
            if (!string.IsNullOrEmpty(p.Category))
                _categoryBox.SelectedItem = p.Category;

            if (!string.IsNullOrEmpty(p.CareLevel))
                _careLevelBox.SelectedItem = p.CareLevel;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // Validate required fields before saving
            if (string.IsNullOrWhiteSpace(_nameBox.Text))
            {
                MessageBox.Show("Product name is required.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _nameBox.Focus();
                return;
            }

            if (!double.TryParse(_priceBox.Text, out double price) || price < 0)
            {
                MessageBox.Show("Please enter a valid price (e.g. 12.99).", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _priceBox.Focus();
                return;
            }

            if (!int.TryParse(_stockBox.Text, out int stock) || stock < 0)
            {
                MessageBox.Show("Please enter a valid stock quantity.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _stockBox.Focus();
                return;
            }

            // Build the updated product object
            Product.Name        = _nameBox.Text.Trim();
            Product.Category    = _categoryBox.SelectedItem?.ToString() ?? "";
            Product.Price       = price;
            Product.Stock       = stock;
            Product.Featured    = _featuredCheck.Checked;
            Product.Description = _descBox.Text.Trim();
            Product.ImageUrl    = _imageUrlBox.Text.Trim();
            Product.CareLevel   = _careLevelBox.SelectedItem?.ToString() ?? "";

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
