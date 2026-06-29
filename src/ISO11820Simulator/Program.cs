using ISO11820Simulator.App;
using ISO11820Simulator.Config;
using ISO11820Simulator.Data;
using ISO11820Simulator.UI;
using Serilog;

namespace ISO11820Simulator;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        var settings = AppConfig.Load();
        Directory.CreateDirectory(PathResolver.Resolve(settings.FileStorage.BaseDirectory));
        Directory.CreateDirectory(PathResolver.Resolve(settings.FileStorage.TestDataDirectory));
        Directory.CreateDirectory(PathResolver.Resolve(settings.Report.OutputDirectory));
        Directory.CreateDirectory(PathResolver.Resolve("Logs"));

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(PathResolver.Resolve("Logs\\iso11820-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
            .CreateLogger();

        try
        {
            using var db = new DbHelper(settings.Database.SqlitePath);
            db.InitializeDatabase();

            using var login = new LoginForm(db);
            if (login.ShowDialog() != DialogResult.OK || login.Session is null)
            {
                return;
            }

            GlobalApp.Initialize(settings, db, login.Session);
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "程序启动失败");
            MessageBox.Show($"程序启动失败：{ex.Message}", "ISO11820", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            GlobalApp.Dispose();
            Log.CloseAndFlush();
        }
    }
}
