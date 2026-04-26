using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Botanika_Desktop.Firebase;
using Botanika_Desktop.Firebase.Models;
using Botanika_Desktop.Theme;

namespace Botanika_Desktop.Forms
{
    // Smart admin chatbot — mirrors the website's BotanikaBot with full
    // inventory, orders, clients, and revenue intelligence.
    public class ChatbotPanel : UserControl
    {
        private Panel       _chatArea;
        private TextBox     _inputBox;
        private Button      _sendBtn;
        private Label       _statusLabel;
        private FlowLayoutPanel _quickActions;

        // Cached data from Firestore
        private List<Product> _products = new List<Product>();
        private List<Client>  _clients  = new List<Client>();
        private List<Order>   _orders   = new List<Order>();
        private bool _dataLoaded;
        private string _lastAdminIntent;

        public ChatbotPanel()
        {
            BackColor = BotanikaColors.Offwhite;
            Dock      = DockStyle.Fill;
            DoubleBuffered = true;
            BuildUI();
            AddBotMessage("Hello! I'm your Botanika assistant. I can help you with inventory, orders, clients, revenue, and product insights. Loading your data now...");
            _ = LoadAllDataAsync();
        }

        // ─── UI Construction ──────────────────────────────────────────────────

        private void BuildUI()
        {
            int pad = 24;

            // Header bar
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = BotanikaColors.Charcoal,
                Padding = new Padding(pad, 0, pad, 0)
            };

            var botIcon = new Label
            {
                Text = "🌿",
                Font = new Font("Segoe UI Emoji", 18f),
                ForeColor = BotanikaColors.Primary,
                Location = new Point(pad, 16),
                AutoSize = true
            };

            var headerTitle = new Label
            {
                Text = "BOTANIKA Assistant",
                Font = BotanikaFonts.Heading(14f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(pad + 50, 14),
                AutoSize = true
            };

            _statusLabel = new Label
            {
                Text = "● Connecting...",
                Font = BotanikaFonts.Caption(8.5f),
                ForeColor = BotanikaColors.PrimaryLight,
                Location = new Point(pad + 50, 40),
                AutoSize = true
            };

            headerPanel.Controls.AddRange(new Control[] { botIcon, headerTitle, _statusLabel });

            // Chat message area
            _chatArea = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BotanikaColors.Offwhite,
                Padding = new Padding(pad, pad, pad, 0)
            };

            // Bottom input area
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 110,
                BackColor = BotanikaColors.White,
                Padding = new Padding(pad, 10, pad, 10)
            };

            // Quick action buttons
            _quickActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                AutoSize = false,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };
            BuildQuickActions();

            _inputBox = new TextBox
            {
                Font = BotanikaFonts.Body(10.5f),
                ForeColor = BotanikaColors.TextMuted,
                BackColor = BotanikaColors.SandLight,
                BorderStyle = BorderStyle.None,
                Text = "Ask about inventory, orders, clients...",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Height = 32
            };
            _inputBox.GotFocus += (s, e) => {
                if (_inputBox.Text == "Ask about inventory, orders, clients...") {
                    _inputBox.Text = "";
                    _inputBox.ForeColor = BotanikaColors.Charcoal;
                }
            };
            _inputBox.LostFocus += (s, e) => {
                if (string.IsNullOrEmpty(_inputBox.Text)) {
                    _inputBox.Text = "Ask about inventory, orders, clients...";
                    _inputBox.ForeColor = BotanikaColors.TextMuted;
                }
            };
            _inputBox.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SendMessage(); }
            };

            _sendBtn = new Button
            {
                Text = "➤",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = BotanikaColors.Primary,
                ForeColor = Color.White,
                Size = new Size(44, 32),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _sendBtn.FlatAppearance.BorderSize = 0;
            _sendBtn.FlatAppearance.MouseOverBackColor = BotanikaColors.PrimaryDark;
            _sendBtn.Click += (s, e) => SendMessage();

            // Layout bottom panel
            bottomPanel.Controls.Add(_quickActions);
            bottomPanel.Controls.Add(_inputBox);
            bottomPanel.Controls.Add(_sendBtn);

            bottomPanel.Resize += (s, e) => LayoutBottom();

            Controls.Add(_chatArea);    // fill
            Controls.Add(bottomPanel);  // bottom
            Controls.Add(headerPanel);  // top

            LayoutBottom();
        }

        private void LayoutBottom()
        {
            var parent = _inputBox.Parent;
            if (parent == null || parent.ClientSize.Width <= 0) return;
            int pad = 24;
            int y = 46;
            _inputBox.Location = new Point(pad, y);
            _inputBox.Size = new Size(parent.ClientSize.Width - pad * 2 - 54, 32);
            _sendBtn.Location = new Point(parent.ClientSize.Width - pad - 44, y);
        }

        private void BuildQuickActions()
        {
            _quickActions.Controls.Clear();
            string[][] actions = {
                new[] { "📊 Inventory", "inventory" },
                new[] { "🏆 Top Products", "top-products" },
                new[] { "👥 Clients", "clients" },
                new[] { "📦 Orders", "orders" },
                new[] { "💰 Revenue", "revenue" },
                new[] { "🔻 Low Stock", "low-stock" }
            };
            foreach (var a in actions)
            {
                var btn = new Button
                {
                    Text = a[0],
                    Tag = a[1],
                    Font = BotanikaFonts.Caption(8f),
                    ForeColor = BotanikaColors.Charcoal,
                    BackColor = BotanikaColors.SandLight,
                    FlatStyle = FlatStyle.Flat,
                    AutoSize = true,
                    Padding = new Padding(8, 2, 8, 2),
                    Margin = new Padding(0, 0, 6, 0),
                    Cursor = Cursors.Hand,
                    Height = 28
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = BotanikaColors.Sand;
                btn.FlatAppearance.MouseOverBackColor = BotanikaColors.Sand;
                btn.Click += (s, e) => HandleQuickAction((string)((Button)s).Tag);
                _quickActions.Controls.Add(btn);
            }
        }

        // ─── Data Loading ─────────────────────────────────────────────────────

        private async Task LoadAllDataAsync()
        {
            try
            {
                var pTask = FirebaseService.Instance.GetAllAsync<Product>("products");
                var cTask = FirebaseService.Instance.GetAllAsync<Client>("users");
                var oTask = FirebaseService.Instance.GetAllAsync<Order>("orders");
                await Task.WhenAll(pTask, cTask, oTask);
                _products = pTask.Result ?? new List<Product>();
                _clients  = cTask.Result ?? new List<Client>();
                _orders   = oTask.Result ?? new List<Order>();
                _dataLoaded = true;
                _statusLabel.Text = $"● Online · {_products.Count} products · {_clients.Count} clients · {_orders.Count} orders";
                _statusLabel.ForeColor = BotanikaColors.Success;
                AddBotMessage($"All systems online! I've loaded {_products.Count} products, {_clients.Count} clients, and {_orders.Count} orders.\n\nTry asking:\n• \"What's the inventory value?\"\n• \"Who are the top customers?\"\n• \"Which products are running low?\"\n• \"Show me revenue breakdown\"");
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "● Limited Mode";
                _statusLabel.ForeColor = BotanikaColors.Warning;
                AddBotMessage($"I couldn't load all data ({ex.Message}). I can still answer general questions!");
            }
        }

        // ─── Message Display ──────────────────────────────────────────────────

        private void AddBotMessage(string text) => AddMessageBubble(text, true);
        private void AddUserMessage(string text) => AddMessageBubble(text, false);

        private void AddMessageBubble(string text, bool isBot)
        {
            var bubble = new Panel
            {
                AutoSize = true,
                MaximumSize = new Size(Math.Max(300, _chatArea.ClientSize.Width - 120), 0),
                MinimumSize = new Size(200, 0),
                BackColor = isBot ? BotanikaColors.White : BotanikaColors.Primary,
                Padding = new Padding(14, 10, 14, 10),
                Margin = isBot ? new Padding(0, 6, 60, 6) : new Padding(60, 6, 0, 6)
            };

            var lbl = new Label
            {
                Text = text,
                Font = BotanikaFonts.Body(9.5f),
                ForeColor = isBot ? BotanikaColors.Charcoal : Color.White,
                AutoSize = true,
                MaximumSize = new Size(Math.Max(250, _chatArea.ClientSize.Width - 160), 0),
                Dock = DockStyle.Fill
            };

            bubble.Controls.Add(lbl);

            // Apply rounded corners to the chat bubble
            bubble.HandleCreated += (s, e) =>
            {
                bubble.Region = CreateRoundRegion(bubble.Width, bubble.Height, 12);
            };
            bubble.SizeChanged += (s, e) =>
            {
                bubble.Region = CreateRoundRegion(bubble.Width, bubble.Height, 12);
            };

            // Sender tag
            var sender = new Label
            {
                Text = isBot ? "🌿 Botanika" : "You",
                Font = BotanikaFonts.Caption(7.5f, FontStyle.Bold),
                ForeColor = isBot ? BotanikaColors.Primary : BotanikaColors.TextMuted,
                AutoSize = true,
                Margin = isBot ? new Padding(4, 4, 0, 0) : new Padding(0, 4, 4, 0)
            };

            // Wrap in a container to control alignment
            var wrapper = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 2),
                BackColor = Color.Transparent
            };

            var flow = new FlowLayoutPanel
            {
                FlowDirection = isBot ? FlowDirection.TopDown : FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                WrapContents = false,
                BackColor = Color.Transparent,
                Dock = DockStyle.Top,
                Padding = isBot ? new Padding(0) : new Padding(Math.Max(0, _chatArea.ClientSize.Width - bubble.MaximumSize.Width - 60), 0, 0, 0)
            };

            flow.Controls.Add(sender);
            flow.Controls.Add(bubble);
            wrapper.Controls.Add(flow);

            _chatArea.Controls.Add(wrapper);
            wrapper.BringToFront(); // panels dock top, newest at bottom by bringing to front

            // Scroll to bottom
            _chatArea.ScrollControlIntoView(wrapper);
        }

        // ─── Send / Receive ───────────────────────────────────────────────────

        private void SendMessage()
        {
            string input = _inputBox.Text.Trim();
            if (string.IsNullOrEmpty(input) || input == "Ask about inventory, orders, clients...") return;
            AddUserMessage(input);
            _inputBox.Clear();
            _inputBox.ForeColor = BotanikaColors.Charcoal;

            // Simulate a brief "thinking" delay
            var timer = new Timer { Interval = 400 };
            timer.Tick += (s, e) => { timer.Stop(); timer.Dispose(); AddBotMessage(GetResponse(input)); };
            timer.Start();
        }

        private void HandleQuickAction(string action)
        {
            string question = "";
            switch (action)
            {
                case "inventory":    question = "Give me the inventory overview"; break;
                case "top-products": question = "Which products are bought the most?"; break;
                case "clients":      question = "Show me a client summary"; break;
                case "orders":       question = "Show recent orders"; break;
                case "revenue":      question = "What's the total revenue?"; break;
                case "low-stock":    question = "What's running low on stock?"; break;
            }
            if (!string.IsNullOrEmpty(question))
            {
                AddUserMessage(question);
                var timer = new Timer { Interval = 400 };
                timer.Tick += (s, e) => { timer.Stop(); timer.Dispose(); AddBotMessage(GetResponse(question)); };
                timer.Start();
            }
        }

        // ─── Intelligence Engine ──────────────────────────────────────────────

        private string GetResponse(string input)
        {
            string q = input.ToLowerInvariant().Trim();

            // Greetings
            if (ContainsAny(q, "hello", "hi", "hey", "howdy", "good morning", "good evening"))
            {
                var greetings = new[] {
                    "Hello! How can I help you manage Botanika today?",
                    "Welcome back! What would you like to know?",
                    "Hey there! Ready to dive into your store data?"
                };
                return greetings[new Random().Next(greetings.Length)];
            }

            if (ContainsAny(q, "thank", "thanks", "thx", "appreciate"))
                return "You're welcome! Is there anything else I can help with?";

            if (ContainsAny(q, "bye", "goodbye", "see you", "later"))
                return "Goodbye! The dashboard will keep tracking everything for you.";

            if (!_dataLoaded)
                return "I'm still loading your data. Give me just a moment... ⏳";

            // ── Inventory & Stock ────────────────────────────────────────────
            if (ContainsAny(q, "inventory value", "inventory overview", "stock summary", "total inventory"))
            {
                _lastAdminIntent = "inventory";
                return GetInventoryOverview();
            }

            if (ContainsAny(q, "out of stock", "no stock", "zero stock", "sold out"))
            {
                var oos = _products.Where(p => p.Stock <= 0).ToList();
                if (oos.Count == 0) return "Great news — nothing is out of stock right now! 🎉";
                var sb = new StringBuilder($"{oos.Count} product(s) are out of stock:\n");
                foreach (var p in oos) sb.AppendLine($"• {p.Name}");
                return sb.ToString().TrimEnd();
            }

            if (ContainsAny(q, "low stock", "running low", "almost out", "restock"))
            {
                var low = _products.Where(p => p.Stock > 0 && p.Stock <= 5).ToList();
                if (low.Count == 0) return "No products are critically low. Stock levels look healthy! ✅";
                var sb = new StringBuilder($"{low.Count} product(s) need restocking:\n");
                foreach (var p in low) sb.AppendLine($"• {p.Name}: {p.Stock} left");
                return sb.ToString().TrimEnd();
            }

            // ── Revenue ──────────────────────────────────────────────────────
            if (ContainsAny(q, "revenue", "total sales", "total earned", "income"))
            {
                _lastAdminIntent = "revenue";
                double totalRev = _orders.Sum(o => o.Total);
                double avgOrder = _orders.Count > 0 ? totalRev / _orders.Count : 0;
                return $"Revenue Report:\n\n" +
                       $"• Total Revenue: ${totalRev:F2}\n" +
                       $"• Total Orders: {_orders.Count}\n" +
                       $"• Average Order Value: ${avgOrder:F2}\n" +
                       $"• Products Sold: {_orders.Sum(o => o.Items?.Count ?? 0)} line items";
            }

            // ── Top Products ─────────────────────────────────────────────────
            if (ContainsAny(q, "most bought", "top product", "best selling", "bought the most", "popular"))
            {
                _lastAdminIntent = "top-products";
                return GetTopProductsReport();
            }

            // ── Client / Customer Intelligence ───────────────────────────────
            if (ContainsAny(q, "who ordered", "clients that ordered", "customers that ordered", "who bought"))
            {
                _lastAdminIntent = "ordered-customers";
                return GetCustomersWithOrdersReport();
            }

            if (ContainsAny(q, "inactive user", "dormant", "long time", "no order", "no purchase", "haven't ordered"))
            {
                _lastAdminIntent = "inactive-users";
                return GetInactiveUsersReport();
            }

            if (ContainsAny(q, "name", "email", "list client", "list customer", "who are", "their name", "their email", "show client", "show customer"))
            {
                if (_clients.Count == 0) return "No clients registered yet.";
                var sb = new StringBuilder($"All {_clients.Count} clients:\n\n");
                foreach (var c in _clients)
                {
                    string name = c.Name ?? "Unknown";
                    string email = c.Email ?? "no email";
                    sb.AppendLine($"• {name} — {email}");
                }
                return sb.ToString().TrimEnd();
            }

            if (ContainsAny(q, "client summary", "customer summary", "client", "customer", "users"))
            {
                _lastAdminIntent = "customer-summary";
                return GetCustomerSummary();
            }

            // ── Orders ───────────────────────────────────────────────────────
            if (ContainsAny(q, "recent order", "latest order", "show order", "order list"))
            {
                if (_orders.Count == 0) return "No orders have been placed yet.";
                var recent = _orders.OrderByDescending(o => o.OrderDate).Take(5).ToList();
                var sb = new StringBuilder($"Last {recent.Count} orders:\n");
                foreach (var o in recent)
                    sb.AppendLine($"• {o.Id} — ${o.Total:F2} ({o.Status ?? "unknown"})");
                return sb.ToString().TrimEnd();
            }

            // ── Highest Payment / Top Spender ────────────────────────────────
            if (ContainsAny(q, "highest payment", "biggest order", "largest order", "top spender", "most spent", "highest spend"))
            {
                if (_orders.Count == 0) return "No orders exist yet, so I can't determine the top spender.";
                // Find the single largest order
                var biggest = _orders.OrderByDescending(o => o.Total).First();
                // Find the client who has spent the most overall
                var spendByClient = new Dictionary<string, double>();
                foreach (var o in _orders)
                {
                    string key = o.CustomerName ?? o.CustomerEmail ?? "Unknown";
                    if (!spendByClient.ContainsKey(key)) spendByClient[key] = 0;
                    spendByClient[key] += o.Total;
                }
                var topSpender = spendByClient.OrderByDescending(kv => kv.Value).First();
                return $"Biggest single order: ${biggest.Total:F2} by {biggest.CustomerName ?? "Unknown"}\n\n" +
                       $"Top spender overall: {topSpender.Key} with ${topSpender.Value:F2} across all orders";
            }

            // ── Order count per client ───────────────────────────────────────
            if (ContainsAny(q, "how many order", "order count", "total order"))
            {
                return $"There are {_orders.Count} orders in total, worth ${_orders.Sum(o => o.Total):F2}.";
            }

            // ── Featured ─────────────────────────────────────────────────────
            if (ContainsAny(q, "featured", "homepage", "on sale"))
            {
                var featured = _products.Where(p => p.Featured).ToList();
                if (featured.Count == 0) return "No products are currently featured on the homepage.";
                return $"{featured.Count} featured: " + string.Join(", ", featured.Select(p => p.Name));
            }

            // ── Price Queries ────────────────────────────────────────────────
            if (ContainsAny(q, "price", "cost", "how much"))
            {
                // Check for specific product name
                foreach (var p in _products)
                {
                    if (!string.IsNullOrEmpty(p.Name) && q.Contains(p.Name.ToLowerInvariant()))
                        return $"{p.Name} costs ${p.Price:F2}. Stock: {p.Stock} units.";
                }
                if (_products.Count == 0) return "No products in catalog.";
                var prices = _products.Select(p => p.Price).ToList();
                return $"Price range: ${prices.Min():F2} – ${prices.Max():F2}\nAverage: ${prices.Average():F2}";
            }

            if (ContainsAny(q, "most expensive", "priciest", "highest price"))
            {
                var top = _products.OrderByDescending(p => p.Price).FirstOrDefault();
                return top != null ? $"Most expensive: \"{top.Name}\" at ${top.Price:F2}" : "No products found.";
            }

            if (ContainsAny(q, "cheapest", "most affordable", "lowest price", "budget"))
            {
                var cheap = _products.OrderBy(p => p.Price).FirstOrDefault();
                return cheap != null ? $"Most affordable: \"{cheap.Name}\" at ${cheap.Price:F2}" : "No products found.";
            }

            // ── Product Count & Listing ──────────────────────────────────────
            if (ContainsAny(q, "how many product", "total product", "count product"))
                return $"You currently have {_products.Count} products in your catalog.";

            if (ContainsAny(q, "all product", "list product", "show product", "what plant"))
            {
                if (_products.Count == 0) return "The catalog is empty.";
                var sb = new StringBuilder($"All {_products.Count} products:\n");
                foreach (var p in _products) sb.AppendLine($"• {p.Name} — ${p.Price:F2} ({p.Stock} in stock)");
                return sb.ToString().TrimEnd();
            }

            // ── Specific product search ──────────────────────────────────────
            foreach (var p in _products)
            {
                if (!string.IsNullOrEmpty(p.Name) && q.Contains(p.Name.ToLowerInvariant()))
                    return BuildProductInfo(p);
            }

            // ── Category search ──────────────────────────────────────────────
            foreach (var cat in new[] { "indoor", "outdoor", "tropical", "succulent", "herb", "art", "print" })
            {
                if (q.Contains(cat))
                {
                    var matches = _products.Where(p => p.Category?.ToLowerInvariant().Contains(cat) == true).ToList();
                    if (matches.Count == 0) return $"No {cat} products found.";
                    var sb = new StringBuilder($"Found {matches.Count} {cat} product(s):\n");
                    foreach (var p in matches) sb.AppendLine($"• {p.Name} — ${p.Price:F2}");
                    return sb.ToString().TrimEnd();
                }
            }

            // ── Help ─────────────────────────────────────────────────────────
            if (ContainsAny(q, "help", "what can you", "command"))
            {
                return "Here's what I can do:\n\n" +
                       "📊 Inventory: \"inventory overview\", \"low stock\", \"out of stock\"\n" +
                       "💰 Revenue: \"total revenue\", \"average order value\"\n" +
                       "🏆 Products: \"top products\", \"featured\", \"cheapest\", \"most expensive\"\n" +
                       "👥 Clients: \"client summary\", \"inactive users\", \"who ordered\"\n" +
                       "📦 Orders: \"recent orders\", \"order count\"\n" +
                       "🔍 Search: Ask about any product by name!";
            }

            // ── Smart Inference — catch natural language before giving up ────
            // Superlatives: least/most/best/worst/newest/oldest
            if (_products.Count > 0)
            {
                if (ContainsAny(q, "least", "lowest", "minimum", "smallest", "fewest"))
                {
                    if (ContainsAny(q, "stock", "inventory", "quantity", "unit"))
                    {
                        var min = _products.Where(p => p.Stock > 0).OrderBy(p => p.Stock).FirstOrDefault();
                        return min != null ? $"Lowest stock: \"{min.Name}\" with only {min.Stock} units left at ${min.Price:F2}." : "All products have 0 stock.";
                    }
                    // Default "least" = cheapest
                    var cheapest = _products.OrderBy(p => p.Price).First();
                    return $"The least expensive product is \"{cheapest.Name}\" at ${cheapest.Price:F2}. Stock: {cheapest.Stock} units.";
                }

                if (ContainsAny(q, "most", "highest", "maximum", "biggest", "greatest"))
                {
                    if (ContainsAny(q, "stock", "inventory", "quantity", "unit"))
                    {
                        var max = _products.OrderByDescending(p => p.Stock).First();
                        return $"Highest stock: \"{max.Name}\" with {max.Stock} units at ${max.Price:F2}.";
                    }
                    if (ContainsAny(q, "price", "expensive", "costly", "plant", "product"))
                    {
                        var top = _products.OrderByDescending(p => p.Price).First();
                        return $"Most expensive: \"{top.Name}\" at ${top.Price:F2}. Stock: {top.Stock} units.";
                    }
                }

                if (ContainsAny(q, "newest", "latest", "recent", "new"))
                {
                    var newest = _products.OrderByDescending(p => p.Id).FirstOrDefault();
                    return newest != null ? $"Most recently added: \"{newest.Name}\" — ${newest.Price:F2} ({newest.Stock} in stock)" : "No products found.";
                }

                if (ContainsAny(q, "average", "avg", "mean"))
                {
                    if (ContainsAny(q, "price", "cost", "plant", "product"))
                        return $"Average product price: ${_products.Average(p => p.Price):F2}";
                    if (ContainsAny(q, "stock", "inventory"))
                        return $"Average stock per product: {_products.Average(p => p.Stock):F0} units";
                }

                // Partial product name match (fuzzy) — catches "monstera" even without exact name
                foreach (var p in _products)
                {
                    if (string.IsNullOrEmpty(p.Name)) continue;
                    var words = p.Name.ToLowerInvariant().Split(' ');
                    foreach (var word in words)
                    {
                        if (word.Length > 3 && q.Contains(word))
                            return BuildProductInfo(p);
                    }
                }
            }

            // ── Conversational catch-alls ─────────────────────────────────────
            if (ContainsAny(q, "what can", "what do", "who are", "tell me about"))
            {
                if (ContainsAny(q, "you", "bot")) return "I'm your Botanika assistant! I know about your products, orders, clients, and revenue. Type \"help\" to see all my capabilities!";
                if (ContainsAny(q, "store", "shop", "botanika")) return GetInventoryOverview();
            }

            if (ContainsAny(q, "how", "why", "when", "where", "what"))
            {
                // Try to be helpful even if we don't understand
                return $"I'm not sure I fully understand that question, but here's what I know:\n\n" +
                       $"• {_products.Count} products (${_products.Sum(p => p.Price * p.Stock):F2} inventory value)\n" +
                       $"• {_clients.Count} clients\n" +
                       $"• {_orders.Count} orders (${_orders.Sum(o => o.Total):F2} total revenue)\n\n" +
                       "Try rephrasing or type \"help\" for specific queries I can handle!";
            }

            // ── Fallback ─────────────────────────────────────────────────────
            return "I'm not sure about that one. Try asking about inventory, revenue, clients, orders, or specific products. Type \"help\" to see everything I can do!";
        }

        // ─── Report Builders ──────────────────────────────────────────────────

        private string GetInventoryOverview()
        {
            double invValue = _products.Sum(p => p.Price * p.Stock);
            int totalStock = _products.Sum(p => p.Stock);
            int lowStock = _products.Count(p => p.Stock > 0 && p.Stock <= 5);
            int outOfStock = _products.Count(p => p.Stock <= 0);

            return $"Inventory Overview:\n\n" +
                   $"• Inventory Value: ${invValue:F2}\n" +
                   $"• Total Products: {_products.Count}\n" +
                   $"• Total Stock Units: {totalStock}\n" +
                   $"• Registered Clients: {_clients.Count}\n" +
                   $"• Orders Tracked: {_orders.Count}\n" +
                   $"• Low Stock Items: {lowStock}\n" +
                   $"• Out of Stock: {outOfStock}";
        }

        private string GetTopProductsReport()
        {
            if (_orders.Count == 0) return "No order history available yet.";
            var productCounts = new Dictionary<string, int>();
            foreach (var order in _orders)
            {
                if (order.Items == null) continue;
                foreach (var item in order.Items)
                {
                    string name = item.ProductName ?? item.ProductId ?? "Unknown";
                    if (!productCounts.ContainsKey(name)) productCounts[name] = 0;
                    productCounts[name] += item.Quantity;
                }
            }
            var top = productCounts.OrderByDescending(kv => kv.Value).Take(5).ToList();
            if (top.Count == 0) return "No product purchase data available.";
            var sb = new StringBuilder("Most Bought Products:\n");
            for (int i = 0; i < top.Count; i++)
                sb.AppendLine($"{i + 1}. {top[i].Key} — {top[i].Value} units sold");
            return sb.ToString().TrimEnd();
        }

        private string GetCustomerSummary()
        {
            if (_clients.Count == 0) return "No client profiles available yet.";
            var withOrders = _clients.Count(c => _orders.Any(o => o.CustomerEmail == c.Email));
            return $"Client Summary:\n\n" +
                   $"• Registered Clients: {_clients.Count}\n" +
                   $"• Clients with Orders: {withOrders}\n" +
                   $"• Clients without Orders: {_clients.Count - withOrders}";
        }

        private string GetCustomersWithOrdersReport()
        {
            var result = new List<string>();
            foreach (var c in _clients)
            {
                var userOrders = _orders.Where(o => o.CustomerEmail == c.Email).ToList();
                if (userOrders.Count > 0)
                    result.Add($"• {c.Name ?? c.Email} — {userOrders.Count} order(s)");
            }
            if (result.Count == 0) return "No clients have placed orders yet.";
            return "Clients who have ordered:\n\n" + string.Join("\n", result);
        }

        private string GetInactiveUsersReport()
        {
            var inactive = new List<string>();
            foreach (var c in _clients)
            {
                var userOrders = _orders.Where(o => o.CustomerEmail == c.Email).OrderByDescending(o => o.OrderDate).ToList();
                if (userOrders.Count == 0)
                    inactive.Add($"• {c.Name ?? c.Email} — has never ordered");
                else
                {
                    var last = userOrders.First().OrderDate;
                    int days = (int)(DateTime.UtcNow - last).TotalDays;
                    if (days > 30) inactive.Add($"• {c.Name ?? c.Email} — last ordered {days} days ago");
                }
            }
            if (inactive.Count == 0) return "All customers are active! Everyone has ordered recently. 🎉";
            return "Customers needing attention:\n\n" + string.Join("\n", inactive.Take(8));
        }

        private string BuildProductInfo(Product p)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{p.Name}");
            if (!string.IsNullOrEmpty(p.Category)) sb.AppendLine($"Category: {p.Category}");
            sb.AppendLine($"Price: ${p.Price:F2}");
            sb.AppendLine($"Stock: {p.Stock} units");
            if (p.Featured) sb.AppendLine("⭐ Currently featured");
            if (!string.IsNullOrEmpty(p.Description)) sb.AppendLine($"\n{p.Description}");
            return sb.ToString().TrimEnd();
        }

        private bool ContainsAny(string input, params string[] keywords)
        {
            foreach (var kw in keywords)
                if (input.Contains(kw)) return true;
            return false;
        }

        // Creates a rounded rectangle region for clipping (chat bubbles)
        private static System.Drawing.Region CreateRoundRegion(int w, int h, int r)
        {
            if (w <= 0 || h <= 0) return null;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = r * 2;
            path.AddArc(0, 0, d, d, 180, 90);
            path.AddArc(w - d, 0, d, d, 270, 90);
            path.AddArc(w - d, h - d, d, d, 0, 90);
            path.AddArc(0, h - d, d, d, 90, 90);
            path.CloseFigure();
            return new System.Drawing.Region(path);
        }
    }
}
