using System.Windows.Automation; // UI Automation: lets us inspect another program's interface

// Works out whether a Chrome window is incognito by looking for the
// incognito indicator button in its toolbar.
static class IncognitoDetector
{
    // Words that mark the incognito indicator. See "Step 5" if this doesn't match.
    const string Marker = "Incognito";

    public static bool IsIncognito(IntPtr hwnd)
    {
        try
        {
            // Turn the raw window handle into a UIA element, the root of the window's interface tree.
            AutomationElement root = AutomationElement.FromHandle(hwnd);
            return Search(root, 0);
        }
        catch
        {
            // Window closed mid-check, access denied, etc. "Not incognito" is the safe
            // answer, because the worst case is that you see the dialog.
            return false;
        }
    }

    // Walks the interface tree looking for a button whose name contains the marker.
    static bool Search(AutomationElement parent, int depth)
    {
        if (depth > 8) return false; // don't dig forever

        var walker = TreeWalker.ControlViewWalker;                  // walks the tree one node at a time
        AutomationElement? child = walker.GetFirstChild(parent);

        while (child != null)
        {
            ControlType type = child.Current.ControlType;

            // Skip the web page itself. A site could contain a button called "Incognito",
            // and we only want Chrome's own toolbar, not page content.
            if (type != ControlType.Document)
            {
                if (type == ControlType.Button &&
                    child.Current.Name.Contains(Marker, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (Search(child, depth + 1)) return true; // look inside this element too
            }

            child = walker.GetNextSibling(child);
        }
        return false;
    }
}