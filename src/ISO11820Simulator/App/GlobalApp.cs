using ISO11820Simulator.Config;
using ISO11820Simulator.Data;
using ISO11820Simulator.Models;
using ISO11820Simulator.Services;

namespace ISO11820Simulator.App;

public static class GlobalApp
{
    public static AppSettings Settings { get; private set; } = new();
    public static DbHelper Db { get; private set; } = null!;
    public static UserSession Session { get; private set; } = null!;
    public static TestController Controller { get; private set; } = null!;
    public static ExportService Exporter { get; private set; } = null!;

    public static void Initialize(AppSettings settings, DbHelper db, UserSession session)
    {
        Settings = settings;
        Db = db;
        Session = session;
        Exporter = new ExportService(settings);
        Controller = new TestController(settings, db, Exporter, session);
    }

    public static void Dispose()
    {
        Controller?.Dispose();
    }
}
