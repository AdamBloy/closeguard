using System.Runtime.InteropServices; // DllImport (calling Windows functions)

sealed class ConfirmForm : Form
{
    // ==================================================================
    // BRAND PALETTE: edit these hex codes to match your icon.
    // ColorTranslator.FromHtml turns "#RRGGBB" into a Color, like CSS.
    // ==================================================================
    static readonly Color Background  = ColorTranslator.FromHtml("#1B1F2A"); // window background
    static readonly Color Accent      = ColorTranslator.FromHtml("#FF6B35"); // your icon's main color
    static readonly Color AccentHover = ColorTranslator.FromHtml("#FF8556"); // slightly lighter, for hover
    static readonly Color TextMain    = ColorTranslator.FromHtml("#FFFFFF"); // heading text
    static readonly Color TextMuted   = ColorTranslator.FromHtml("#9AA3B5"); // secondary text
    static readonly Color Secondary   = ColorTranslator.FromHtml("#272D3B"); // secondary button fill
    static readonly Color SecondaryHover = ColorTranslator.FromHtml("#323A4D");

    // ---- Windows functions used for dragging and rounded corners ----
    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hwnd, int msg, int wParam, int lParam);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    const int WM_NCLBUTTONDOWN = 0xA1; // "the mouse went down on the non-client area"
    const int HTCAPTION = 2;           // "...specifically the title bar" (this makes Windows drag the window)

    public ConfirmForm()
    {
        // ----- Window basics -----
        Text = "Close Guard";                           // still used by screen readers
        Icon = AppIcon.Load();
        FormBorderStyle = FormBorderStyle.None;        // removes the standard Windows title bar and border
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        TopMost = true;                                // stay above Chrome
        BackColor = Background;

        // Scale correctly on high-DPI screens (our layout numbers are for 96 DPI).
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        ClientSize = new Size(420, 190);

        // ----- Accent stripe along the top edge (a branded touch) -----
        var stripe = new Panel
        {
            BackColor = Accent,
            Left = 0, Top = 0, Width = 420, Height = 5
        };

        // ----- Logo -----
        var logo = new PictureBox
        {
            Image = LoadLogo(),
            SizeMode = PictureBoxSizeMode.Zoom,        // scale to fit without stretching
            BackColor = Color.Transparent,
            Left = 24, Top = 28, Width = 64, Height = 64
        };

        // ----- Heading and sub-text -----
        var heading = new Label
        {
            Text = "Close this window?",
            Font = new Font("Segoe UI Semibold", 15),
            ForeColor = TextMain,
            BackColor = Color.Transparent,
            Left = 104, Top = 30, Width = 296, Height = 32
        };

        var subtext = new Label
        {
            Text = "You clicked the close button. Do you want to close it?",
            Font = new Font("Segoe UI", 10),
            ForeColor = TextMuted,
            BackColor = Color.Transparent,
            Left = 104, Top = 64, Width = 296, Height = 44
        };

        // ----- Buttons -----
        // "Keep open" is the filled accent button because it's the safe default.
        var keepOpen = new BrandButton
        {
            Text = "Keep open",
            Fill = Accent, HoverFill = AccentHover, TextColor = Color.White,
            DialogResult = DialogResult.No,            // what ShowDialog() returns when clicked
            Left = 24, Top = 128, Width = 180, Height = 40
        };

        // "Close window" is the quieter outlined button.
        var closeIt = new BrandButton
        {
            Text = "Close window",
            Fill = Secondary, HoverFill = SecondaryHover, TextColor = TextMain,
            BorderColor = ColorTranslator.FromHtml("#3A4357"),
            DialogResult = DialogResult.Yes,
            Left = 216, Top = 128, Width = 180, Height = 40
        };

        // Enter and Esc both mean "Keep open" (safe default).
        AcceptButton = keepOpen;
        CancelButton = keepOpen;

        Controls.AddRange(new Control[] { stripe, logo, heading, subtext, keepOpen, closeIt });

        // Without a title bar the user can't drag the window, so we let them drag
        // by grabbing the background, logo or text.
        foreach (Control c in new Control[] { this, logo, heading, subtext })
            c.MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                ReleaseCapture();                                   // let go of the mouse
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0); // tell Windows "start dragging"
            };

        Shown += (_, _) => Activate(); // pull to the foreground so keyboard focus works
    }

    // Load logo.png from beside the exe. Falls back to the small embedded icon.
    static Image LoadLogo()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "logo.png");
            if (File.Exists(path))
            {
                // Copy into a fresh Bitmap so the file isn't left locked by Windows.
                using var temp = new Bitmap(path);
                return new Bitmap(temp);
            }
        }
        catch { /* ignore and use the fallback below */ }

        return AppIcon.Load().ToBitmap();
    }

    // Runs once the window exists. Here we ask Windows 11 to round the corners.
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        int round = 2; // DWMWCP_ROUND
        // 33 = DWMWA_WINDOW_CORNER_PREFERENCE. On Windows 10 this call does nothing,
        // so the window simply keeps square corners.
        DwmSetWindowAttribute(Handle, 33, ref round, sizeof(int));
    }

    // Adds a soft drop shadow, which borderless windows don't get by default.
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            return cp;
        }
    }
}