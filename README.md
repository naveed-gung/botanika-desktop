# Botanika Desktop

<p align="center">
  <img src="https://img.shields.io/badge/.NET-WinForms-blue?style=for-the-badge&logo=dotnet" />
  <img src="https://img.shields.io/badge/Architecture-Layered-green?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Backend-Firebase-orange?style=for-the-badge&logo=firebase" />
  <img src="https://img.shields.io/badge/Status-Completed-success?style=for-the-badge" />
</p>

---

## Overview

**Botanika Desktop** is a Windows-based administrative application built using **C# WinForms**.  
It was developed as an extended implementation of a basic CRUD assignment, evolving into a more complete **admin management system**.

The application serves as the **administrative counterpart** to the Botanika platform, enabling structured management of business data such as products, clients, orders, and payments.

---

## Project Requirements (Original Assignment)

The project was required to include:

- Admin Login Form  
- Home Form with a ListBox  
- CRUD operations (Create, Read, Update, Delete)  
- Import / Export functionality  
- Optional bonus features  

---

## Implemented Features

This project significantly expands beyond the original requirements.

### Core Functionality

- Secure **Admin Authentication System**
- Dashboard with modular panels
- Full CRUD operations across multiple entities:
  - Products
  - Clients
  - Orders
  - Payments
  - Suppliers

### Data Management

- Firebase integration for real-time data handling
- Structured models for all entities
- Centralized service layer (`FirebaseService.cs`)

### Import / Export

- CSV Export
- Markdown Export
- Data Import Handler

### UI System

- Custom reusable components:
  - `BotanikaButton`
  - `BotanikaListView`
  - `SidebarItem`
  - `ToastNotification`

- Theming system:
  - Centralized color palette
  - Font management
  - Consistent UI styling

### Additional Features (Bonus)

- Chatbot panel interface
- Revenue tracking dashboard
- Modular panel navigation system
- Session handling system

---

## Architecture

The project follows a **modular and layered structure**:
Forms/ → UI and user interaction
Controls/ → Reusable UI components
Firebase/ → Data models and backend service
Export/ → Data import/export logic
Theme/ → Styling and UI consistency
Session.cs → Application state management

This separation improves maintainability, scalability, and readability.

---

## Technologies Used

- **C# (.NET Framework / WinForms)**
- **Firebase (Firestore / Realtime DB via Admin SDK)**
- **Newtonsoft.Json**
- Custom UI components and theming system

---

## Getting Started

### Prerequisites

- Visual Studio (recommended)
- .NET Framework installed
- Firebase project

### Setup

1. Clone the repository:

```bash
git clone https://github.com/your-username/botanika-desktop.git
```

2. Open the solution:
   Botanika-Desktop.sln

3. Configure Firebase:
- Add your Firebase Admin SDK JSON file
- Do NOT commit this file to GitHub
4. Build and run the project

Security Notice

Sensitive files such as Firebase credentials are excluded via .gitignore.

Example:

botanika-*-firebase-adminsdk-*.json

Ensure your credentials are stored securely and never pushed to version control.

Screenshots

Add screenshots here if needed

Future Improvements
Role-based access control
Advanced analytics dashboard
API abstraction layer
Migration to WPF or web-based admin panel
Related Project

Botanika Desktop is designed as the administrative interface for the Botanika ecosystem.

License

This project is developed for educational purposes.


---

### If you want to push this further (recommended)

Right now this README is **solid**, but you can make it *stand out* by:

- Adding real screenshots (huge impact)
- Adding a small architecture diagram (I can generate one)
- Linking your main Botanika website repo
- Adding a short demo GIF

If you want, I can:
- :contentReference[oaicite:0]{index=0}
- :contentReference[oaicite:1]{index=1}
- or :contentReference[oaicite:2]{index=2}

Just tell me 👍
