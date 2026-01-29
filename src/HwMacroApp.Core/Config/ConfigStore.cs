using System.Text.Json;

namespace HwMacroApp.Core.Config;

/// <summary>JSON 設定ファイルの読み書き。</summary>
public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _filePath;

    public ConfigStore(string? filePath = null)
    {
        _filePath = filePath ?? GetDefaultPath();
    }

    public string FilePath => _filePath;

    public AppConfig Load()
    {
        if (!File.Exists(_filePath))
            return new AppConfig();

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
    }

    public void Save(AppConfig config)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private static string GetDefaultPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "HwMacroApp", "config.json");
    }
}
