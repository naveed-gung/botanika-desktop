# Botanika Desktop

<p align="center">
  <img src="https://img.shields.io/badge/.NET-WinForms-blue?style=for-the-badge&logo=dotnet" />
  <img src="https://img.shields.io/badge/Architecture-Layered-green?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Backend-Firebase-orange?style=for-the-badge&logo=firebase" />
  <img src="https://img.shields.io/badge/Status-Completed-success?style=for-the-badge" />
</p>

---

## Overview

**Botanika Desktop** is a modern Windows-based administrative application built using **C# WinForms**.  
It serves as the **administrative CRM counterpart** to the [Botanika E-commerce Platform](https://botanika-754.netlify.app), enabling structured real-time management of business data such as products, clients, orders, and payments.

This application has been significantly polished to feature a rich, modern, SaaS-like dashboard, dynamic sidebar icons, and robust error handling.

---

## Implemented Features

### Core Functionality
- **Secure Admin Authentication:** Validates login credentials against Firebase Auth REST APIs and ensures admin-only access.
- **Real-Time Dashboard:** A modern UI displaying key business metrics (Total Revenue, Orders, Products, Clients) fetched directly from Firestore.
- **Full CRUD Operations:** Manage Products, Clients, Orders, Payments, and Suppliers effortlessly.

### Data Management & Backend
- **Custom Firebase Integration:** Direct integration with Google Cloud Firestore using a zero-dependency C# HTTP bridge (`FirebaseService.cs`).
- **PKCS#8 JWT Authentication:** Robust logic that mints service account tokens securely on the fly without heavy dependencies.
- **Structured Data Models:** Strongly-typed entity mappings (`Product.cs`, `Order.cs`, etc.).

### UI System & Aesthetics
- **SaaS-Style Modern Layout:** Designed with a clean, high-contrast dark-mode sidebar, rounded corner panels, and smooth typography (`BotanikaFonts.cs`).
- **Custom Reusable Components:** 
  - `BotanikaButton`
  - `BotanikaListView`
  - `SidebarItem` (with SVG-derived auto-scaling PNG icons)
  - `ToastNotification`
- **Dynamic Theming:** Configurable application-wide theme values (`BotanikaColors.cs`).

---

## Architecture

The project follows a **modular and layered structure**:
- `Forms/` → UI and user interaction
- `Controls/` → Reusable custom WinForms UI components
- `Firebase/` → Data models, serializers, and the custom backend service
- `Export/` → Data export logic (CSV, Markdown)
- `Theme/` → Styling definitions and color palettes
- `Session.cs` → Secure application state management

---

## Technologies Used

- **C# (.NET Framework 4.7.2)**
- **Firebase Firestore & Authentication (REST APIs)**
- **Newtonsoft.Json**

---

## Getting Started

### Prerequisites
- Visual Studio 2022 (recommended)
- .NET Framework 4.7.2

### Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/naveed-gung/botanika-desktop.git
   ```

2. **Configure Firebase Credentials:**
   To securely connect to Firestore, you must provide your private Firebase keys.
   - Create a folder named `Assets` in the root of the executable or project directory if it doesn't exist.
   - Add your **Firebase Admin SDK JSON file** and rename it to `serviceAccount.json`.
   - Add your **Web API Key** to a file named `firebase_api_key.txt`.
   
   *Note: Both of these files are automatically ignored by Git to prevent accidental exposure.*

3. **Build and Run:**
   Open `Botanika-Desktop.sln` in Visual Studio and hit `F5` to run the application.

---

## Security Notice

**Safe Git Pushing:** Sensitive files such as `serviceAccount.json` and `firebase_api_key.txt` have been explicitly added to the `.gitignore` file. Any pushes to GitHub are **completely safe**, and your credentials will not be uploaded.

---

## Future Improvements
- Role-based access control (RBAC) via Firestore Custom Claims
- Advanced analytics dashboard with graphing libraries
- Data export to PDF functionality

## License
This project is developed for educational and portfolio purposes.
