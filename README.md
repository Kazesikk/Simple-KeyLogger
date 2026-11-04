# Simple KeyLogger

A tiny, no-admin keylogger for Windows. Run it anywhere, press **Ctrl+Alt+K** to start/stop, and keys are written to `keylog.txt` next to the executable.

## Usage

1. Double-click `Keylogger.exe` (or compile the source).
2. It appears in the system tray.
3. Press **Ctrl + Alt + K** to start recording (high beep) or stop (low beep).
4. Logs are saved to `keylog.txt` beside the `.exe` with millisecond timestamps.

## Compile

```
csc /target:winexe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /out:Keylogger.exe keylogger.cs
```

## Notes

- Uses a low-level keyboard hook (`WH_KEYBOARD_LL`), so it works in every app without stealing focus and needs no admin rights.
- The window is hidden (minimized, off-taskbar); only a tray icon remains. Delete the `NotifyIcon` block in `Main()` to make it fully invisible.