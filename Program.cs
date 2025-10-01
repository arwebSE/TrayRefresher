using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Drawing;
using Timer = System.Windows.Forms.Timer;
internal static class Program
{
    public const int REFRESH_MS =  30 * 1000; // every 30s

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new HiddenPump());
    }
}

sealed class HiddenPump : Form
{
    private readonly Timer _tick;
    private readonly uint _taskbarCreated;
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu;

    public HiddenPump()
    {
        ShowInTaskbar = false;
        Opacity = 0;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.Manual;
        SetBounds(-32000, -32000, 1, 1);
        _taskbarCreated = RegisterWindowMessage("TaskbarCreated");

        // Tray icon + menu
        _menu = new ContextMenuStrip();
        var refreshItem = new ToolStripMenuItem("Refresh now", null, (_, __) => RefreshAll());
        var exitItem = new ToolStripMenuItem("Exit", null, (_, __) => Close());
        _menu.Items.Add(refreshItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem($"Auto every {Program.REFRESH_MS / 1000}s") { Enabled = false });
        _menu.Items.Add(exitItem);

        _tray = new NotifyIcon
        {
            Icon = new Icon(Path.Combine(Application.StartupPath, "systray.ico")),
            Visible = true,
            Text = "TrayRefresher",
            ContextMenuStrip = _menu
        };
        _tray.DoubleClick += (_, __) => RefreshAll();

        // Periodic sweep
        _tick = new Timer { Interval = Program.REFRESH_MS };
        _tick.Tick += (_, __) => RefreshAll();
        _tick.Start();

        // Triggers
        Load += (_, __) => RefreshAll();
        SystemEvents.DisplaySettingsChanged += (_, __) => RefreshAll();
        SystemEvents.SessionSwitch += (_, e) =>
        {
            if (e.Reason is SessionSwitchReason.SessionUnlock or SessionSwitchReason.SessionLogon) RefreshAll();
        };
        SystemEvents.PowerModeChanged += (_, e) => { if (e.Mode == PowerModes.Resume) RefreshAll(); };
        FormClosed += (_, __) => { _tick.Stop(); _tray.Visible = false; _tray.Dispose(); _menu.Dispose(); };
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == _taskbarCreated) RefreshAll(); // Explorer restarted
        base.WndProc(ref m);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            const int WS_EX_TOOLWINDOW = 0x80;
            cp.ExStyle |= WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    private static void RefreshAll()
    {
        foreach (var hwnd in GetNotifyToolbars())
        {
            RedrawDeep(hwnd);
            SweepMouse(hwnd);
        }
    }

    private static IEnumerable<IntPtr> GetNotifyToolbars()
    {
        var list = new List<IntPtr>();

        IntPtr AddToolbarUnder(IntPtr parent)
        {
            var sysPager = FindWindowEx(parent, IntPtr.Zero, "SysPager", null);
            var tb = sysPager != IntPtr.Zero
                ? FindWindowEx(sysPager, IntPtr.Zero, "ToolbarWindow32", null)
                : FindWindowEx(parent, IntPtr.Zero, "ToolbarWindow32", null);
            return tb;
        }

        var shell = FindWindow("Shell_TrayWnd", null);
        if (shell != IntPtr.Zero)
        {
            var trayNotify = FindWindowEx(shell, IntPtr.Zero, "TrayNotifyWnd", null);
            var tb = trayNotify != IntPtr.Zero ? AddToolbarUnder(trayNotify) : IntPtr.Zero;
            if (tb != IntPtr.Zero) list.Add(tb);
        }

        var overflow = FindWindow("NotifyIconOverflowWindow", null);
        if (overflow != IntPtr.Zero)
        {
            var tbOverflow = FindWindowEx(overflow, IntPtr.Zero, "ToolbarWindow32", null);
            if (tbOverflow != IntPtr.Zero) list.Add(tbOverflow);
        }

        var sec = FindWindow("Shell_SecondaryTrayWnd", null);
        if (sec != IntPtr.Zero)
        {
            var trayNotify = FindWindowEx(sec, IntPtr.Zero, "TrayNotifyWnd", null);
            var tb = trayNotify != IntPtr.Zero ? AddToolbarUnder(trayNotify) : IntPtr.Zero;
            if (tb != IntPtr.Zero) list.Add(tb);
        }

        return list;
    }

    private static void SweepMouse(IntPtr hwndToolbar)
    {
        const int WM_MOUSEMOVE = 0x0200;
        if (!GetClientRect(hwndToolbar, out var rc)) return;
        int w = Math.Max(1, rc.Right - rc.Left);
        int h = Math.Max(1, rc.Bottom - rc.Top);
        int cols = 12, rows = 3;

        for (int i = 0; i < cols; i++)
            for (int j = 0; j < rows; j++)
            {
                int x = 2 + (i * Math.Max(1, (w - 4) / Math.Max(1, cols - 1)));
                int y = 2 + (j * Math.Max(1, (h - 4) / Math.Max(1, rows - 1)));
                var lParam = (IntPtr)((y << 16) | (x & 0xFFFF)); // client coords
                SendMessageTimeout(hwndToolbar, WM_MOUSEMOVE, IntPtr.Zero, lParam, 0, 50, out _);
            }
    }

    private static void RedrawDeep(IntPtr hwnd)
    {
        const uint RDW_INVALIDATE = 0x0001;
        const uint RDW_ALLCHILDREN = 0x0080;
        const uint RDW_UPDATENOW = 0x0100;
        RedrawWindow(hwnd, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_ALLCHILDREN | RDW_UPDATENOW);
        InvalidateRect(hwnd, IntPtr.Zero, true);
        UpdateWindow(hwnd);
    }

    #region Win32
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string? className, string? windowTitle);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);
    [DllImport("user32.dll")] private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
    [DllImport("user32.dll")] private static extern bool UpdateWindow(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern uint RegisterWindowMessage(string lpString);
    [DllImport("user32.dll")] private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
    #endregion
}