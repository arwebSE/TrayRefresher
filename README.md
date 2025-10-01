[![Build-WinSingleExe](https://github.com/arwebSE/TrayRefresher/actions/workflows/build.yml/badge.svg)](https://github.com/arwebSE/TrayRefresher/actions/workflows/build.yml)

# TrayRefresher

TrayRefresher is a tiny Windows utility that fixes a **long-standing Windows issue** where the system tray (notification area) shows **stale or “ghost” icons** that only clear when you hover over them. This problem has persisted across multiple Windows versions. TrayRefresher runs quietly in the background, keeps the tray fresh automatically, and gives you a quick **Refresh now** action from its own tray icon.

---

## 🚀 What it does

- **Prevents stuck/ghost icons** in the system tray.
- **Auto-refreshes** on a timer (default: every 5 minutes).
- **Instant refresh on system events:**
  - Logon / unlock
  - Resume from sleep
  - Display settings change
  - Explorer restart (TaskbarCreated)
- **Has its own tray icon & menu:**
  - **Refresh now**
  - **Exit**

---

## 🖥️ How to use

1. **Download** the latest release from the **Releases** page.
   - `TrayRefresher_<version>_win-x64_selfcontained.exe`  
     → larger, **no .NET required** (portable).
   - `TrayRefresher_<version>_win-x64_frameworkdep.exe`  
     → smaller, **requires [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)** installed.

2. **Run** the `.exe`.  
   - You’ll see the TrayRefresher icon in your system tray.
   - Right-click it to **Refresh now** or **Exit**.
   - It auto-refreshes in the background at the configured interval.

3. **(Optional) Start automatically at logon**

   ```powershell
   schtasks /Create /TN "TrayRefresher" /TR "`"C:\Path\TrayRefresher.exe`"" /SC ONLOGON /RL HIGHEST /F
   ```

4. **(Optional) Remove from startup / uninstall**
   - Delete the `.exe` wherever you placed it.
   - Remove the scheduled task if you created one:

     ```powershell
     schtasks /Delete /TN "TrayRefresher" /F
     ```

---

## ❓ FAQ

**Does this slow down my PC?**  
No. It’s lightweight and sleeps between quick, targeted refresh sweeps.

**Does it modify or kill Explorer?**  
No. It sends safe redraw/mouse-move messages to the tray’s toolbar windows—no process restarts.

**Will it work on multi-monitor / secondary taskbars?**  
Yes. It targets both primary and secondary tray windows (Windows 10/11).

**Do I need admin rights?**  
Running the app doesn’t require admin. Creating a scheduled task with highest privileges may prompt for elevation depending on your system policy.

**Which file should I download?**  
- If you **don’t** want to install .NET → use **self-contained**.  
- If you **have .NET 8 Desktop Runtime** → use **framework-dependent**.

---

## 🔧 Building from source / development

*(Optional — for contributors or advanced users)*

- Requirements: **Windows 10/11**, **.NET 8 SDK**, **Visual Studio 2022** (or just the .NET CLI).
- Publish locally:

```powershell
# Self-contained (no runtime required)
dotnet publish -c Release -r win-x64 `
  /p:PublishSingleFile=true /p:SelfContained=true `
  /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true `
  /p:PublishTrimmed=false -o artifacts/sc

# Framework-dependent (needs .NET 8 Desktop Runtime)
dotnet publish -c Release -r win-x64 `
  /p:PublishSingleFile=true /p:SelfContained=false `
  /p:PublishTrimmed=false -o artifacts/fx
```

---

## ⚙️ Customizing (refresh interval)

The app refreshes every **30 seconds** by default. If you want a different interval, you can edit one constant in the source and rebuild:

```csharp
public const int REFRESH_MS = 30 * 1000; // 30 seconds (milliseconds)
```

Examples:
- 1 minute → `1 * 60 * 1000`
- 30 seconds → `30 * 1000`

---

- **CI/CD:**  
  The workflow builds **both** variants. On tagged pushes (`v*`), it creates a **GitHub Release** and uploads:
  - `TrayRefresher_<version>_win-x64_selfcontained.exe`
  - `TrayRefresher_<version>_win-x64_frameworkdep.exe`

---

## 📜 Why this app exists

For years, many Windows users have experienced tray icons that stop updating until hovered. This behavior still shows up in modern builds under certain conditions. **TrayRefresher** provides a practical, always-on fix without hacks or heavy resource usage.

---

## 📝 License

**MIT** — free to use, modify, and share.
