static class AppIcon
{
    // Returns the icon embedded in CloseGuard.exe (the one from app.ico),
    // or a built-in shield icon if that fails for any reason.
    public static Icon Load()
    {
        try
        {
            // Environment.ProcessPath = full path of the running exe.
            // "!" tells the compiler "I promise this isn't null".
            // "??" is the null-coalescing operator, same as in modern JS:
            // use the right side if the left side is null.
            return Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Shield;
        }
        catch
        {
            return SystemIcons.Shield;
        }
    }
}