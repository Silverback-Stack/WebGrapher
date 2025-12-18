# Getting Started

This guide walks you through setting up **WebGrapher** for local development on Windows, including HTTPS support for the Vue (Vite) UI.

---

## Prerequisites

This project uses:

* .NET
* Node.js / npm
* Vue + Vite
* HTTPS (self-signed certificate for localhost)

The steps below assume **Windows 10/11**.

---

## 1. Install Visual Studio 2022

Install **Visual Studio 2022** from:
[https://visualstudio.microsoft.com/](https://visualstudio.microsoft.com/)

During installation, ensure the following workloads are selected:

* ASP.NET and web development
* Azure development
* Node.js development
* .NET desktop development
* Data storage and processing

These workloads cover all backend, CLI, and frontend development needs for the project.

---

## 2. Install Node.js (includes npm)

If you don’t already have Node.js installed:

1. Download the **LTS version** from:
   [https://nodejs.org](https://nodejs.org)
2. Run the installer
3. Make sure to check:

   **Automatically install the necessary tools**

This installs both `node` and `npm`.

Verify installation:

```powershell
node -v
npm -v
```

---

## 3. Download the Repository

Clone or download the WebGrapher repository:

```
[https://github.com/Silverback-Stack/WebGrapher](https://github.com/Silverback-Stack/WebGrapher)
```

Install to your local drive. Example:

```
C:\WebGrapher
```

---

## 4. Create a Self-Signed Certificate (HTTPS)

The Vue dev server runs over HTTPS. You’ll need a self-signed certificate for `localhost`.

### 4.1 Create a certificate folder. Example:

```powershell
mkdir C:\Certs
```

### 4.2 Open PowerShell as Administrator

Right-click **PowerShell** → **Run as administrator**

### 4.3 Create the self-signed certificate

```powershell
$cert = New-SelfSignedCertificate \
  -DnsName "localhost" \
  -CertStoreLocation "cert:\\LocalMachine\\My" \
  -NotAfter (Get-Date).AddYears(1)
```

This creates a certificate in the Local Machine store and assigns it to `$cert`.

---

### 4.4 Export the certificate + private key (PFX)

Create an empty password (no hardcoded secrets):

```powershell
$pwd = ConvertTo-SecureString -String "" -Force -AsPlainText
```

Export the PFX file:

```powershell
Export-PfxCertificate \
  -Cert $cert \
  -FilePath "C:\\Certs\\localhost.pfx" \
  -Password $pwd
```

---

### 4.5 Export the public certificate

```powershell
Export-Certificate \
  -Cert $cert \
  -FilePath "C:\\Certs\\localhost.cer"
```

---

### 4.6 Trust the certificate

1. Double-click:
   `C:\Certs\localhost.cer`
2. Click **Install Certificate**
3. Choose:

   * Store Location: **Local Machine**
   * Certificate Store: **Trusted Root Certification Authorities**
4. Finish the wizard

This prevents HTTPS browser warnings for localhost.

---

## 5. Configure Vite to Use the Certificate

Open the Vite config file:

```
C:\WebGrapher\src\webgrapher.webui\vite.config.js
```

Ensure the HTTPS section points to your certificate:

```js
server: {
  https: {
    pfx: fs.readFileSync('C:/Certs/localhost.pfx'),
    passphrase: ''
  },
  port: 52924
}
```

---

## 6. Run the Backend / CLI

1. Open the solution file:

```
C:\WebGrapher\WebGrapher.sln
```

2. Set the startup project to:

```
WebGrapher.Cli
```

3. Run using the launch profile:

```
WebGrapher.Cli (Memory)
```

---

## 7. Run the UI (SPA)

Open **Developer PowerShell** in Visual Studio (or any terminal).

Navigate to the project web UI folder:

```powershell
cd src\webgrapher.webui
```

Install dependencies (first run only):

```powershell
npm install
```

Start the Vite dev server:

```powershell
npm run dev
```

---

## 8. Open the App

Navigate to:

```
https://localhost:52924/
```

You should now see the WebGrapher UI running over HTTPS.

---

## 9. Login

Enter the default username and password.

These can be found and changed in the Shared.Auth app settings file:

```
C:\WebGrapher\src\Settings.Core\Shared.Auth\appsettings.json
```

---

## Troubleshooting

### Certificate warnings

* Ensure the `.cer` file is installed under **Trusted Root Certification Authorities**

### Port already in use

* Make sure nothing else is running on port `52924`

### npm command not found

* Verify Node.js is installed
* Restart your terminal after installation

---

You're now ready to start developing with **WebGrapher** 🚀
