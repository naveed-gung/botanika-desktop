using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Botanika_Desktop.Firebase;
using Botanika_Desktop.Firebase.Models;
using Botanika_Desktop.Theme;

namespace Botanika_Desktop.Forms
{
    // Bonus feature: a rule-based chatbot that knows your product inventory.
    // Same logic as the website's chatbot.js — no external AI API needed.
    // Ask it about plants, care tips, prices, or stock levels!
    public class ChatbotPanel : UserControl
    {
        private RichTextBox _chatHistory;
        private TextBox     _inputBox;
        private Button      _sendBtn;
        private Label       _statusLabel;

        // Cached products so we don't hit Firestore on every single message
        private List<Product> _products = new List<Product>();
        private bool          _productsLoaded;

        public ChatbotPanel()
        {
            BackColor = BotanikaColors.Offwhite;
            Dock      = DockStyle.Fill;
            BuildUI();

            // Greet the admin right away
            AppendMessage("🌿 Botanika", "Hello! I'm your Botanika assistant. " +
                "Ask me about plants, prices, stock levels, or anything about the store. " +
                "I'll load the current product catalog in a moment...",
                isBot: true);

            _ = LoadProductsAsync();
        }

        private void BuildUI()
        {
            int pad = 24;

            var header = new Label
            {
                Text      = "AI Assistant",
                Font      = BotanikaFonts.Heading(18f, FontStyle.Bold),
                ForeColor = BotanikaColors.Charcoal,
                Location  = new Point(pad, 16),
                AutoSize  = true
            };

            var subtitle = new Label
            {
                Text      = "Ask me anything about your plants, products, stock, or orders.",
                Font      = BotanikaFonts.Body(9.5f),
                ForeColor = BotanikaColors.TextMuted,
                Location  = new Point(pad, 46),
                AutoSize  = true
            };

            // ── Chat history display ───────────────────────────────────────────
            // RichTextBox gives us colored text and better formatting than a regular TextBox
            _chatHistory = new RichTextBox
            {
                Location   = new Point(pad, 76),
                Size       = new Size(ClientSize.Width - pad * 2, ClientSize.Height - 160),
                Anchor     = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackColor  = BotanikaColors.White,
                ForeColor  = BotanikaColors.Charcoal,
                Font       = BotanikaFonts.Body(10f),
                ReadOnly   = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            // ── Input area at the bottom ───────────────────────────────────────
            _inputBox = new TextBox
            {
                // PlaceholderText not available in .NET 4.7.2 — we handle it via GotFocus
                Font       = BotanikaFonts.Body(10.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor     = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            // Simulate placeholder
            _inputBox.Text      = "Ask about a plant, price, stock level...";
            _inputBox.ForeColor = BotanikaColors.TextMuted;
            _inputBox.GotFocus  += (s2, e2) => { if (_inputBox.Text == "Ask about a plant, price, stock level...") { _inputBox.Text = ""; _inputBox.ForeColor = BotanikaColors.Charcoal; } };
            _inputBox.LostFocus += (s2, e2) => { if (string.IsNullOrEmpty(_inputBox.Text)) { _inputBox.Text = "Ask about a plant, price, stock level..."; _inputBox.ForeColor = BotanikaColors.TextMuted; } };
            // Enter key sends the message — much more natural than clicking Send every time
            _inputBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;  // prevent the annoying ding sound
                    SendMessage();
                }
            };

            _sendBtn = new Button
            {
                Text      = "Send ➤",
                FlatStyle = FlatStyle.Flat,
                BackColor = BotanikaColors.Primary,
                ForeColor = Color.White,
                Font      = BotanikaFonts.Body(10f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _sendBtn.FlatAppearance.BorderSize = 0;
            _sendBtn.Click += (s, e) => SendMessage();

            _statusLabel = new Label
            {
                Text      = "Loading product catalog...",
                Font      = BotanikaFonts.Caption(8f),
                ForeColor = BotanikaColors.TextMuted,
                Anchor    = AnchorStyles.Bottom | AnchorStyles.Left,
                AutoSize  = true
            };

            // ── Layout the bottom controls ─────────────────────────────────────
            // We do manual anchoring calculation in the Resize event
            SizeChanged += (s, e) => LayoutBottomBar();
            LayoutBottomBar();

            Controls.AddRange(new Control[]
            {
                header, subtitle, _chatHistory, _inputBox, _sendBtn, _statusLabel
            });

            LayoutBottomBar();
        }

        // Positions the input box, send button, and status label at the bottom
        private void LayoutBottomBar()
        {
            int pad    = 24;
            int bottom = ClientSize.Height - pad;

            _statusLabel.Location = new Point(pad, bottom - 16);
            _inputBox.Size        = new Size(ClientSize.Width - pad * 2 - 90, 32);
            _inputBox.Location    = new Point(pad, bottom - 52);
            _sendBtn.Size         = new Size(80, 32);
            _sendBtn.Location     = new Point(ClientSize.Width - pad - 80, bottom - 52);

            if (_chatHistory != null)
                _chatHistory.Size = new Size(
                    ClientSize.Width - pad * 2,
                    ClientSize.Height - 160);
        }

        // ─── Product Loading ───────────────────────────────────────────────────

        private async Task LoadProductsAsync()
        {
            try
            {
                _products       = await FirebaseService.Instance.GetAllAsync<Product>("products");
                _productsLoaded = true;
                _statusLabel.Text = $"Ready · {_products.Count} products loaded";
                AppendMessage("🌿 Botanika",
                    $"I've loaded your catalog — {_products.Count} products ready. " +
                    "You can ask me things like:\n" +
                    "• \"What plants are in stock?\"\n" +
                    "• \"How much does the Monstera cost?\"\n" +
                    "• \"Which products are featured?\"\n" +
                    "• \"What's running low on stock?\"",
                    isBot: true);
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Couldn't load products — responses will be limited";
                AppendMessage("🌿 Botanika",
                    $"Hmm, I couldn't load the product catalog right now ({ex.Message}). " +
                    "I can still answer general questions though!",
                    isBot: true);
            }
        }

        // ─── Message Handling ──────────────────────────────────────────────────

        private void SendMessage()
        {
            string input = _inputBox.Text.Trim();
            // Ignore if user hasn't typed anything real yet
            if (string.IsNullOrEmpty(input) || input == "Ask about a plant, price, stock level...") return;

            // Show what the admin typed
            AppendMessage("You", input, isBot: false);
            _inputBox.Clear();

            // Generate and show the bot's response
            string response = GetBotResponse(input);
            AppendMessage("🌿 Botanika", response, isBot: true);
        }

        // Shows a message in the chat history with appropriate formatting
        private void AppendMessage(string sender, string message, bool isBot)
        {
            if (_chatHistory.Text.Length > 0)
                _chatHistory.AppendText("\n\n");

            // Sender name in accent color
            _chatHistory.SelectionStart = _chatHistory.TextLength;
            _chatHistory.SelectionColor = isBot ? BotanikaColors.Primary : BotanikaColors.Charcoal;
            _chatHistory.SelectionFont  = BotanikaFonts.Body(9.5f, FontStyle.Bold);
            _chatHistory.AppendText(sender + "\n");

            // Message body in normal text
            _chatHistory.SelectionColor = BotanikaColors.Charcoal;
            _chatHistory.SelectionFont  = BotanikaFonts.Body(10f);
            _chatHistory.AppendText(message);

            // Auto-scroll to the latest message
            _chatHistory.SelectionStart = _chatHistory.Text.Length;
            _chatHistory.ScrollToCaret();
        }

        // ─── Rule-Based Response Engine ────────────────────────────────────────

        // Generates a response based on keyword matching against the product catalog.
        // Same logic as the website chatbot — no API key needed!
        private string GetBotResponse(string input)
        {
            string lower = input.ToLowerInvariant();

            // ── Greetings ──────────────────────────────────────────────────────
            if (ContainsAny(lower, "hello", "hi", "hey", "howdy"))
                return "Hello! How can I help you with Botanika today? 🌿";

            if (ContainsAny(lower, "how are you", "how's it going"))
                return "I'm great, thanks for asking! Ready to help with your plant business. 🌱";

            // ── Product catalog questions ──────────────────────────────────────
            if (!_productsLoaded)
                return "I'm still loading the product catalog. Give me just a moment... ⏳";

            if (ContainsAny(lower, "how many product", "total product", "count product"))
                return $"You currently have {_products.Count} products in your catalog.";

            if (ContainsAny(lower, "out of stock", "no stock", "zero stock"))
            {
                var outOfStock = _products.FindAll(p => p.Stock <= 0);
                if (outOfStock.Count == 0) return "Great news — no products are currently out of stock! 🎉";
                var names = string.Join(", ", outOfStock.ConvertAll(p => p.Name));
                return $"{outOfStock.Count} product(s) are out of stock: {names}";
            }

            if (ContainsAny(lower, "low stock", "running low", "almost out"))
            {
                var low = _products.FindAll(p => p.Stock > 0 && p.Stock <= 5);
                if (low.Count == 0) return "No products are critically low right now. Stock levels look healthy! ✅";
                var sb = new StringBuilder($"{low.Count} product(s) are running low:\n");
                foreach (var p in low)
                    sb.AppendLine($"• {p.Name}: {p.Stock} left");
                return sb.ToString().TrimEnd();
            }

            if (ContainsAny(lower, "featured", "homepage", "on sale"))
            {
                var featured = _products.FindAll(p => p.Featured);
                if (featured.Count == 0) return "No products are currently featured on the homepage.";
                var names = string.Join(", ", featured.ConvertAll(p => p.Name));
                return $"{featured.Count} featured product(s): {names}";
            }

            if (ContainsAny(lower, "most expensive", "highest price", "priciest"))
            {
                var top = _products.Count > 0
                    ? _products.OrderByDescending(p => p.Price).First()
                    : null;
                return top != null
                    ? $"The most expensive product is \"{top.Name}\" at ${top.Price:F2}."
                    : "No products found.";
            }

            if (ContainsAny(lower, "cheapest", "lowest price", "most affordable"))
            {
                var cheapest = _products.Count > 0
                    ? _products.OrderBy(p => p.Price).First()
                    : null;
                return cheapest != null
                    ? $"The most affordable product is \"{cheapest.Name}\" at ${cheapest.Price:F2}."
                    : "No products found.";
            }

            if (ContainsAny(lower, "all products", "list products", "show products", "what plants"))
            {
                if (_products.Count == 0) return "The catalog is empty.";
                var sb = new StringBuilder($"Here are all {_products.Count} products:\n");
                foreach (var p in _products)
                    sb.AppendLine($"• {p.Name} — ${p.Price:F2} ({p.StockStatus})");
                return sb.ToString().TrimEnd();
            }

            // ── Search for a specific product by name ──────────────────────────
            // This catches questions like "how much is the Monstera?" or "is Peace Lily in stock?"
            foreach (var product in _products)
            {
                if (string.IsNullOrEmpty(product.Name)) continue;
                if (lower.Contains(product.Name.ToLowerInvariant()))
                {
                    return BuildProductInfo(product);
                }
            }

            // ── Category questions ─────────────────────────────────────────────
            string[] categories = { "tropical", "succulent", "herb", "flower", "tree" };
            foreach (var cat in categories)
            {
                if (lower.Contains(cat))
                {
                    var catProducts = _products.FindAll(p =>
                        p.Category?.ToLowerInvariant().Contains(cat) == true);
                    if (catProducts.Count == 0)
                        return $"No {cat} products in the catalog right now.";
                    var sb = new StringBuilder($"Found {catProducts.Count} {cat} product(s):\n");
                    foreach (var p in catProducts)
                        sb.AppendLine($"• {p.Name} — ${p.Price:F2} (Stock: {p.StockStatus})");
                    return sb.ToString().TrimEnd();
                }
            }

            // ── Help ───────────────────────────────────────────────────────────
            if (ContainsAny(lower, "help", "what can you do", "commands"))
            {
                return "Here's what I can help with:\n" +
                       "• \"How many products do we have?\"\n" +
                       "• \"What's out of stock?\"\n" +
                       "• \"What's running low?\"\n" +
                       "• \"Which products are featured?\"\n" +
                       "• \"Tell me about the Monstera\"\n" +
                       "• \"Show all tropical plants\"\n" +
                       "• \"What's the most expensive plant?\"";
            }

            // ── Fallback ───────────────────────────────────────────────────────
            return "I'm not sure about that one. Try asking about specific products, " +
                   "stock levels, categories, or pricing. Type \"help\" to see what I can do!";
        }

        // Builds a detailed info string for a specific product
        private string BuildProductInfo(Product p)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"**{p.Name}**");
            if (!string.IsNullOrEmpty(p.Category))   sb.AppendLine($"Category: {p.Category}");
            sb.AppendLine($"Price: ${p.Price:F2}");
            sb.AppendLine($"Stock: {p.StockStatus}");
            if (!string.IsNullOrEmpty(p.CareLevel))  sb.AppendLine($"Care Level: {p.CareLevel}");
            if (p.Featured)                           sb.AppendLine("⭐ Currently featured on the homepage");
            if (!string.IsNullOrEmpty(p.Description)) sb.AppendLine($"\n{p.Description}");
            return sb.ToString().TrimEnd();
        }

        // Checks if the input contains any of the given keywords
        private bool ContainsAny(string input, params string[] keywords)
        {
            foreach (var kw in keywords)
                if (input.Contains(kw)) return true;
            return false;
        }
    }
}
