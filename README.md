<div align="center">

<!-- Custom SVG Banner -->
<img src="https://capsule-render.vercel.app/api?type=waving&color=0:2D5A27,50:4A7C59,100:8FBC8F&height=220&section=header&text=🌿%20Botanika%20Desktop&fontSize=42&fontColor=FFFFFF&fontAlignY=35&desc=Admin%20CRM%20%E2%80%A2%20Inventory%20%E2%80%A2%20Analytics&descSize=18&descAlignY=55&descColor=C8E6C9&animation=fadeIn" width="100%" />

<!-- Badges Row 1 -->
<p>
  <a href="#"><img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white" /></a>
  <a href="#"><img src="https://img.shields.io/badge/.NET_Framework-4.7.2-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" /></a>
  <a href="#"><img src="https://img.shields.io/badge/UI-WinForms-68217A?style=for-the-badge&logo=visualstudio&logoColor=white" /></a>
  <a href="#"><img src="https://img.shields.io/badge/Backend-Firebase-FFCA28?style=for-the-badge&logo=firebase&logoColor=black" /></a>
</p>

<!-- Badges Row 2 -->
<p>
  <a href="#"><img src="https://img.shields.io/badge/Architecture-Layered_MVC-2D5A27?style=flat-square" /></a>
  <a href="#"><img src="https://img.shields.io/badge/Auth-Service_Account_JWT-4A7C59?style=flat-square" /></a>
  <a href="#"><img src="https://img.shields.io/badge/Data-Firestore_REST_API-FF6F00?style=flat-square" /></a>
  <a href="#"><img src="https://img.shields.io/badge/Status-Completed-brightgreen?style=flat-square" /></a>
</p>

<br/>

<!-- Decorative Divider -->
<img src="https://user-images.githubusercontent.com/73097560/115834477-dbab4500-a447-11eb-908a-139a6edaec5c.gif" width="80%">

<br/>

<i>A modern, premium admin CRM desktop application — the administrative backbone of the <a href="https://botanika-754.netlify.app">Botanika</a> plant e-commerce ecosystem.</i>

</div>

<br/>

## <img src="https://img.shields.io/badge/-%F0%9F%8C%B1-2D5A27?style=flat-square" /> About

**Botanika Desktop** is a full-featured Windows admin panel built from the ground up in **C# WinForms**. It was developed as an extended implementation of a basic CRUD assignment, evolving far beyond the original requirements into a polished, production-grade **administrative CRM system**.

The application acts as the operational command center for the Botanika platform — managing products, clients, orders, payments, suppliers, and revenue analytics — all synchronized in real-time with **Google Cloud Firestore**.

<br/>

<div align="center">
<table>
<tr>
<td align="center" width="200">
<img src="https://cdn.jsdelivr.net/gh/devicons/devicon/icons/google/google-original.svg" width="40"/><br/>
<b>Secure Auth</b><br/>
<sub>Firebase Auth + Admin gate</sub>
</td>
<td align="center" width="200">
<img src="https://cdn.jsdelivr.net/gh/devicons/devicon/icons/grafana/grafana-original.svg" width="40"/><br/>
<b>Live Dashboard</b><br/>
<sub>Real-time KPI metrics</sub>
</td>
<td align="center" width="200">
<img src="https://cdn.jsdelivr.net/gh/devicons/devicon/icons/firebase/firebase-plain.svg" width="40"/><br/>
<b>Firestore CRUD</b><br/>
<sub>Full entity management</sub>
</td>
<td align="center" width="200">
<img src="https://cdn.jsdelivr.net/gh/devicons/devicon/icons/filezilla/filezilla-plain.svg" width="40"/><br/>
<b>Import / Export</b><br/>
<sub>CSV & Markdown</sub>
</td>
</tr>
</table>
</div>

<br/>

---

## <img src="https://img.shields.io/badge/-%E2%9C%A8-4A7C59?style=flat-square" /> Features

<details>
<summary><img src="https://img.shields.io/badge/-Authentication_%26_Security-2D5A27?style=flat-square&logo=shield&logoColor=white" /></summary>
<br/>

| Feature | Description |
|---------|-------------|
| Firebase Auth REST | Email/password sign-in via `identitytoolkit.googleapis.com` |
| Admin Gate | Secondary admin-email verification prevents unauthorized access |
| Service Account JWT | PKCS#8 RSA key parsing → signed JWT → OAuth2 access token exchange |
| Credential Isolation | API keys loaded from external files, excluded via `.gitignore` |

</details>

<details>
<summary><img src="https://img.shields.io/badge/-Dashboard_%26_Analytics-4A7C59?style=flat-square&logo=googleanalytics&logoColor=white" /></summary>
<br/>

| Feature | Description |
|---------|-------------|
| KPI Stat Cards | Total Revenue, Orders, Products, and Clients at a glance |
| Recent Orders Feed | Live-updating list of the latest transactions |
| Revenue Panel | Dedicated revenue tracking and financial overview |
| Auto-Refresh | Dashboard data pulled fresh from Firestore on every load |

</details>

<details>
<summary><img src="https://img.shields.io/badge/-Full_CRUD_Management-8FBC8F?style=flat-square&logo=databricks&logoColor=white" /></summary>
<br/>

| Module | Capabilities |
|--------|-------------|
| **Products** | Create, edit, delete, search — with image URLs and category tags |
| **Clients** | Customer profiles with contact info and order history |
| **Orders** | Order tracking with status management and item breakdowns |
| **Payments** | Payment recording with method, amount, and date tracking |
| **Suppliers** | Supplier directory with product associations |

</details>

<details>
<summary><img src="https://img.shields.io/badge/-Custom_UI_System-68217A?style=flat-square&logo=materialdesign&logoColor=white" /></summary>
<br/>

| Component | Purpose |
|-----------|---------|
| `BotanikaButton` | Themed action buttons with hover states |
| `BotanikaListView` | Custom-styled list/grid with alternating row colors |
| `SidebarItem` | Icon-based navigation items with active state highlighting |
| `ToastNotification` | Non-blocking success/error notifications |
| `BotanikaColors` | Centralized color palette (Primary, Charcoal, Sand, etc.) |
| `BotanikaFonts` | Typography system with heading, body, and caption presets |
| `BotanikaTheme` | Rounded corners, shadows, and global styling utilities |

</details>

<details>
<summary><img src="https://img.shields.io/badge/-Chatbot_Panel-FF6F00?style=flat-square&logo=chatbot&logoColor=white" /></summary>
<br/>

An integrated conversational interface for quick admin operations and help — accessible directly from the sidebar.

</details>

<details>
<summary><img src="https://img.shields.io/badge/-Data_Import_%2F_Export-0078D6?style=flat-square&logo=microsoftexcel&logoColor=white" /></summary>
<br/>

| Format | Direction |
|--------|-----------|
| **CSV** | Export — generate spreadsheet-compatible data dumps |
| **Markdown** | Export — create formatted reports |
| **Import** | Bulk data ingestion from external files |

</details>

---

## <img src="https://img.shields.io/badge/-%F0%9F%8F%97%EF%B8%8F-2D5A27?style=flat-square" /> Architecture

```
Botanika-Desktop/
│
├── 📂 Forms/                    ← UI Screens & Panels
│   ├── LoginForm.cs             ← Secure admin authentication
│   ├── MainForm.cs              ← Shell with sidebar navigation
│   ├── DashboardPanel.cs        ← KPI cards + recent orders
│   ├── ProductsPanel.cs         ← Product CRUD
│   ├── ProductEditDialog.cs     ← Product create/edit modal
│   ├── ClientsPanel.cs          ← Client management
│   ├── OrdersPanel.cs           ← Order tracking
│   ├── PaymentsPanel.cs         ← Payment records
│   ├── SuppliersPanel.cs        ← Supplier directory
│   ├── RevenuePanel.cs          ← Financial analytics
│   └── ChatbotPanel.cs          ← Integrated chatbot
│
├── 📂 Controls/                 ← Reusable UI Components
│   ├── BotanikaButton.cs
│   ├── BotanikaListView.cs
│   ├── SidebarItem.cs
│   └── ToastNotification.cs
│
├── 📂 Firebase/                 ← Backend Layer
│   ├── FirebaseService.cs       ← HTTP bridge, JWT auth, CRUD
│   └── Models/
│       ├── Product.cs
│       ├── Client.cs
│       ├── Order.cs
│       ├── Payment.cs
│       └── Supplier.cs
│
├── 📂 Export/                   ← Data Import/Export
│   ├── CsvExporter.cs
│   ├── MarkdownExporter.cs
│   └── ImportHandler.cs
│
├── 📂 Theme/                    ← Design System
│   ├── BotanikaColors.cs
│   ├── BotanikaFonts.cs
│   └── BotanikaTheme.cs
│
├── 📂 Assets/                   ← Icons, branding, credentials
├── Session.cs                   ← Global session state
└── Program.cs                   ← Application entry point
```

---

## <img src="https://img.shields.io/badge/-%F0%9F%94%A7-4A7C59?style=flat-square" /> Tech Stack

<div align="center">
<table>
<tr>
<td align="center" width="140">
<img src="https://cdn.jsdelivr.net/gh/devicons/devicon/icons/csharp/csharp-original.svg" width="48"/><br/>
<b>C#</b><br/>
<sub>Primary Language</sub>
</td>
<td align="center" width="140">
<img src="https://cdn.jsdelivr.net/gh/devicons/devicon/icons/dot-net/dot-net-original.svg" width="48"/><br/>
<b>.NET 4.7.2</b><br/>
<sub>Framework</sub>
</td>
<td align="center" width="140">
<img src="https://cdn.jsdelivr.net/gh/devicons/devicon/icons/firebase/firebase-plain.svg" width="48"/><br/>
<b>Firebase</b><br/>
<sub>Auth & Firestore</sub>
</td>
<td align="center" width="140">
<img src="https://cdn.jsdelivr.net/gh/devicons/devicon/icons/json/json-original.svg" width="48"/><br/>
<b>Newtonsoft</b><br/>
<sub>JSON Serialization</sub>
</td>
<td align="center" width="140">
<img src="https://cdn.jsdelivr.net/gh/devicons/devicon/icons/visualstudio/visualstudio-plain.svg" width="48"/><br/>
<b>WinForms</b><br/>
<sub>Desktop UI</sub>
</td>
</tr>
</table>
</div>

---

## <img src="https://img.shields.io/badge/-%F0%9F%9A%80-2D5A27?style=flat-square" /> Getting Started

### Prerequisites

- **Visual Studio 2022** (or later)
- **.NET Framework 4.7.2** runtime
- A **Firebase project** with Firestore enabled

### Installation

```bash
# 1. Clone the repository
git clone https://github.com/naveed-gung/botanika-desktop.git

# 2. Open the solution
# Launch Botanika-Desktop.sln in Visual Studio
```

### Firebase Configuration

> [!IMPORTANT]
> You must provide two credential files before the app can connect to Firebase.
> These files are **excluded from Git** by `.gitignore` and will never be committed.

<table>
<tr>
<th>File</th>
<th>Location</th>
<th>Purpose</th>
</tr>
<tr>
<td><code>serviceAccount.json</code></td>
<td><code>Assets/</code></td>
<td>Firebase Admin SDK key — powers all Firestore CRUD operations via service account JWT</td>
</tr>
<tr>
<td><code>firebase_api_key.txt</code></td>
<td><code>Assets/</code></td>
<td>Firebase Web API key (single line) — used for email/password sign-in verification</td>
</tr>
</table>

**Where to find them:**
1. **`serviceAccount.json`** → [Firebase Console](https://console.firebase.google.com/) → Project Settings → Service Accounts → Generate New Private Key
2. **`firebase_api_key.txt`** → [Firebase Console](https://console.firebase.google.com/) → Project Settings → General → Web API Key → paste into the file

Then press **F5** in Visual Studio to build and run.

---

## <img src="https://img.shields.io/badge/-%F0%9F%94%92-4A7C59?style=flat-square" /> Security

<div align="center">

| Layer | Protection |
|:------|:-----------|
| **Credentials** | `serviceAccount.json` and `firebase_api_key.txt` are in `.gitignore` — never tracked by Git |
| **Authentication** | Firebase Auth REST API validates admin credentials server-side |
| **Authorization** | Secondary admin-email gate ensures only authorized users access the CRM |
| **Token Management** | Service account JWTs are minted locally with 1-hour expiry and auto-refreshed |

</div>

---

## <img src="https://img.shields.io/badge/-%F0%9F%8C%90-8FBC8F?style=flat-square" /> Related Project

<div align="center">

| | Botanika Web | Botanika Desktop |
|---|:---:|:---:|
| **Type** | E-commerce storefront | Admin CRM |
| **Tech** | HTML / CSS / JS | C# WinForms |
| **Users** | Customers | Administrators |
| **Link** | [botanika-754.netlify.app](https://botanika-754.netlify.app) | *This repository* |

</div>

---

## <img src="https://img.shields.io/badge/-%F0%9F%93%84-2D5A27?style=flat-square" /> License

This project was developed for **educational and portfolio purposes**.

---

<div align="center">

<img src="https://capsule-render.vercel.app/api?type=waving&color=0:2D5A27,50:4A7C59,100:8FBC8F&height=120&section=footer" width="100%" />

<br/>

**Built with 🌿 by [Naveed Sohail Gung](https://github.com/naveed-gung)**

<p>
  <a href="https://www.linkedin.com/in/naveed-sohail-gung-285645310/"><img src="https://img.shields.io/badge/LinkedIn-0A66C2?style=for-the-badge&logo=linkedin&logoColor=white" /></a>
  <a href="https://github.com/naveed-gung"><img src="https://img.shields.io/badge/GitHub-181717?style=for-the-badge&logo=github&logoColor=white" /></a>
  <a href="https://naveed-gung.dev/"><img src="https://img.shields.io/badge/Portfolio-4A7C59?style=for-the-badge&logo=googlechrome&logoColor=white" /></a>
</p>

</div>
