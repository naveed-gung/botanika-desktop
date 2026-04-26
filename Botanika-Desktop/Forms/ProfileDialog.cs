using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Botanika_Desktop.Controls;
using Botanika_Desktop.Firebase;
using Botanika_Desktop.Firebase.Models;
using Botanika_Desktop.Theme;

namespace Botanika_Desktop.Forms
{
    public class ProfileDialog : Form
    {
        private TextBox _nameBox;
        private TextBox _emailBox;
        private PictureBox _profilePic;
        private Button _uploadBtn;
        private Button _deletePicBtn;
        
        private string _currentBase64Pic;
        
        public ProfileDialog()
        {
            Text = "Edit Admin Profile";
            Size = new Size(420, 380);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = BotanikaColors.Offwhite;

            BuildUI();
            _ = LoadProfileAsync();
        }

        private void BuildUI()
        {
            int pad = 24;

            // Header
            var header = new Label
            {
                Text = "Admin Profile",
                Font = BotanikaFonts.Heading(14f, FontStyle.Bold),
                ForeColor = BotanikaColors.Charcoal,
                Location = new Point(pad, pad),
                AutoSize = true
            };
            Controls.Add(header);

            // Profile Picture
            _profilePic = new PictureBox
            {
                Size = new Size(80, 80),
                Location = new Point(pad, 60),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = BotanikaColors.SandLight,
                Cursor = Cursors.Hand
            };
            
            // Make picture box circular
            _profilePic.Paint += (s, e) => {
                GraphicsPath path = new GraphicsPath();
                path.AddEllipse(0, 0, _profilePic.Width, _profilePic.Height);
                _profilePic.Region = new Region(path);
            };
            
            _profilePic.Click += (s, e) => UploadPicture();
            Controls.Add(_profilePic);

            _uploadBtn = new Button
            {
                Text = "Upload",
                Size = new Size(70, 26),
                Location = new Point(pad + 90, 70),
            };
            BotanikaTheme.StyleSecondaryButton(_uploadBtn);
            _uploadBtn.Click += (s, e) => UploadPicture();
            Controls.Add(_uploadBtn);

            _deletePicBtn = new Button
            {
                Text = "Remove",
                Size = new Size(70, 26),
                Location = new Point(pad + 90, 106),
                ForeColor = BotanikaColors.Terracotta
            };
            BotanikaTheme.StyleSecondaryButton(_deletePicBtn);
            _deletePicBtn.Click += (s, e) => {
                _currentBase64Pic = null;
                _profilePic.Image = null;
            };
            Controls.Add(_deletePicBtn);

            // Form inputs
            int y = 160;
            int lw = 60, fw = 280, rh = 30;

            void Row(string label, Control ctrl)
            {
                Controls.Add(new Label
                {
                    Text = label, Font = BotanikaFonts.Body(9.5f),
                    ForeColor = BotanikaColors.TextMuted,
                    Size = new Size(lw, rh), Location = new Point(pad, y + 5)
                });
                ctrl.Size = new Size(fw, rh); ctrl.Location = new Point(pad + lw + 10, y);
                Controls.Add(ctrl);
                y += rh + 12;
            }

            _nameBox = new TextBox(); BotanikaTheme.StyleTextBox(_nameBox); Row("Name", _nameBox);
            _emailBox = new TextBox { ReadOnly = true, BackColor = BotanikaColors.SandLight }; 
            BotanikaTheme.StyleTextBox(_emailBox); 
            Row("Email", _emailBox);

            // Save and Cancel buttons
            var saveBtn = new Button
            {
                Text = "Save Changes", Size = new Size(130, 34),
                Location = new Point(pad + lw + 10 + fw - 130, y + 10)
            };
            BotanikaTheme.StyleButton(saveBtn);
            saveBtn.Click += async (s, e) => await SaveProfileAsync();

            var cancelBtn = new Button
            {
                Text = "Cancel", Size = new Size(80, 34),
                Location = new Point(saveBtn.Left - 90, y + 10)
            };
            BotanikaTheme.StyleSecondaryButton(cancelBtn);
            cancelBtn.Click += (s, e) => Close();

            Controls.Add(saveBtn);
            Controls.Add(cancelBtn);
        }

        private async Task LoadProfileAsync()
        {
            if (string.IsNullOrEmpty(Session.UserId)) return;
            try
            {
                var adminUser = await FirebaseService.Instance.GetByIdAsync<Client>("users", Session.UserId);
                if (adminUser != null)
                {
                    _nameBox.Text = adminUser.Name ?? Session.DisplayName;
                    _emailBox.Text = adminUser.Email ?? Session.Email;
                    
                    if (!string.IsNullOrEmpty(adminUser.ProfilePicture))
                    {
                        _currentBase64Pic = adminUser.ProfilePicture;
                        try {
                            var bytes = Convert.FromBase64String(_currentBase64Pic);
                            using (var ms = new MemoryStream(bytes))
                            {
                                _profilePic.Image = Image.FromStream(ms);
                            }
                        } catch { }
                    }
                }
            }
            catch (Exception) { }
        }

        private void UploadPicture()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Profile Picture";
                openFileDialog.Filter = "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var original = Image.FromFile(openFileDialog.FileName);
                        // Resize image to max 200x200 to keep base64 small
                        int newWidth = 200;
                        int newHeight = (int)(original.Height * ((float)newWidth / original.Width));
                        var resized = new Bitmap(newWidth, newHeight);
                        using (var g = Graphics.FromImage(resized))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawImage(original, 0, 0, newWidth, newHeight);
                        }
                        
                        _profilePic.Image = resized;
                        using (var ms = new MemoryStream())
                        {
                            resized.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                            _currentBase64Pic = Convert.ToBase64String(ms.ToArray());
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to load image: " + ex.Message);
                    }
                }
            }
        }

        private async Task SaveProfileAsync()
        {
            if (string.IsNullOrWhiteSpace(_nameBox.Text))
            {
                MessageBox.Show("Name is required.");
                return;
            }

            try
            {
                var adminUser = await FirebaseService.Instance.GetByIdAsync<Client>("users", Session.UserId);
                if (adminUser == null)
                {
                    adminUser = new Client {
                        Id = Session.UserId,
                        Role = "admin",
                        CreatedAt = DateTime.UtcNow
                    };
                }

                adminUser.Name = _nameBox.Text.Trim();
                adminUser.Email = _emailBox.Text; // read-only, but keep it
                adminUser.ProfilePicture = _currentBase64Pic;

                await FirebaseService.Instance.SaveAsync("users", Session.UserId, adminUser);
                
                Session.DisplayName = adminUser.Name;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save profile: " + ex.Message);
            }
        }
    }
}
