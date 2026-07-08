using System.Text.Json;
using RecallIQ.Core.Interfaces;
using RecallIQ.Core.Models;

namespace RecallIQ.UI.Services;

public sealed class SettingsService : ISettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RecallIQ", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppSettings CurrentSettings { get; private set; } = new();
    public event EventHandler<AppSettings>? SettingsChanged;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = await File.ReadAllTextAsync(SettingsPath, cancellationToken);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded != null)
                    CurrentSettings = loaded;
            }
        }
        catch
        {
            CurrentSettings = new AppSettings();
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(CurrentSettings, JsonOptions);
        await File.WriteAllTextAsync(SettingsPath, json, cancellationToken);
        SettingsChanged?.Invoke(this, CurrentSettings);
    }
}
