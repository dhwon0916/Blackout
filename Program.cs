namespace Blackout;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Contains("--self-test"))
        {
            Environment.ExitCode = SelfTest.Run();
            return;
        }
        using var singleInstance = new Mutex(true, @"Local\Blackout.Tray.v1", out bool first);
        if (!first)
        {
            Native.PostMessage(new IntPtr(0xffff), Native.ShowSettingsMessage, IntPtr.Zero, IntPtr.Zero);
            return;
        }
        try
        {
            using var context = new TrayContext(!args.Contains("--startup"));
            Application.Run(context);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Blackout could not start", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
