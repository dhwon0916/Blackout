# Blackout

A small Windows tray utility that covers individual monitors with black windows. Other apps keep running and the desktop layout stays intact.

## Run

Download **Blackout-1.0.0-win-x64.zip** from this repository's **Releases** page, extract the entire archive into a permanent folder, and open `Blackout.exe`. The release supports Windows 10/11 x64 and includes the .NET runtime. Close the settings window to keep Blackout in the tray; double-click the tray icon to reopen it. Use **Quit Blackout** in the tray menu to exit and restore every screen.

If you build the smaller framework-dependent package in `dist`, it requires the .NET 10 Windows Desktop Runtime. The app is unsigned.

- **Ctrl+Alt+1**, **Ctrl+Alt+2**, etc.: toggle the corresponding monitor (first nine monitors by default).
- **Ctrl+Alt+R**: restore all monitors.
- Double-click a black screen to restore that monitor, or right-click for recovery options.
- Customize hotkeys in settings, then choose **Save settings**. An empty monitor binding disables that shortcut. The restore-all binding is required. Conflicting shortcuts are rejected.
- Enable **Start Blackout when I sign in** and save to start silently in the tray. Keep the app in a permanent location before enabling this; save again if you move it.

The display name in each monitor row matches the Windows display device. Display configuration changes restore all monitors automatically. Monitor bindings are remembered by Windows display device name; drivers may renumber these after hardware changes, so review assignments after reconnecting displays.

## Behavior and resource use

Native WinForms, no external packages, no web runtime, timers, polling, animations, or image buffers. Only blacked-out monitors get overlay windows; restoring disposes them. Windows delivers hotkey and display-change events while the application is idle. One instance per Windows session.

This is a visual blackout, not hardware power-off: LCD backlights stay on, the pointer may remain visible, and exclusive fullscreen apps, elevated system windows, or the secure desktop can appear above an overlay. It is not a privacy or security lock. Normal clicks are intercepted by the overlay; it never deliberately changes focus when shown. Startup is opt-in and uses the current user's Windows Run registry key; no administrator access is required. Settings are stored in `%LOCALAPPDATA%\Blackout\settings.json`.

## Build and check

With the .NET 10 SDK on Windows:

```powershell
dotnet build -c Release
dotnet publish -c Release --no-self-contained -o dist
$test = Start-Process -FilePath .\dist\Blackout.exe -ArgumentList '--self-test' -Wait -PassThru -WindowStyle Hidden
Get-Content .\dist\self-test-results.txt
```

Self-tests cover hotkey parsing, actual Windows hotkey conflicts and rollback, monitor reconciliation, settings persistence, corrupt settings, and overlay geometry. They never black out a display or modify startup settings.

To build the self-contained release archive and SHA-256 checksum:

```powershell
.\package.ps1
```

The package and checksum are written under `artifacts`. The script runs the self-tests before packaging.

Manual checks: toggle each connected display and restore it with the same shortcut; restore all; double-click and right-click an overlay; verify tray hiding, shortcut edits, startup on next sign-in, and monitor reconnects. Check mixed DPI and fullscreen apps on your own display configuration.

To remove: disable startup and save, quit from the tray, then delete the app folder. Optionally delete `%LOCALAPPDATA%\Blackout` to remove saved settings.
