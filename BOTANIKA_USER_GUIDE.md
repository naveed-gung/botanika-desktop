# 🌿 Botanika Desktop CRM - User Guide

Welcome to the **Botanika Desktop CRM**, your premium administrative command center for managing the Botanika nursery and store. This guide will walk you through the core features and how to navigate the application efficiently.

---

## 🔐 Getting Started: Login
The application begins with a secure login screen. 
- **Admin Identity**: The system automatically detects the admin user and displays their circular profile picture and the Botanika logo.
- **Credentials**: Enter your administrator email and password to access the dashboard.
- **Persistence**: Once logged in, your session is securely managed via Firebase.

---

## 📊 Dashboard: The Bird's-Eye View
The Dashboard is your primary landing page, providing real-time store metrics.
- **Stat Cards**: View instant counts for **Total Revenue**, **Total Orders**, **Products**, and **Clients**. These numbers sync directly with Firestore.
- **Recent Orders**: A tightly-wrapped table showing the 10 most recent transactions. You can see the order number, customer name, date, and status at a glance.
- **Refresh**: Click the **⟳ Refresh** button at any time to pull the latest live data from the database.

---

## 👥 Clients Management
Manage your customer base with ease in the Clients panel.
- **Circular Avatars**: Every client is represented by a circular avatar. If a custom profile picture was uploaded via the web app, it will display here (fully decoded and sanitized). Otherwise, a default placeholder is used.
- **Smart Search**: Use the search bar to filter clients by Name or Email instantly.
- **Auto-Sizing Tables**: The client table automatically adjusts its column widths to perfectly fit the longest names and email addresses, ensuring no data is ever clipped.

---

## 💰 Revenue Analysis
Dive deep into your financial performance.
- **KPI Metrics**: Track **Avg. Order Value** and **Top Performing Products**.
- **Monthly Revenue Chart**: A clean bar chart visualizing your income over the last 12 months. Bar widths are capped for clarity, even with limited data.
- **Top Products Pie Chart**: A new interactive visualization showing your best-selling items categorized by sales volume.

---

## 💳 Payments & Orders
Track the lifecycle of every transaction.
- **Payment Tiers**: The Payments panel categorizes transactions into tabs for easier tracking.
- **Responsive Tables**: Like all lists in Botanika, these tables wrap tightly around their data, removing unnecessary white space and providing a clean, "Excel-like" density.

---

## 🛠 Navigation & Socials
- **Sidebar**: Use the left sidebar to switch between panels. The sidebar is color-coded with Botanika's signature **Forest Green** and **Sand** palette.
- **Social Links**: At the bottom of the sidebar, you can find quick links to:
    - 🌐 **Botanika Website**
    - **in** **LinkedIn**
    - **GH** **GitHub Repository**
    - 💼 **Professional Portfolio**

---

## ✅ Common Admin Tasks

- **Add a Product**: Open the Products panel, click the add action, complete the product details, and save to sync the new record to Firestore.
- **Update an Order Status**: Open the Orders panel, select the target order, change its status, and save so the order lifecycle remains accurate.
- **Export Data**: Use the export actions available in management panels to generate CSV or Markdown output for reporting.

---

## 💡 Pro Tips
- **Double-Click**: In most tables, double-clicking a row will open the detailed edit view for that item.
- **Responsive Window**: You can resize the Botanika window to any size; all charts, tables, and KPI cards will dynamically re-anchor themselves to fit your screen perfectly.

---

*For technical support or feature requests, please visit the GitHub repository via the sidebar link.*
