using System;
using System.Drawing;
using System.Windows.Forms;
using Botanika_Desktop.Theme;

namespace Botanika_Desktop.Controls
{
    // A single navigation item in the sidebar.
    // Shows an emoji icon, a label, and highlights when active or hovered.
    public class SidebarItem : Panel
    {
        private Label _iconLabel;
        private Label _textLabel;
        private bool  _isActive;

        // Expose this so MainForm can check which section we navigate to
        public string SectionName { get; set; }

        // Raised when the user clicks this item
        public event EventHandler NavigateClicked;

        // Whether this item represents the currently active section
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                UpdateStyle();
            }
        }

        public SidebarItem(string icon, string text, string section)
        {
            SectionName = section;

            // Full width of the sidebar, fixed height for consistent spacing
            Size        = new Size(220, 44);
            Cursor      = Cursors.Hand;
            Padding     = new Padding(0);

            // Icon on the left
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", icon + ".png");
            if (!System.IO.File.Exists(iconPath)) iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Assets", icon + ".png");

            _iconLabel = new Label
            {
                Image = System.IO.File.Exists(iconPath) ? Image.FromFile(iconPath) : null,
                ImageAlign = ContentAlignment.MiddleCenter,
                Size      = new Size(44, 44),
                Location  = new Point(0, 0)
            };

            // Section name text
            _textLabel = new Label
            {
                Text      = text,
                Font      = BotanikaFonts.Body(10f),
                ForeColor = Color.White,
                Size      = new Size(176, 44),
                Location  = new Point(44, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Controls.Add(_iconLabel);
            Controls.Add(_textLabel);

            UpdateStyle();

            // Wire up click events on all children so anywhere you click fires Navigate
            Click         += OnItemClicked;
            _iconLabel.Click += OnItemClicked;
            _textLabel.Click += OnItemClicked;

            MouseEnter         += OnHoverEnter;
            MouseLeave         += OnHoverLeave;
            _iconLabel.MouseEnter += OnHoverEnter;
            _iconLabel.MouseLeave += OnHoverLeave;
            _textLabel.MouseEnter += OnHoverEnter;
            _textLabel.MouseLeave += OnHoverLeave;
        }

        private void OnItemClicked(object sender, EventArgs e)
        {
            NavigateClicked?.Invoke(this, EventArgs.Empty);
        }

        private void OnHoverEnter(object sender, EventArgs e)
        {
            if (!_isActive)
                BackColor = Color.FromArgb(60, 255, 255, 255);
        }

        private void OnHoverLeave(object sender, EventArgs e)
        {
            if (!_isActive)
                BackColor = BotanikaColors.Charcoal;
        }

        // Updates colors based on active state
        private void UpdateStyle()
        {
            BackColor = _isActive
                ? BotanikaColors.Primary
                : BotanikaColors.Charcoal;

            _iconLabel.BackColor = BackColor;
            _textLabel.BackColor = BackColor;

            // Active text is slightly brighter white for extra emphasis
            Color textColor = _isActive ? Color.White : Color.FromArgb(200, 255, 255, 255);
            _iconLabel.ForeColor = textColor;
            _textLabel.ForeColor = textColor;

            // Bold when active
            _textLabel.Font = _isActive
                ? BotanikaFonts.Body(10f, FontStyle.Bold)
                : BotanikaFonts.Body(10f);
        }
    }
}
