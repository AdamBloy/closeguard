using System.Diagnostics;            // Process class (to find which program owns a window)
using System.Runtime.InteropServices; // Marshal (to read raw memory)
using System.Text;                   // StringBuilder
using static Native;                 // lets us write SetWindowsHookEx(...) instead of Native.SetWindowsHookEx(...)

// "sealed" = nothing can inherit from this class.
// ": IDisposable" = this class has cleanup work to do (removing the hook) via Dispose().
sealed class CloseGuardHook : IDisposable
{
    // The programs we protect. HashSet = a collection of unique values (like a JS Set).
    // StringComparer.OrdinalIgnoreCase = compare names ignoring upper/lower case.
    // To protect other browsers, add their process names, e.g. "msedge", "firefox", "brave".
    static readonly HashSet<string> Guarded =
        new(StringComparer.OrdinalIgnoreCase) { "chrome" };

    // IMPORTANT: we keep our callback in a field. If we only passed it to Windows,
    // C#'s garbage collector could delete it, and the app would crash on the next click.
    readonly HookProc _mouseProc;

    // The handle Windows gives us for our hook, needed later to remove it.
    readonly IntPtr _mouseHook;

    // When we swallow a mouse-DOWN we must also swallow the matching mouse-UP,
    // otherwise Chrome would see a lone "button released" event.
    bool _swallowMouseUp;

    // An "event" others can subscribe to (like emitter.on('closeRequested', handler) in JS).
    // Action<IntPtr> = a function that takes one IntPtr (the window handle) and returns nothing.
    // The "?" means it may be null (nobody subscribed).
    public event Action<IntPtr>? CloseRequested;

    // Constructor: runs when we write "new CloseGuardHook()".
    public CloseGuardHook()
    {
        _mouseProc = MouseCallback; // point the field at our callback method below

        // Register for system-wide low-level mouse events.
        // threadId 0 = "listen to every thread", i.e. the whole desktop.
        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(null), 0);
    }

    // ---------------------------------------------------------------
    // Windows calls this for EVERY mouse event on the system (moves, clicks, wheel...).
    // It must return quickly or Windows removes the hook, so we keep it light.
    // ---------------------------------------------------------------
    IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // try/catch: if anything throws inside a hook callback the whole app can crash.
        // We catch everything and fall through to "let the click through".
        try
        {
            // Negative nCode means "don't process this", so only act when it's 0 or more.
            if (nCode >= 0)
            {
                int message = (int)wParam; // which mouse event is this?

                if (message == WM_LBUTTONDOWN) // left button pressed
                {
                    // lParam is a raw memory pointer. This line reads it as an MSLLHOOKSTRUCT,
                    // which gives us the click's screen coordinates.
                    var info = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                    // Was the click on the close button of a guarded window?
                    IntPtr window = FindGuardedCloseButton(info.pt);

                    if (window != IntPtr.Zero) // IntPtr.Zero = "nothing found"
                    {
                        _swallowMouseUp = true;

                        // Tell subscribers (TrayApp) which window was clicked.
                        // "?." means "only call if someone subscribed".
                        CloseRequested?.Invoke(window);

                        // Returning 1 means "swallow this click", so Chrome never receives it.
                        return (IntPtr)1;
                    }
                }
                else if (message == WM_LBUTTONUP && _swallowMouseUp)
                {
                    // Swallow the release that belongs to the click we swallowed.
                    _swallowMouseUp = false;
                    return (IntPtr)1;
                }
            }
        }
        catch
        {
            // Deliberately empty: never let an error escape a hook callback.
        }

        // Anything we didn't swallow is passed on untouched, so normal clicks work as usual.
        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    // If the point is on the close button of a guarded window, return that window's handle.
    // Otherwise return IntPtr.Zero. "static" = needs no instance data.
    static IntPtr FindGuardedCloseButton(POINT pt)
    {
        // Which window is under the cursor?
        IntPtr hwnd = WindowFromPoint(pt);
        if (hwnd == IntPtr.Zero) return IntPtr.Zero;

        // Get the top-level window (the one with the title bar).
        IntPtr root = GetAncestor(hwnd, GA_ROOT);
        if (root == IntPtr.Zero) return IntPtr.Zero;

        // CHEAP CHECKS FIRST. This function runs on every click in every app,
        // so we bail out as early as possible for anything that isn't Chrome.
        //   1) Class-name check is very fast.
        //   2) Process-name check is slower, so it only runs if (1) passed.
        if (!IsChromiumWindowClass(root) || !IsGuardedProcess(root))
            return IntPtr.Zero;

        // Ask the window: "what part of you is at this point?"
        // Windows packs X and Y into one number: Y in the high 16 bits, X in the low 16.
        // "<<" shifts bits left, "&" and "|" are bitwise AND / OR (same as in JS).
        var lParam = new IntPtr((pt.Y << 16) | (pt.X & 0xFFFF));

        if (!IsChromiumWindowClass(root) || !IsGuardedProcess(root))
    return IntPtr.Zero;

        // NEW: skip incognito/private windows. Returning Zero means "not a guarded click",
        // so the click passes straight through and the window closes immediately, no dialog.
        if (IsPrivateWindow(root))
        
        
            return IntPtr.Zero;
            
            

        // "out var result" = the function writes its answer into a new variable called result.
        // If the window is frozen, we give up after 100 ms instead of freezing the mouse.
        if (SendMessageTimeout(root, WM_NCHITTEST, IntPtr.Zero, lParam,
                SMTO_ABORTIFHUNG, 100, out var result) == IntPtr.Zero)
            return IntPtr.Zero;

        // If the answer is HTCLOSE, the click was on the X button.
        // "a ? b : c" is the same ternary operator as in JS.
        return (int)result == HTCLOSE ? root : IntPtr.Zero;
    }

    // Fast check: Chrome, Edge, Brave (and Electron apps like VS Code) share this window class.
    // It's only a pre-filter; IsGuardedProcess decides for real.
    static bool IsChromiumWindowClass(IntPtr hwnd)
    {
        var name = new StringBuilder(256); // a text buffer Windows writes into
        return GetClassName(hwnd, name, name.Capacity) > 0
               && name.ToString() == "Chrome_WidgetWin_1";
    }

    // Words browsers append to the title of private windows.
// Chrome: "(Incognito)", Edge: "InPrivate", Brave: "Private" (e.g. "Private Window").
// Add more here if you guard other browsers.
static readonly string[] PrivateMarkers =
{
    "(Incognito)",     // some Chrome versions append this to page titles
    "Incognito tab",   // the new-tab page in incognito: "New Incognito tab"

};

// Returns true if the window's title shows it's a private/incognito window.
static bool IsPrivateWindow(IntPtr hwnd)
{
    var title = new StringBuilder(512);              // text buffer Windows writes into
    if (GetWindowText(hwnd, title, title.Capacity) <= 0)
        return false;                                // no title found, so assume a normal window

    string text = title.ToString();

   

    // Check whether the title contains any of the markers (case-insensitive).
    // "foreach" loops over the array, like for...of in JS.
    foreach (var marker in PrivateMarkers)
        if (text.Contains(marker, StringComparison.OrdinalIgnoreCase))
            return true;

    return false;
}

    // Is this window owned by one of the programs in the Guarded set?
    // "internal" = visible to other files in this project (TrayApp uses it too).
    internal static bool IsGuardedProcess(IntPtr hwnd)
    {
        try
        {
            GetWindowThreadProcessId(hwnd, out uint pid); // find the owning process id

            // "using var" = automatically clean up the Process object when we leave this block.
            using var process = Process.GetProcessById((int)pid);

            // ProcessName is e.g. "chrome" (no ".exe").
            return Guarded.Contains(process.ProcessName);
        }
        catch
        {
            // The process may have just exited, or we lack permission to inspect it.
            return false;
        }
    }

    // Called when the app shuts down: stop listening to the mouse.
    public void Dispose() => UnhookWindowsHookEx(_mouseHook);
}