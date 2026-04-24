# Botanika Desktop Admin

Botanika Desktop is a standalone, fully-featured administrative desktop application built for the [Botanika web storefront](https://botanika-754.netlify.app/) (see the [web repository here](https://github.com/naveed-gung/Botanika)). 

Built with **.NET Framework 4.7.2** and **Windows Forms**, this app connects directly to the exact same Firebase project as the website. While the website handles the customer-facing experience and lightweight admin controls, this desktop app provides a significantly richer management surface for inventory, financials, and CRM.

## ✨ Features

- **Inventory Management:** Full CRUD operations for all botanical products.
- **Client & Supplier CRM:** Track customer orders, supplier details, and lifetime value.
- **Financial Dashboard:** Visualize revenue trends, track pending/received payments, and monitor order volumes.
- **Order Processing:** View and manage orders placed on the website.
- **Inline AI Assistant:** Built-in chatbot panel for quick store insights.
- **Advanced Data Export/Import:** Seamlessly export tables to **Excel (.xlsx), PDF, Word (.docx), Markdown, and CSV**. Support for bulk importing via CSV and Excel.
- **Modern UI:** Custom-built components (`BotanikaListView`, `BotanikaButton`) designed to match the specific color palette and typography of the Botanika brand.

## 🚀 Setup & Local Development

Because this application uses the **Firebase Admin SDK**, it bypasses standard security rules and has full read/write access. For security, the secret keys are **not** tracked in this repository.

To run the project locally, you must provide your own Firebase credentials:

1. **Service Account JSON**: 
   - Go to your Firebase Console → Project Settings → Service Accounts.
   - Generate a new private key.
   - Rename the downloaded file to exactly `serviceAccount.json`.
   - Place it inside the `Botanika-Desktop/Botanika-Desktop/Assets/` directory.

2. **Web API Key**:
   - Go to your Firebase Console → Project Settings → General.
   - Copy the "Web API Key".
   - Create a text file named `firebase_api_key.txt` inside the `Botanika-Desktop/Botanika-Desktop/Assets/` directory.
   - Paste the key inside the text file.

*(Note: Both of these files are automatically ignored by Git via `.gitignore` to keep your project secure).*

### Building the Project

Open the solution (`Botanika-Desktop.sln`) in Visual Studio:
- **To Debug:** Press `F5` to run the application in Debug mode.
- **To Publish a Standalone EXE:** 
  1. Change the build configuration to **Release**.
  2. Build the project (`Ctrl+Shift+B`).
  3. Optionally, install the `Costura.Fody` NuGet package to bundle all DLL dependencies into a single, clean `.exe` file.
  4. Ensure your `Assets/` folder travels alongside the `.exe` file for authentication.

## 🛠 Tech Stack

- **C# / .NET Framework 4.7.2**
- **WinForms** (with extensive custom-drawn controls)
- **Firebase Admin SDK** (via REST & Custom JWT Minting)
- **ClosedXML** (Excel Export)
- **QuestPDF** (PDF Export)
- **DocumentFormat.OpenXml** (Word Export)
- **Newtonsoft.Json** (Data parsing)
