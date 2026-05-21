using System.IO;
using System.Text.Json;

namespace MediaCatalog.Services.Settings;

public class SettingsService : ISettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "MediaCatalog", "settings.json");

    public AppSettings Current { get; private set; } = new();

    public async Task LoadAsync()
    {
        if (!File.Exists(SettingsPath))
        {
            Current = new AppSettings();
            return;
        }

        await using var stream = File.OpenRead(SettingsPath);
        Current = await JsonSerializer.DeserializeAsync<AppSettings>(stream) ?? new AppSettings();
    }

    public async Task SaveAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        await using var stream = File.Create(SettingsPath);
        await JsonSerializer.SerializeAsync(stream, Current, new JsonSerializerOptions { WriteIndented = true });
    }
}