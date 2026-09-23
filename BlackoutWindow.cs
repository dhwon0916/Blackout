namespace Blackout;

internal sealed class BlackoutWindow : Form
{
    public BlackoutWindow(Rectangle bounds, Action restore, Action restoreAll)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = bounds;
        BackColor = Color.Black;
        ShowInTaskbar = false;
        TopMost = true;
        AutoScaleMode = AutoScaleMode.None;
        DoubleBuffered = false;
        AccessibleName = "Blacked-out monitor. Double-click to restore.";
        MouseDoubleClick += (_, _) => restore();
        var menu = new ContextMenuStrip();
        menu.Items.Add("Restore this monitor", null, (_, _) => restore());
        menu.Items.Add("Restore all monitors", null, (_, _) => restoreAll());
        ContextMenuStrip = menu;
        Disposed += (_, _) => menu.Dispose();
    }
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ExStyle |= 0x08000000 | 0x00000080; return cp; }
    }
}
