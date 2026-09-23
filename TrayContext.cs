using Microsoft.Win32;

namespace Blackout;

internal sealed class TrayContext : ApplicationContext
{
    private readonly HotkeyWindow hotkeys = new();
    private readonly Dictionary<string, BlackoutWindow> overlays = [];
    private readonly NotifyIcon tray;
    private readonly Control dispatcher = new();
    private SettingsForm? form;
    public Settings Settings { get; private set; }
    public Screen[] Screens { get; private set; } = Screen.AllScreens.OrderBy(s => s.DeviceName, StringComparer.Ordinal).ToArray();
    public event Action? Changed;
    public bool IsBlack(string device) => overlays.ContainsKey(device);

    public TrayContext(bool show)
    {
        _ = dispatcher.Handle;
        string? warning = null;
        try { Settings = Settings.Load(); }
        catch (Exception ex) { Settings = new(); warning = "Saved settings could not be loaded. Defaults are in use. " + ex.Message; }
        Settings.Reconcile(Screens);
        tray = new NotifyIcon { Text = "Blackout • monitor control", Icon = AppIcon.Value, Visible = true };
        tray.DoubleClick += (_, _) => ShowSettings();
        hotkeys.ShowRequested += ShowSettings;
        BuildMenu();
        try { hotkeys.Replace(Bindings(Settings)); }
        catch (Exception ex) { warning = ex.Message; }
        SystemEvents.DisplaySettingsChanged += DisplayChanged;
        if (show || warning != null) ShowSettings();
        if (warning != null) form!.SetStatus(warning, true);
    }

    private List<(Hotkey Key, Action Action)> Bindings(Settings settings)
    {
        var bindings = new List<(Hotkey, Action)> { (Hotkey.Parse(settings.RestoreHotkey), RestoreAll) };
        foreach (var monitor in settings.Monitors.Where(m => Screens.Any(s => s.DeviceName == m.Device)))
        {
            string device = monitor.Device;
            if (!string.IsNullOrWhiteSpace(monitor.Hotkey)) bindings.Add((Hotkey.Parse(monitor.Hotkey), () => Toggle(device)));
        }
        return bindings;
    }

    public void Apply(Settings next, bool startup)
    {
        var previous = Settings;
        bool wasStartup = Startup.Enabled;
        hotkeys.Replace(Bindings(next));
        try
        {
            Startup.Set(startup);
            next.Save();
        }
        catch (Exception ex)
        {
            var errors = new List<string> { ex.Message };
            try { Startup.Set(wasStartup); } catch (Exception rollback) { errors.Add(rollback.Message); }
            try { hotkeys.Replace(Bindings(previous)); } catch (Exception rollback) { errors.Add(rollback.Message); }
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }
        Settings = next;
        BuildMenu();
    }

    public void Toggle(string device)
    {
        if (overlays.Remove(device, out var existing)) existing.Dispose();
        else
        {
            var screen = Screens.FirstOrDefault(s => s.DeviceName == device);
            if (screen == null) return;
            var overlay = new BlackoutWindow(screen.Bounds, () => Toggle(device), RestoreAll);
            overlays.Add(device, overlay);
            overlay.Show();
        }
        Changed?.Invoke();
        BuildMenu();
    }
    public void RestoreAll()
    {
        foreach (var overlay in overlays.Values) overlay.Dispose();
        overlays.Clear();
        Changed?.Invoke();
        BuildMenu();
    }
    public void ShowSettings()
    {
        RestoreAll();
        if (form == null || form.IsDisposed) form = new SettingsForm(this);
        form.Show();
        form.WindowState = FormWindowState.Normal;
        form.Activate();
    }
    private void BuildMenu()
    {
        var old = tray.ContextMenuStrip;
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open Blackout", null, (_, _) => ShowSettings());
        menu.Items.Add("Restore all monitors", null, (_, _) => RestoreAll());
        menu.Items.Add(new ToolStripSeparator());
        for (int i = 0; i < Screens.Length; i++)
        {
            string device = Screens[i].DeviceName;
            string key = Settings.Monitors.First(m => m.Device == device).Hotkey;
            menu.Items.Add($"{(IsBlack(device) ? "Restore" : "Black out")} monitor {i + 1}    {key}", null, (_, _) => Toggle(device));
        }
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit Blackout", null, (_, _) => ExitThread());
        tray.ContextMenuStrip = menu;
        old?.Dispose();
    }
    private void DisplayChanged(object? sender, EventArgs e)
    {
        if (dispatcher.IsDisposed) return;
        try { dispatcher.BeginInvoke(() =>
        {
            RestoreAll();
            Screens = Screen.AllScreens.OrderBy(s => s.DeviceName, StringComparer.Ordinal).ToArray();
            Settings.Reconcile(Screens);
            string? error = null;
            try { hotkeys.Replace(Bindings(Settings)); } catch (Exception ex) { error = ex.Message; }
            BuildMenu();
            form?.Reload();
            if (error != null) { ShowSettings(); form!.SetStatus(error, true); }
        }); } catch (InvalidOperationException) { }
    }
    protected override void ExitThreadCore()
    {
        SystemEvents.DisplaySettingsChanged -= DisplayChanged;
        RestoreAll();
        form?.Dispose();
        tray.Visible = false;
        tray.ContextMenuStrip?.Dispose();
        tray.Dispose();
        hotkeys.Dispose();
        dispatcher.Dispose();
        base.ExitThreadCore();
    }
}
