using System.Runtime.InteropServices; // gives us DllImport, StructLayout, etc.
using System.Text;                    // StringBuilder (used to receive text from Windows)

// "static class" = a container of things you use without creating an object,
// similar to a plain JS object holding constants and functions.
static class Native
{
    // ---------------------------------------------------------------
    // CONSTANTS
    // "const" = a value that never changes (like const in JS).
    // These numbers are defined by Windows, we just give them readable names.
    // ---------------------------------------------------------------

    public const int WH_MOUSE_LL = 14;        // "install a global low-level MOUSE hook"
    public const int WM_LBUTTONDOWN = 0x0201; // message: left mouse button pressed
    public const int WM_LBUTTONUP = 0x0202;   // message: left mouse button released
    public const int WM_NCHITTEST = 0x0084;   // message: "what part of your window is at this point?"
    public const int WM_CLOSE = 0x0010;       // message: "please close yourself"
    public const int HTCLOSE = 20;            // answer to NCHITTEST: "that's the Close (X) button"

    public const uint GA_ROOT = 2;            // GetAncestor flag: "give me the top-level window"
    public const uint SMTO_ABORTIFHUNG = 0x0002; // SendMessageTimeout flag: give up if the target is frozen

    // ---------------------------------------------------------------
    // TYPES
    // ---------------------------------------------------------------

    // A "delegate" is a function type. This describes the shape of the function
    // Windows will call whenever a mouse event happens (our "event listener").
    //   nCode  : if negative, we must ignore the event
    //   wParam : which mouse event (button down, button up, move...)
    //   lParam : a pointer to extra data about the event
    // IntPtr = a pointer-sized integer, used here as a raw memory address or handle.
    public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    // A "struct" is a plain data record with a fixed memory layout.
    // This one is an X/Y screen position.
    [StructLayout(LayoutKind.Sequential)] // fields must sit in memory in this exact order, as Windows expects
    public struct POINT
    {
        public int X, Y;
    }

    // The data Windows gives us for each low-level mouse event.
    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;            // where the cursor was on screen
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // ---------------------------------------------------------------
    // WINDOWS FUNCTIONS WE CALL
    // [DllImport("user32.dll")] means "this function lives in user32.dll".
    // "extern" means "the body isn't in C#, it's elsewhere".
    // ---------------------------------------------------------------

    // Install a hook (register our listener for system-wide mouse events).
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint threadId);

    // Remove a hook (stop listening).
    [DllImport("user32.dll")]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    // Pass an event along to the next listener in the chain (i.e. let the click through).
    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    // Get a handle to our own program (the hook installer needs it).
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr GetModuleHandle(string? name);

    // Which window is under this screen point?
    [DllImport("user32.dll")]
    public static extern IntPtr WindowFromPoint(POINT p);

    // From a child window, get its top-level parent window.
    [DllImport("user32.dll")]
    public static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    // Which process (program) owns this window?
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    // Read a window's "class name". Chrome's windows are all "Chrome_WidgetWin_1".
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(IntPtr hwnd, StringBuilder className, int maxCount);

    // Send a message to a window and wait for its reply, but give up after a timeout.
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam,
        uint flags, uint timeoutMs, out IntPtr result);

    // Drop a message in a window's queue without waiting (used to politely ask it to close).
    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    // Is this window handle still valid? (Windows can reuse handles after a window closes.)
    [DllImport("user32.dll")]
    public static extern bool IsWindow(IntPtr hwnd);

    // Read a window's title bar text into a buffer.
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);
}