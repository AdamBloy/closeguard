using Microsoft.Win32; // Registry classes

static class Startup
{
    // Windows launches every program listed under this registry key when the user signs in.
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    // The name of our entry inside that key. The installer uses the same name.
    const string ValueName = "CloseGuard";

    // A property with only a "get": reading Startup.IsEnabled runs this code.
    public static bool IsEnabled
    {
        get
        {
            // HKEY_CURRENT_USER only affects the current user and needs no admin rights.
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) != null; // "?." = skip if key is null
        }
    }

    // Turn "run at login" on or off.
    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)!;

        if (enabled)
            // The path is wrapped in quotes in case it contains spaces ("Program Files").
            // $"..." is string interpolation, like a JS template literal `...${x}...`.
            key.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}