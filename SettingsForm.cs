namespace Blackout;

internal sealed class SettingsForm : Form
{
    private readonly TrayContext context;
    private readonly FlowLayoutPanel monitors = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly Dictionary<string, TextBox> editors = [];
    private readonly Dictionary<string, Button> toggles = [];
    private readonly TextBox restore = new() { Width = 175 };
    private readonly CheckBox startup = new() { Text = "Start Blackout when I sign in", AutoSize = true };
    private readonly Label status = new() { Dock = DockStyle.Fill, AutoSize = false, ForeColor = Color.FromArgb(82, 98, 113) };

    public SettingsForm(TrayContext context)
    {
        this.context = context;
        AutoScaleMode = AutoScaleMode.None;
        Text = "Blackout";
        Font = new Font("Segoe UI", 14, GraphicsUnit.Pixel);
        BackColor = Color.FromArgb(247, 248, 250);
        ClientSize = new Size(590, 510);
        MinimumSize = new Size(590, 510);
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Icon = AppIcon.Value;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 7 };
        foreach (var size in new[] { 46f, 46f, 0f, 48f, 36f, 48f, 55f })
            layout.RowStyles.Add(size == 0 ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.Absolute, size));
        layout.Controls.Add(new Label { Text = "Blackout", Font = new Font("Segoe UI Semibold", 30, GraphicsUnit.Pixel), AutoSize = true }, 0, 0);
        layout.Controls.Add(new Label { Text = "A little quiet for your extra screens.\nToggle a monitor again to bring it back instantly.", AutoSize = true, ForeColor = Color.FromArgb(82, 98, 113) }, 0, 1);
        layout.Controls.Add(monitors, 0, 2);
        var restoreRow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };
        restoreRow.Controls.Add(new Label { Text = "Restore all hotkey", AutoSize = true, Margin = new Padding(0, 4, 18, 0) });
        restoreRow.Controls.Add(restore);
        layout.Controls.Add(restoreRow, 0, 3);
        layout.Controls.Add(startup, 0, 4);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var save = MakeButton("Save settings", 135);
        save.BackColor = Color.FromArgb(40, 69, 91);
        save.ForeColor = Color.White;
        save.Click += (_, _) => Save();
        var all = MakeButton("Restore all", 115);
        all.Click += (_, _) => context.RestoreAll();
        var hide = MakeButton("Hide to tray", 120);
        hide.Click += (_, _) => Hide();
        buttons.Controls.AddRange([save, all, hide]);
        layout.Controls.Add(buttons, 0, 5);
        layout.Controls.Add(status, 0, 6);
        Controls.Add(layout);
        context.Changed += RefreshStates;
        Disposed += (_, _) => context.Changed -= RefreshStates;
        FormClosing += (_, e) => { if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); } };
        Reload();
    }
    private static Button MakeButton(string text, int width) => new() { Text = text, Width = width, Height = 34, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0) };
    public void Reload()
    {
        foreach (Control control in monitors.Controls.Cast<Control>().ToArray()) control.Dispose();
        editors.Clear(); toggles.Clear();
        for (int i = 0; i < context.Screens.Length; i++)
        {
            var screen = context.Screens[i];
            var row = new Panel { Width = 505, Height = 78, BackColor = Color.White, Margin = new Padding(0, 4, 0, 6) };
            row.Controls.Add(new Label { Text = $"Monitor {i + 1}{(screen.Primary ? " · Primary" : "")}", Font = new Font(Font, FontStyle.Bold), Location = new Point(12, 10), AutoSize = true });
            row.Controls.Add(new Label { Text = $"{screen.Bounds.Width} × {screen.Bounds.Height}  ·  {screen.DeviceName.Replace(@"\\.\", "")}", Location = new Point(12, 38), AutoSize = true, ForeColor = Color.DimGray });
            var editor = new TextBox { Text = context.Settings.Monitors.First(m => m.Device == screen.DeviceName).Hotkey, Location = new Point(235, 25), Width = 140, AccessibleName = $"Monitor {i + 1} hotkey" };
            var toggle = MakeButton("Black out", 115);
            toggle.Location = new Point(382, 23);
            toggle.Click += (_, _) => context.Toggle(screen.DeviceName);
            row.Controls.Add(editor); row.Controls.Add(toggle);
            monitors.Controls.Add(row);
            editors.Add(screen.DeviceName, editor); toggles.Add(screen.DeviceName, toggle);
        }
        restore.Text = context.Settings.RestoreHotkey;
        try { startup.Checked = Startup.Enabled; } catch (Exception ex) { SetStatus(ex.Message, true); }
        RefreshStates();
        SetStatus("Type hotkeys, then save. Leave a monitor hotkey blank to disable it.\nDouble-click a black screen to restore it. Closing this window keeps the tray app running.");
    }
    private void RefreshStates() { foreach (var pair in toggles) pair.Value.Text = context.IsBlack(pair.Key) ? "Restore" : "Black out"; }
    public void SetStatus(string text, bool error = false) { status.Text = text; status.ForeColor = error ? Color.Firebrick : Color.FromArgb(82, 98, 113); }
    private void Save()
    {
        try
        {
            var next = new Settings { RestoreHotkey = Hotkey.Parse(restore.Text).ToString(), Monitors = context.Settings.Monitors.Select(m => new MonitorSetting { Device = m.Device, Hotkey = m.Hotkey }).ToList() };
            foreach (var pair in editors) next.Monitors.First(m => m.Device == pair.Key).Hotkey = string.IsNullOrWhiteSpace(pair.Value.Text) ? "" : Hotkey.Parse(pair.Value.Text).ToString();
            context.Apply(next, startup.Checked);
            restore.Text = next.RestoreHotkey;
            foreach (var pair in editors) pair.Value.Text = next.Monitors.First(m => m.Device == pair.Key).Hotkey;
            SetStatus("Settings saved. Your hotkeys are active everywhere.");
        }
        catch (Exception ex) { SetStatus(ex.Message, true); }
    }
}
