using System.Runtime.InteropServices;

namespace Blackout;

internal static class Native
{
    public static readonly uint ShowSettingsMessage = RegisterWindowMessage("Blackout.ShowSettings.v1");
    [DllImport("user32.dll", SetLastError = true)] public static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr window, int id);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string text);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}

internal readonly record struct Hotkey(uint Modifiers, Keys Key)
{
    public static Hotkey Parse(string text)
    {
        uint modifiers = 0;
        Keys key = Keys.None;
        foreach (string token in text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (token.ToUpperInvariant())
            {
                case "CTRL": case "CONTROL": modifiers |= 2; continue;
                case "ALT": modifiers |= 1; continue;
                case "SHIFT": modifiers |= 4; continue;
                case "WIN": modifiers |= 8; continue;
            }
            Keys parsed;
            if (token.Length == 1 && char.IsAsciiDigit(token[0])) parsed = Keys.D0 + (token[0] - '0');
            else if (!Enum.TryParse(token, true, out parsed)) throw new ArgumentException($"Unknown key: {token}");
            if (key != Keys.None || !IsAllowed(parsed)) throw new ArgumentException("Choose a letter, number, F1–F24, or navigation key.");
            key = parsed;
        }
        if (key == Keys.None || (modifiers & (1 | 2 | 8)) == 0) throw new ArgumentException("Hotkeys need Ctrl, Alt, or Win plus a key.");
        return new(modifiers, key);
    }

    private static bool IsAllowed(Keys key) => key is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9 or >= Keys.F1 and <= Keys.F24 or >= Keys.NumPad0 and <= Keys.NumPad9 or Keys.Space or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown or Keys.Insert or Keys.Delete or Keys.Up or Keys.Down or Keys.Left or Keys.Right;
    public override string ToString()
    {
        var parts = new List<string>();
        if ((Modifiers & 2) != 0) parts.Add("Ctrl");
        if ((Modifiers & 1) != 0) parts.Add("Alt");
        if ((Modifiers & 4) != 0) parts.Add("Shift");
        if ((Modifiers & 8) != 0) parts.Add("Win");
        parts.Add(Key is >= Keys.D0 and <= Keys.D9 ? ((int)Key - (int)Keys.D0).ToString() : Key.ToString());
        return string.Join('+', parts);
    }
}

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private readonly Dictionary<int, Action> actions = [];
    private List<(Hotkey Key, Action Action)> current = [];
    public event Action? ShowRequested;
    public HotkeyWindow() => CreateHandle(new CreateParams { Caption = "Blackout hotkeys", Style = 0 });

    public void Replace(List<(Hotkey Key, Action Action)> bindings)
    {
        if (bindings.Select(b => b.Key).Distinct().Count() != bindings.Count) throw new ArgumentException("Each action needs a different hotkey.");
        var previous = current;
        Clear();
        try { Register(bindings); current = bindings; }
        catch (Exception original)
        {
            Clear();
            try { Register(previous); current = previous; }
            catch { Clear(); current = []; throw new InvalidOperationException("Hotkeys could not be restored. Use the tray menu and save different hotkeys.", original); }
            throw;
        }
    }
    private void Register(List<(Hotkey Key, Action Action)> bindings)
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            var binding = bindings[i];
            if (!Native.RegisterHotKey(Handle, i + 1, binding.Key.Modifiers | 0x4000, (uint)binding.Key.Key))
                throw new InvalidOperationException($"{binding.Key} is unavailable, possibly used by another app. Choose a different hotkey.");
            actions.Add(i + 1, binding.Action);
        }
    }
    private void Clear() { foreach (int id in actions.Keys) Native.UnregisterHotKey(Handle, id); actions.Clear(); }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0312 && actions.TryGetValue(m.WParam.ToInt32(), out var action)) action();
        else if ((uint)m.Msg == Native.ShowSettingsMessage) ShowRequested?.Invoke();
        base.WndProc(ref m);
    }
    public void Dispose() { Clear(); DestroyHandle(); }
}
