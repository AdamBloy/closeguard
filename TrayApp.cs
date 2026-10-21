using static Native;

// ApplicationContext lets an app run with no main window, living only in the tray.
sealed class TrayApp : ApplicationContext
{
    readonly NotifyIcon _tray;        // the tray icon itself
    readonly CloseGuardHook _hook;    // the mouse listener
    readonly Control _ui = new();     // an invisible control used to hop back onto the UI thread
    bool _dialogOpen;                 // prevents stacking several dialogs

    public TrayApp()
    {
        // Touching .Handle forces the invisible control to create its window handle.
        // Without this, BeginInvoke below wouldn't work. "_ =" means "discard the result".
        _ = _ui.Handle;

        // ----- Tray right-click menu -----
        var startupItem = new ToolStripMenuItem("Run at startup")
        {
            Checked = Startup.IsEnabled, // tick it if startup is currently on
            CheckOnClick = true          // clicking toggles the tick
        };
        // "+=" subscribes to an event, like addEventListener. Fires when the tick changes.
        startupItem.CheckedChanged += (_, _) => Startup.Set(startupItem.Checked);

        var menu = new ContextMenuStrip();
        menu.Items.Add(startupItem);
        // Items.Add(text, image, onClick). ExitThread() ends the app.
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        // ----- Tray icon -----
        _tray = new NotifyIcon
        {
            Icon = AppIcon.Load(),
            Text = "Close Guard",       // tooltip on hover
            ContextMenuStrip = menu,
            Visible = true
        };

        // ----- Start listening to the mouse -----
        _hook = new CloseGuardHook();

        // When the hook reports a guarded close click, show the dialog.
        // BeginInvoke queues Ask() to run on the UI thread AFTER the hook callback returns.
        // This matters: Windows removes hooks whose callbacks take too long, and a modal
        // dialog blocks until the user answers.
        _hook.CloseRequested += hwnd => _ui.BeginInvoke(() => Ask(hwnd));
    }

    // "async" lets us wait for the background check without freezing the UI.
    async void Ask(IntPtr hwnd)
    {
        if (_dialogOpen) return; // already handling one
        _dialogOpen = true;

        try
        {
            // Run the incognito check on a background thread (UI Automation can take a moment
            // and works best off the UI thread). Give up after 1.5 seconds.
            var check = Task.Run(() => IncognitoDetector.IsIncognito(hwnd));
            bool finished = await Task.WhenAny(check, Task.Delay(1500)) == check;
            bool incognito = finished && check.Result; // timed out = treat as a normal window

            // The window may have vanished while we checked, so validate the handle again.
            if (!IsWindow(hwnd) || !CloseGuardHook.IsGuardedProcess(hwnd)) return;

            if (incognito)
            {
                // Incognito: no dialog, just carry out the close the user asked for.
                PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                return;
            }

            using var dialog = new ConfirmForm();
            if (dialog.ShowDialog() == DialogResult.Yes
                && IsWindow(hwnd)
                && CloseGuardHook.IsGuardedProcess(hwnd))
            {
                PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }
        finally
        {
            _dialogOpen = false; // always reset, even after an early return or error
        }
    }

    // Runs when the app exits (tray menu > Exit). Clean up so no ghost tray icon is left behind.
    protected override void ExitThreadCore()
    {
        _tray.Visible = false;
        _tray.Dispose();
        _hook.Dispose();
        base.ExitThreadCore(); // let the base class finish the shutdown
    }
}