namespace Blackout;

internal static class AppIcon
{
    public static Icon Value { get; } = Load();

    private static Icon Load()
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream("Blackout.AppIcon.ico")
            ?? throw new InvalidOperationException("Application icon is missing.");
        using var icon = new Icon(stream, new Size(32, 32));
        return (Icon)icon.Clone();
    }
}
