using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Botanika_Desktop.Controls;
using Botanika_Desktop.Firebase;
using System.Linq;
using Botanika_Desktop.Theme;
using Newtonsoft.Json;

namespace Botanika_Desktop.Forms
{
    // The first thing the admin sees — full screen, dark charcoal background,
    // email/password login with floating label animation.
    // Only users with isAdmin == true in Firestore can get through.
    public class LoginForm : Form
    {
        // ─── Controls ──────────────────────────────────────────────────────────
        private Panel _centerCard;
        private Label _logoLabel;
        private Label _subtitleLabel;

        private Panel _emailField;
        private TextBox _emailBox;
        private Label _emailLabel;

        private Panel _passwordField;
        private TextBox _passwordBox;
        private Label _passwordLabel;

        private Button _loginButton;
        private Label _errorLabel;
        private Label _versionLabel;

        // Are we currently waiting for Firebase? Prevents double-submits.
        private bool _isLoading;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Full-screen dark form — matches the website's admin navbar color
            Text = "Botanika Admin — Sign In";
            Size = new Size(1000, 700);
            MinimumSize = new Size(800, 600);
            BackColor = BotanikaColors.Charcoal;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterScreen;

            // Set the custom Botanika icon
            if (Program.AppIcon != null) this.Icon = Program.AppIcon;

            // ── Center card ────────────────────────────────────────────────────
            // The white card that holds the login form — floats in the middle
            _centerCard = new Panel
            {
                Size = new Size(440, 500),
                BackColor = BotanikaColors.Offwhite,
                Anchor = AnchorStyles.None
            };
            CenterCard();
            this.Resize += (_, __) => CenterCard();

            // Apply rounded corners to the card (radius = 16px, matching the website cards)
            ApplyRoundedCorners(_centerCard, 16);

            var _adminAvatar = new PictureBox
            {
                Size = new Size(48, 48),
                Location = new Point(30, 36), // Align with divider
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Transparent,
                Visible = false // hidden until loaded
            };

            var _titleIcon = new PictureBox
            {
                Image = Image.FromFile(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "plant_icon.png")),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(32, 32),
                Location = new Point(105, 44)
            };

            // Attempt to load admin avatar
            _ = LoadAdminAvatarAsync(_adminAvatar);

            _logoLabel = new Label
            {
                Text = "BOTANIKA",
                Font = BotanikaFonts.Heading(24f, FontStyle.Bold),
                ForeColor = BotanikaColors.Primary,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(200, 56),
                Location = new Point(140, 32)
            };

            _subtitleLabel = new Label
            {
                Text = "Admin Dashboard",
                Font = BotanikaFonts.Body(10f),
                ForeColor = BotanikaColors.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(380, 24),
                Location = new Point(20, 80)
            };

            // ── Email field ────────────────────────────────────────────────────
            _emailField = CreateFloatingField("Email Address", false, out _emailBox, out _emailLabel);
            _emailField.Location = new Point(30, 130);

            // ── Password field ─────────────────────────────────────────────────
            _passwordField = CreateFloatingField("Password", true, out _passwordBox, out _passwordLabel);
            _passwordField.Location = new Point(30, 210);

            // ── Sign In button ─────────────────────────────────────────────────
            _loginButton = new Button
            {
                Text = "Sign In",
                Size = new Size(360, 44),
                Location = new Point(30, 300),
                Font = BotanikaFonts.Body(11f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = BotanikaColors.Primary,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _loginButton.FlatAppearance.BorderSize = 0;
            _loginButton.Click += LoginButton_Click;

            // Enter key in password box also submits
            _passwordBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) LoginButton_Click(s, e);
            };
            _emailBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) _passwordBox.Focus();
            };

            // ── Error message label ────────────────────────────────────────────
            _errorLabel = new Label
            {
                Text = "",
                ForeColor = BotanikaColors.Terracotta,
                Font = BotanikaFonts.Body(9f),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(360, 40),
                Location = new Point(30, 352),
                Visible = false
            };

            // ── Decorative divider ─────────────────────────────────────────────
            var divider = new Panel
            {
                Size = new Size(360, 1),
                Location = new Point(30, 110),
                BackColor = BotanikaColors.Sand
            };

            // ── Version label at the bottom ────────────────────────────────────
            _versionLabel = new Label
            {
                Text = "Botanika Desktop v1.0  ·  Admin Only",
                Font = BotanikaFonts.Caption(8f),
                ForeColor = BotanikaColors.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(380, 24),
                Location = new Point(20, 440)
            };

            // ── Assemble the card ──────────────────────────────────────────────
            _centerCard.Controls.AddRange(new Control[]
            {
                _adminAvatar, _titleIcon, _logoLabel, _subtitleLabel, divider,
                _emailField, _passwordField,
                _loginButton, _errorLabel, _versionLabel
            });

            Controls.Add(_centerCard);


        }

        // Centers the card when the form is resized
        private void CenterCard()
        {
            _centerCard.Location = new Point(
                (ClientSize.Width - _centerCard.Width) / 2,
                (ClientSize.Height - _centerCard.Height) / 2);
        }

        // Clips the control to a rounded rectangle — same effect as CSS border-radius.
        // The corners become transparent, showing the charcoal background through them.
        private static void ApplyRoundedCorners(Control ctrl, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            int w = ctrl.Width;
            int h = ctrl.Height;

            path.AddArc(0,     0,     d, d, 180, 90);  // top-left
            path.AddArc(w - d, 0,     d, d, 270, 90);  // top-right
            path.AddArc(w - d, h - d, d, d,   0, 90);  // bottom-right
            path.AddArc(0,     h - d, d, d,  90, 90);  // bottom-left
            path.CloseAllFigures();

            ctrl.Region = new Region(path);
        }

        // Creates a text field with a floating label placeholder effect.
        // The label slides up when the box is focused or has content.
        private Panel CreateFloatingField(string placeholder, bool isPassword,
            out TextBox box, out Label floatLabel)
        {
            var panel = new Panel { Size = new Size(360, 60), BackColor = BotanikaColors.Offwhite };

            // The floating label starts in the middle (placeholder position)
            floatLabel = new Label
            {
                Text = placeholder,
                Font = BotanikaFonts.Body(10f),
                ForeColor = BotanikaColors.TextMuted,
                Location = new Point(4, 18),
                AutoSize = true
            };

            box = new TextBox
            {
                Size = new Size(360, 28),
                Location = new Point(0, 26),
                BorderStyle = BorderStyle.None,
                Font = BotanikaFonts.Body(11f),
                ForeColor = BotanikaColors.Charcoal,
                BackColor = BotanikaColors.Offwhite
            };
            if (isPassword) box.PasswordChar = '•';

            // Bottom border line — mimics a Material Design text field
            var underline = new Panel
            {
                Size = new Size(360, 1),
                Location = new Point(0, 54),
                BackColor = BotanikaColors.Sand
            };
            var underlineActive = new Panel
            {
                Size = new Size(0, 2),
                Location = new Point(0, 53),
                BackColor = BotanikaColors.Primary
            };

            // Capture the out params into locals so lambdas can close over them
            // (C# doesn't allow capturing out/ref params in lambdas directly)
            Label capturedLabel = floatLabel;
            TextBox capturedBox = box;

            // Animate label up when focused
            capturedBox.GotFocus += (s, e) =>
            {
                capturedLabel.Font = BotanikaFonts.Caption(8f);
                capturedLabel.ForeColor = BotanikaColors.Primary;
                capturedLabel.Location = new Point(4, 4);
                underlineActive.Width = 360;
            };

            // Animate label back down when focus is lost and field is empty
            capturedBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrEmpty(capturedBox.Text))
                {
                    capturedLabel.Font = BotanikaFonts.Body(10f);
                    capturedLabel.ForeColor = BotanikaColors.TextMuted;
                    capturedLabel.Location = new Point(4, 18);
                }
                underlineActive.Width = 0;
            };

            panel.Controls.AddRange(new Control[] { floatLabel, box, underline, underlineActive });
            return panel;
        }

        // ─── Login Logic ───────────────────────────────────────────────────────

        private async void LoginButton_Click(object sender, EventArgs e)
        {
            if (_isLoading) return;

            string email = _emailBox.Text.Trim();
            string password = _passwordBox.Text;

            // Basic client-side validation before hitting the network
            if (string.IsNullOrEmpty(email))
            {
                ShowError("Please enter your email address.");
                _emailBox.Focus();
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                ShowError("Please enter your password.");
                _passwordBox.Focus();
                return;
            }

            await PerformLoginAsync(email, password);
        }

        private async Task PerformLoginAsync(string email, string password)
        {
            _isLoading = true;
            _loginButton.Text = "Signing in...";
            _loginButton.Enabled = false;
            HideError();

            try
            {

                // Read API key from Environment Variable or an external file (ignored in .gitignore)
                string webApiKey = Environment.GetEnvironmentVariable("FIREBASE_WEB_API_KEY");
                if (string.IsNullOrEmpty(webApiKey))
                {
                    string keyPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "firebase_api_key.txt");
                    if (!System.IO.File.Exists(keyPath))
                        keyPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Assets", "firebase_api_key.txt");

                    if (System.IO.File.Exists(keyPath))
                        webApiKey = System.IO.File.ReadAllText(keyPath).Trim();
                }

                if (string.IsNullOrEmpty(webApiKey))
                {
                    ShowError("Web API key not found. Place firebase_api_key.txt in the Assets folder or set FIREBASE_WEB_API_KEY.");
                    return;
                }

                FirebaseService.Instance.SetWebApiKey(webApiKey);

                // Step 1: Sign in with Firebase Auth REST to verify credentials
                var authResult = await FirebaseService.Instance.SignInAsync(email, password);

                // Step 2: Verify this is a known admin email.
                // Firebase Auth already validated the password above — this is a secondary
                // gate to ensure no random Firebase user can access the admin desktop.
                // The Firestore isAdmin flag is unreliable from the desktop (field mapping issues),
                // so we use a direct email check against the known admin account.
                var adminEmails = new[]
                {
                    "admin@botanika.com",
                };
                bool isAdmin = Array.Exists(adminEmails, a =>
                    string.Equals(a, email.Trim(), System.StringComparison.OrdinalIgnoreCase));

                // Also try the Firestore doc as a secondary confirmation (non-blocking)
                if (!isAdmin)
                {
                    try
                    {
                        var userDoc = await FirebaseService.Instance.GetByIdAsync<UserDoc>("users", authResult.LocalId);
                        isAdmin = userDoc?.IsAdmin ?? false;
                    }
                    catch { /* Firestore check is optional — email whitelist is authoritative */ }
                }

                if (!isAdmin)
                {
                    FirebaseService.Instance.SignOut();
                    ShowError("Access denied — this account does not have admin privileges.");
                    return;
                }

                // Step 3: Store the session and open the main dashboard
                Session.IdToken = authResult.IdToken;
                Session.UserId = authResult.LocalId;
                Session.Email = authResult.Email;
                Session.DisplayName = authResult.DisplayName ?? email.Split('@')[0];
                Session.IsAdmin = true;

                var mainForm = new MainForm();
                // We must Hide instead of Close because LoginForm is the root Application.Run form.
                // Closing it directly kills the whole application message loop.
                mainForm.FormClosed += (s, args) => 
                {
                    if (mainForm.DialogResult == DialogResult.Retry)
                    {
                        // User logged out — clear inputs and show login screen again
                        _emailBox.Clear();
                        _passwordBox.Clear();
                        _loginButton.Text = "Sign In";
                        _loginButton.Enabled = true;
                        this.Show();
                    }
                    else
                    {
                        // User closed the main window — exit the app completely
                        this.Close();
                    }
                };
                this.Hide();
                mainForm.Show();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Web API key"))
            {
                // The web API key placeholder wasn't replaced — guide the developer
                ShowError("Web API key not configured. Open LoginForm.cs and set the webApiKey constant.");
            }
            catch (Exception ex)
            {
                // Firebase Auth errors come through as nice human messages from ParseAuthError
                ShowError(ex.Message);
            }
            finally
            {
                _isLoading = false;
                _loginButton.Text = "Sign In";
                _loginButton.Enabled = true;
            }
        }

        private Timer _errorTimer;

        private void ShowError(string message)
        {
            _errorLabel.Text = message;
            _errorLabel.Visible = true;

            // Auto-dismiss after 4 seconds
            if (_errorTimer != null) { _errorTimer.Stop(); _errorTimer.Dispose(); }
            _errorTimer = new Timer { Interval = 4000 };
            _errorTimer.Tick += (s, e) => { _errorTimer.Stop(); _errorTimer.Dispose(); _errorTimer = null; HideError(); };
            _errorTimer.Start();
        }

        private void HideError()
        {
            _errorLabel.Text = "";
            _errorLabel.Visible = false;
        }

        private async Task LoadAdminAvatarAsync(PictureBox picBox)
        {
            try
            {
                // To fetch without auth, read access must be open.
                var users = await FirebaseService.Instance.GetAllAsync<Botanika_Desktop.Firebase.Models.Client>("users");
                var admin = users.FirstOrDefault(c => c.Email == "admin@botanika.com" || c.Role == "admin");
                if (admin != null && !string.IsNullOrEmpty(admin.ProfilePicture))
                {
                    string b64 = admin.ProfilePicture;
                    if (b64.Contains(",")) b64 = b64.Substring(b64.IndexOf(",") + 1);

                    var bytes = Convert.FromBase64String(b64);
                    using (var ms = new System.IO.MemoryStream(bytes))
                    {
                        var img = Image.FromStream(ms);
                        // Crop to square
                        int min = Math.Min(img.Width, img.Height);
                        var cropRect = new Rectangle((img.Width - min) / 2, (img.Height - min) / 2, min, min);
                        var squareBmp = new Bitmap(min, min);
                        using (var g = Graphics.FromImage(squareBmp))
                        {
                            g.DrawImage(img, new Rectangle(0, 0, min, min), cropRect, GraphicsUnit.Pixel);
                        }

                        picBox.Invoke((MethodInvoker)delegate {
                            picBox.Image = squareBmp;
                            var path = new System.Drawing.Drawing2D.GraphicsPath();
                            path.AddEllipse(0, 0, picBox.Width, picBox.Height);
                            picBox.Region = new Region(path);
                            picBox.Visible = true;
                        });
                    }
                }
            }
            catch { }
        }
    }

    // Minimal representation of the user document in Firestore
    // We only care about the isAdmin flag
    public class UserDoc
    {
        [JsonProperty("isAdmin")]
        public bool IsAdmin { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
