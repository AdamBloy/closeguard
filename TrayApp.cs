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

    // Show the confirmation dialog and act on the answer.
    void Ask(IntPtr hwnd)
    {
        if (_dialogOpen) return; // already showing one
        _dialogOpen = true;

        try
        {
            using var dialog = new ConfirmForm();

            // ShowDialog() blocks until the user clicks a button, then returns which one.
            // We only close Chrome if:
            //   - the user chose "Close window", AND
            //   - the window still exists, AND
            //   - the handle still belongs to a guarded program (handles can be reused).
            if (dialog.ShowDialog() == DialogResult.Yes
                && IsWindow(hwnd)
                && CloseGuardHook.IsGuardedProcess(hwnd))
            {
                // WM_CLOSE = polite request to close, same as Chrome's own X button.
                // Chrome can still show its own "close all tabs?" prompts.
                PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }
        finally
        {
            // "finally" always runs, even after errors, so we never get stuck "open".
            _dialogOpen = false;
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