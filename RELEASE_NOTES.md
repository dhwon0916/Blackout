# Blackout 1.0.1

A lightweight Windows tray app for blacking out individual monitors without changing your desktop layout.

## Download and run

Download **Blackout-1.0.1-win-x64.zip**, extract the entire archive into a permanent folder, and open **Blackout.exe**. This package includes the .NET runtime; no separate runtime installation is required.

## New in 1.0.1

Custom monitor-and-crescent icon for the executable, settings window, and system tray, with nine sizes for Windows display scaling.

## Features

- Per-monitor blackout with instant restore.
- Customizable global hotkeys: Ctrl+Alt+1 through Ctrl+Alt+9 for monitors and Ctrl+Alt+R to restore all by default.
- Tray controls and a compact settings window.
- Optional startup at Windows sign-in.
- Double-click or right-click a black screen for recovery.
- Automatic restore when the display configuration changes.
- Event-driven native WinForms implementation with no background polling.

## Notes

For Windows 10/11 x64. Blackout covers screens visually; it does not turn off monitor power or LCD backlights. It is not a security lock, and some system or exclusive fullscreen windows may appear above it.

The app is unsigned. Startup is disabled by default; enable it in settings after placing the app in its permanent folder.

## Validation

Release build and 14 self-checks passed, including Windows hotkey registration conflicts and rollback, settings persistence, monitor reconciliation, and overlay geometry. A live monitor blackout and restore-all hotkey cycle was also verified.

