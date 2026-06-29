using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ISO11820Simulator.Config;

public static class AppConfig
{
    public static AppSettings Load()
    {
        var baseDir = AppContext.BaseDirectory;
        var cfg = new ConfigurationBuilder()
            .SetBasePath(baseDir)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        return cfg.Get<AppSettings>() ?? new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        var file = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(file, json);
    }
}

public static class PathResolver
{
    public static string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return AppContext.BaseDirectory;
        return Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
    }
}
