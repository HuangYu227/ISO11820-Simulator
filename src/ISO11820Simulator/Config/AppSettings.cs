namespace ISO11820Simulator.Config;

public sealed class AppSettings
{
    public DatabaseSettings Database { get; set; } = new();
    public HardwareSettings Hardware { get; set; } = new();
    public SimulationSettings Simulation { get; set; } = new();
    public ExperimentSettings Experiment { get; set; } = new();
    public FileStorageSettings FileStorage { get; set; } = new();
    public ReportSettings Report { get; set; } = new();
    public UiSettings Ui { get; set; } = new();
}

public sealed class DatabaseSettings
{
    public string Provider { get; set; } = "Sqlite";
    public string SqlitePath { get; set; } = "Data\\ISO11820.db";
}

public sealed class HardwareSettings
{
    public int ConstPower { get; set; } = 2048;
    public double PidTemperature { get; set; } = 750;
    public string SensorProtocol { get; set; } = "Simulation";
}

public sealed class SimulationSettings
{
    public bool EnableSimulation { get; set; } = true;
    public bool SimulateSensors { get; set; } = true;
    public bool SimulatePidController { get; set; } = true;
    public double InitialFurnaceTemp { get; set; } = 720;
    public double InitialAmbientTemp { get; set; } = 25;
    public double TargetFurnaceTemp { get; set; } = 750;
    public double HeatingRatePerSecond { get; set; } = 40;
    public double TempFluctuation { get; set; } = 0.5;
    public double StableThreshold { get; set; } = 3;
    public int StableTickCount { get; set; } = 4;
    public bool SimulateFlame { get; set; }
    public double MaxTemperatureDriftPerTenMinutes { get; set; } = 2;
}

public sealed class ExperimentSettings
{
    public int StandardDurationSeconds { get; set; } = 3600;
    public int DemoDurationSeconds { get; set; } = 90;
    public bool UseDemoDurationByDefault { get; set; } = true;
    public int CsvSamplingSeconds { get; set; } = 1;
    public int ChartWindowSeconds { get; set; } = 600;
}

public sealed class FileStorageSettings
{
    public string BaseDirectory { get; set; } = ".";
    public string TestDataDirectory { get; set; } = "TestData";
}

public sealed class ReportSettings
{
    public string OutputDirectory { get; set; } = "Reports";
    public bool EnablePdfExport { get; set; } = true;
    public bool EnableExcelExport { get; set; } = true;
}

public sealed class UiSettings
{
    public string Theme { get; set; } = "ModernDark";
}
