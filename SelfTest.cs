namespace Blackout;

internal static class SelfTest
{
    public static int Run()
    {
        var results = new List<string>();
        string temp = Path.Combine(Path.GetTempPath(), "Blackout-test-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            Check(Hotkey.Parse("alt + control + 1").ToString() == "Ctrl+Alt+1", "Hotkey normalization");
            foreach (string invalid in new[] { "R", "Shift+R", "Ctrl+Alt", "Ctrl+A+B", "Ctrl+999", "Ctrl+None" })
            {
                bool rejected = false;
                try { Hotkey.Parse(invalid); } catch (ArgumentException) { rejected = true; }
                Check(rejected, "Reject invalid binding: " + invalid);
            }
            var settings = new Settings();
            settings.Reconcile(Screen.AllScreens);
            settings.Reconcile(Screen.AllScreens);
            Check(settings.Monitors.Count == Screen.AllScreens.Length, "Monitor reconciliation is idempotent");
            settings.Save(temp);
            var loaded = Settings.Load(temp);
            Check(loaded.Monitors.Count == settings.Monitors.Count && loaded.RestoreHotkey == settings.RestoreHotkey, "Settings round trip");
            using var first = new HotkeyWindow();
            using var second = new HotkeyWindow();
            var key = Hotkey.Parse("Ctrl+Alt+Shift+F23");
            var other = Hotkey.Parse("Ctrl+Alt+Shift+F24");
            first.Replace([(key, () => { })]);
            second.Replace([(other, () => { })]);
            bool conflict = false;
            try { first.Replace([(other, () => { })]); } catch (InvalidOperationException) { conflict = true; }
            Check(conflict, "Windows reports occupied hotkey");
            bool rollback = false;
            try { second.Replace([(key, () => { })]); } catch (InvalidOperationException) { rollback = true; }
            Check(rollback, "Original hotkey retained after failed replacement");
            first.Replace([]);
            second.Replace([(key, () => { })]);
            Check(true, "Released hotkey can be registered again");
            using var overlay = new BlackoutWindow(new Rectangle(-1920, -100, 1920, 1080), () => { }, () => { });
            Check(overlay.Bounds == new Rectangle(-1920, -100, 1920, 1080) && overlay.BackColor == Color.Black && overlay.TopMost && !overlay.ShowInTaskbar, "Overlay geometry and behavior");
            File.WriteAllText(temp, "{broken");
            bool corrupt = false;
            try { Settings.Load(temp); } catch (System.Text.Json.JsonException) { corrupt = true; }
            Check(corrupt, "Corrupt settings are detected");
            File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "self-test-results.txt"), results);
            return 0;
        }
        catch (Exception ex) { results.Add("FAIL: " + ex); File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "self-test-results.txt"), results); return 1; }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        void Check(bool condition, string name) { if (!condition) throw new Exception(name); results.Add("PASS: " + name); }
    }
}
