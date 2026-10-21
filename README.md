# Close Guard

A small Windows tray app that asks **"Close this window?"** before a Chrome window is closed with the mouse, so you never lose a window full of tabs to a misclick.

![Close Guard dialog](D:\Development (Production)\Apps\Chrome Closer Blocker\CloseGuard\img\screenshot.png)

# Close Guard

A small Windows app that asks **"Close this window?"** before a Chrome window is closed with the mouse, so a misclick never loses your tabs.

<!-- Add a screenshot of the dialog here:
![Close Guard dialog](docs/screenshot.png)
-->

## Download

### [⬇ Download Close Guard for Windows](https://github.com/AdamBloy/closeguard/releases/latest/download/CloseGuard-Setup.exe)

Windows 10 or 11 (64-bit) · Free · [All versions and release notes](https://github.com/AdamBloy/closeguard/releases)

**How to install:**

1. Click the download link above and run `CloseGuard-Setup.exe`.
2. If Windows shows **"Windows protected your PC"**, click **More info**, then **Run anyway**. This appears because the app isn't code-signed yet, which is normal for small free tools.
3. Follow the steps. The Close Guard icon appears in your system tray (click the `^` arrow near the clock if you can't see it).

## What it does

- Watches for a click on the **X (close) button** of a Chrome window.
- Blocks that click and shows a confirmation dialog instead.
- Closes the window only if you choose **Close window**. **Keep open** (also the Enter and Esc default) leaves everything as it was.
- Sits quietly in the system tray, with an option to start when you sign in.

## What it doesn't do

Close Guard is built to catch _accidental_ mouse clicks, so deliberate actions are left alone:

- **Alt+F4**, **Ctrl+Shift+W**, the taskbar's right-click **Close window**, and Task Manager are **not** intercepted.
- **Incognito windows** are not guarded and close immediately.
- Browsers other than Chrome are not guarded by default (see [Guarding other browsers](#guarding-other-browsers)).

It is a guard against accidents, not a security feature. Anything running on your account can bypass it.

## Privacy

- Listens to **mouse clicks only**. It has no keyboard hook and never sees what you type.
- Makes **no network connections** and collects, stores and sends nothing.
- Reads only the information needed to decide whether a click landed on a Chrome close button: which window is under the cursor, which program owns it, and its title (used to detect incognito).
- Source code is included so you can check all of this yourself.

## Requirements

- Windows 10 or 11, 64-bit
- .NET 8 Desktop Runtime (the installer installs it for you if it's missing)

## Installation

1. Download `CloseGuard-Setup.exe` from the [Releases](https://github.com/AdamBloy/closeguard/releases) page.
2. Run it. If Windows shows **"Windows protected your PC"**, click **More info**, then **Run anyway**. This appears because the installer isn't code-signed yet, which is normal for small free tools.
3. Follow the wizard. Tick **Start Close Guard automatically when I sign in to Windows** if you want it always on.
4. Look for the Close Guard icon in the system tray (you may need to click the `^` arrow to see it).

### Verify your download (optional)

Compare the file's checksum with the one published on the release page. In PowerShell:

```powershell
Get-FileHash .\CloseGuard-Setup.exe -Algorithm SHA256
```

## Using Close Guard

| Action                                          | Result                                 |
| ----------------------------------------------- | -------------------------------------- |
| Click X on a Chrome window                      | Confirmation dialog appears            |
| Choose **Close window**                         | The window closes as normal            |
| Choose **Keep open**, press Enter, or press Esc | Nothing happens                        |
| Right-click the tray icon                       | Toggle **Run at startup**, or **Exit** |

You can drag the dialog by clicking on its background, logo or text.

## Uninstalling

Go to **Settings > Apps > Installed apps**, find **Close Guard**, and choose **Uninstall**. This also removes the startup entry.

## Troubleshooting

**The dialog never appears.**
Check Close Guard is running (tray icon or Task Manager). If it isn't, launch it from the Start menu. Close Guard relies on Chrome reporting its close button to Windows, so it may not work with unusual themes or heavily customised window frames. Please open an issue and say which Chrome version you use.

**Chrome closes immediately without asking.**
Check whether it's an incognito window (not guarded by design) or whether you used a keyboard shortcut (not guarded by design).

**My mouse stopped responding.**
This is very unlikely, but if it happens press `Ctrl+Shift+Esc` to open Task Manager with the keyboard, end `CloseGuard`, and please report it.

**Windows Defender or antivirus flags the installer.**
Tools that watch mouse input can trigger false positives, particularly unsigned ones. Verify the checksum, review the source, and if needed add an exclusion for `C:\Program Files\Close Guard`. Reports of false positives can be submitted to the antivirus vendor.

**Windows protected your PC (SmartScreen).**
Click **More info**, then **Run anyway**.

**Close Guard won't start while installing/updating.**
Exit it from the tray icon first. Only one copy can run at a time.

**It doesn't guard windows that are running as administrator.**
Windows prevents a normal app from intercepting clicks on elevated windows. Chrome normally isn't elevated, so this rarely matters.

## Guarding other browsers

By default only Chrome is guarded. To add other Chromium-based browsers, edit the `Guarded` set in `CloseGuardHook.cs`:

```csharp
static readonly HashSet<string> Guarded =
    new(StringComparer.OrdinalIgnoreCase) { "chrome", "msedge", "brave" };
```

Use the process names shown in Task Manager (without `.exe`). Private-window detection relies on the window title, so you may also need to add that browser's private marker to `PrivateMarkers`. Firefox uses a different window class and isn't supported by this code as written.

## Building from source

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) (only for building the installer)
- VS Code with the **C# Dev Kit** extension (optional)

### Project layout

```
CloseGuard/
├── CloseGuard.csproj      Project settings and exe metadata
├── app.ico                Icon for the exe, tray and installer
├── logo.png               Logo shown in the dialog (512x512 or larger recommended)
├── Native.cs              Windows API declarations
├── CloseGuardHook.cs      Global mouse hook and close-button detection
├── ConfirmForm.cs         The confirmation dialog (branding lives here)
├── BrandButton.cs         Custom rounded button
├── SmoothPictureBox.cs    High-quality image control
├── AppIcon.cs             Loads the embedded icon
├── Startup.cs             "Run at startup" registry helper
├── TrayApp.cs             Tray icon, menu and glue code
├── Program.cs             Entry point and single-instance check
└── installer/
    ├── CloseGuard.iss     Inno Setup script
    ├── wizard-large.bmp   164 x 314, 24-bit BMP
    ├── wizard-small.bmp   55 x 55, 24-bit BMP
    └── redist/
        └── windowsdesktop-runtime-win-x64.exe   (see below)
```

### Run in development

```powershell
dotnet run
```

Exit any installed copy from the tray first. The single-instance check makes a second copy quit silently. Don't set breakpoints inside the mouse callback, because pausing there freezes mouse input system-wide.

### Build the app

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o publish
```

### Build the installer

1. Download the **.NET Desktop Runtime 8.x (x64)** installer from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Save it as `installer/redist/windowsdesktop-runtime-win-x64.exe`.
3. Open `installer/CloseGuard.iss` in Inno Setup and press `Ctrl+F9`, or run:

   ```powershell
   & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\CloseGuard.iss
   ```

The installer is written to `installer/output/CloseGuard-Setup.exe`.

### Customising the look

- **Colours:** edit the palette block at the top of `ConfirmForm.cs`.
- **Wording:** edit the `heading` and `subtext` strings in `ConfirmForm.cs`.
- **Logo and icon:** replace `logo.png` and `app.ico`, then rebuild.
- **Installer text and images:** edit `installer/CloseGuard.iss` and the two BMP files.

## How it works

1. A low-level mouse hook (`WH_MOUSE_LL`) is notified of every mouse click on the system.
2. On a left-button press, the app finds the window under the cursor and checks it is a Chrome window (cheap class-name check first, then the owning process).
3. Incognito windows are skipped by checking for private-browsing wording in the window title.
4. The app sends the window a `WM_NCHITTEST` message asking which part of the window is under the cursor. If the answer is the close button, the click is swallowed and the dialog is shown.
5. If you confirm, a standard `WM_CLOSE` message is posted to the window, exactly as if you'd clicked X, so Chrome still handles its own prompts and session restore.

## Known limitations

- Incognito detection uses the window title, which can vary by Chrome version and language. If your incognito windows still show the dialog, check the title text and add it to `PrivateMarkers`.
- Only the mouse close button is guarded.
- Unsigned builds trigger SmartScreen warnings until the project is code-signed.

## Contributing

Issues and pull requests are welcome. Please keep the app's privacy promises: no keyboard hooks, no network access, no data collection.

## License

Released under the MIT License. See `LICENSE` for details.

Copyright © 2026 Your Name
