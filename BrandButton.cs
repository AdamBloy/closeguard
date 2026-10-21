using System.Drawing.Drawing2D; // GraphicsPath, SmoothingMode (for rounded shapes and smooth edges)

// A button that draws itself with rounded corners and a hover color.
// ": Button" means "start from a normal Button and override how it looks".
sealed class BrandButton : Button
{
    // Public fields so ConfirmForm can set the colors when it creates the button.
    public Color Fill;          // normal background color
    public Color HoverFill;     // background color while the mouse is over it
    public Color TextColor;     // label color
    public Color? BorderColor;  // optional outline. The "?" means it can be null (no border).

    bool _hover;                // true while the mouse is over the button

    public BrandButton()
    {
        FlatStyle = FlatStyle.Flat;            // turn off the old 3D Windows look
        FlatAppearance.BorderSize = 0;         // we draw our own border
        Cursor = Cursors.Hand;                 // pointer cursor on hover, like a web link
        Font = new Font("Segoe UI Semibold", 10);
    }

    // Stops Windows drawing the dotted focus rectangle around the button after a keypress.
    protected override bool ShowFocusCues => false;

    // These run when the mouse enters or leaves. Invalidate() = "please repaint me".
    protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    // OnPaint is called whenever the button needs drawing. "e.Graphics" is our canvas,
    // much like a <canvas> 2D context in JavaScript.
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;   // smooth (non-jagged) curves

        // Paint the corners with the form's color first, so the rounded shape
        // sits cleanly on the background.
        g.Clear(Parent?.BackColor ?? BackColor);

        // The shape: a rectangle with 8px rounded corners. Width-1 / Height-1 keeps
        // the outline inside the button's bounds.
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedRect(rect, 8);

        // Fill it. "using var" disposes the brush automatically when we leave the method.
        using (var brush = new SolidBrush(_hover ? HoverFill : Fill))
            g.FillPath(brush, path);

        // Optional outline (used by the secondary button).
        if (BorderColor.HasValue)
            using (var pen = new Pen(BorderColor.Value, 1.5f))
                g.DrawPath(pen, path);

        // Draw the label centred. TextRenderer matches normal Windows text quality.
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, TextColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    // Builds a rounded-rectangle outline from four arcs (one per corner).
    static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = radius * 2;                          // an arc is drawn inside a d x d square
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);                    // top-left corner
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);            // top-right
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);     // bottom-right
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);            // bottom-left
        path.CloseFigure();                                      // join the last point back to the first
        return path;
    }
}