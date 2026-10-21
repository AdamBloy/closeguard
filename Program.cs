static class Program
{
    // [STAThread] is required by WinForms. It just means "use the single-threaded UI model".
    [STAThread]
    static void Main()
    {
        // A named mutex ensures only one copy runs. Think of it as a system-wide flag.
        // "out bool isFirst" is true if we created it, false if another copy already holds it.
        // The installer also uses this name (AppMutex) to detect a running copy.
        using var mutex = new Mutex(true, "CloseGuard.SingleInstance", out bool isFirst);
        if (!isFirst) return; // another copy is already running, so quit silently

        // Sets up WinForms (DPI mode, fonts, etc.) from the csproj settings.
        ApplicationConfiguration.Initialize();

        // Start the tray app. This blocks until the user chooses Exit.
        Application.Run(new TrayApp());
    }
}