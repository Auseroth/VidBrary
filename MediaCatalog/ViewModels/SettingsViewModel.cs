using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VidBrary.Data;
using VidBrary.Models;
using VidBrary.Services.Scanner;
using VidBrary.Services.Settings;
using VidBrary.Services.Theme;
using VidBrary.Services.Tmdb;
using VidBrary.ViewModels.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace VidBrary.ViewModels;

public partial class SettingsViewModel(
    ISettingsService settingsService,
    IScannerService scannerService,
    ILogger<SettingsViewModel> logger) : ViewModelBase
{
    // ── Directories ───────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<string> _movieDirectories = [];
    [ObservableProperty] private ObservableCollection<string> _tvShowDirectories = [];

    // ── File Extensions ───────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<string> _allowedExtensions = [];
    [ObservableProperty] private string _newExtension = string.Empty;

    // ── TMDB ──────────────────────────────────────────────────────────────────
    [ObservableProperty] private string _tmdbApiKey = string.Empty;
    [ObservableProperty] private bool _tmdbKeyVisible;

    // ── Playback ──────────────────────────────────────────────────────────────
    [ObservableProperty] private string _mediaPlayerPath = string.Empty;
    [ObservableProperty] private bool _useWindowsDefault = true;

    // ── Scan ──────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _scanOnLaunch;

    // ── Appearance ────────────────────────────────────────────────────────────
    [ObservableProperty] private AppTheme _selectedTheme;
    [ObservableProperty] private ViewMode _selectedViewMode;

    public IReadOnlyList<AppTheme> AvailableThemes { get; } =
        Enum.GetValues<AppTheme>().ToList();

    public IReadOnlyList<ViewMode> AvailableViewModes { get; } =
        Enum.GetValues<ViewMode>().ToList();

    // ── Custom colors ─────────────────────────────────────────────────────────
    [ObservableProperty] private string _backgroundColor = "#1a1a2e";
    [ObservableProperty] private string _surfaceColor    = "#16213e";
    [ObservableProperty] private string _accentColor     = "#D9652B";
    [ObservableProperty] private string _secondaryColor  = "#0f3460";

    // ── TV Season Ordering ────────────────────────────────────────────────────
    [ObservableProperty] private SeasonOrderMode _defaultSeasonOrderMode;

    public IReadOnlyList<SeasonOrderOption> AvailableSeasonOrderModes { get; } =
    [
        new(SeasonOrderMode.TmdbAuto,    "TMDB Auto"),
        new(SeasonOrderMode.ManualFolder,"Manual (folder / filename)")
    ];

    [ObservableProperty] private SeasonOrderOption _selectedSeasonOrderMode =
        new(SeasonOrderMode.TmdbAuto, "TMDB Auto");

    // ── Database ──────────────────────────────────────────────────────────────
    [ObservableProperty] private string _databasePath = string.Empty;

    // ── Update ────────────────────────────────────────────────────────────────
    [ObservableProperty] private string? _updateStatus;
    [ObservableProperty] private bool _isCheckingUpdate;

    // ── DB Operations ─────────────────────────────────────────────────────────
    [ObservableProperty] private string? _dbOperationStatus;
    [ObservableProperty] private bool _isDbBusy;

    // ── Confirmation ──────────────────────────────────────────────────────────
    [ObservableProperty] private string? _saveConfirmation;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override Task OnNavigatedToAsync(object? parameter = null)
    {
        var s = settingsService.Current;

        MovieDirectories  = new ObservableCollection<string>(s.MovieDirectories);
        TvShowDirectories = new ObservableCollection<string>(s.TvShowDirectories);
        AllowedExtensions = new ObservableCollection<string>(s.AllowedExtensions);

        TmdbApiKey        = s.TmdbApiKey ?? string.Empty;
        MediaPlayerPath   = s.DefaultMediaPlayerPath ?? string.Empty;
        UseWindowsDefault = string.IsNullOrWhiteSpace(s.DefaultMediaPlayerPath);
        ScanOnLaunch      = s.ScanOnLaunch;
        SelectedTheme     = s.Theme;
        SelectedViewMode  = s.DefaultViewMode;
        DatabasePath      = s.DatabasePath ?? string.Empty;

        BackgroundColor = s.BackgroundColor;
        SurfaceColor    = s.SurfaceColor;
        AccentColor     = s.AccentColor;
        SecondaryColor  = s.SecondaryColor;

        SelectedSeasonOrderMode = AvailableSeasonOrderModes
            .FirstOrDefault(o => o.Mode == s.DefaultSeasonOrderMode)
            ?? AvailableSeasonOrderModes[0];

        return Task.CompletedTask;
    }

    // ── Movie Directories ─────────────────────────────────────────────────────

    [RelayCommand]
    private void AddMovieDirectory()
    {
        var path = BrowseForFolder();
        if (path is not null && !MovieDirectories.Contains(path))
            MovieDirectories.Add(path);
    }

    [RelayCommand]
    private void RemoveMovieDirectory(string path) =>
        MovieDirectories.Remove(path);

    // ── TV Directories ────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddTvDirectory()
    {
        var path = BrowseForFolder();
        if (path is not null && !TvShowDirectories.Contains(path))
            TvShowDirectories.Add(path);
    }

    [RelayCommand]
    private void RemoveTvDirectory(string path) =>
        TvShowDirectories.Remove(path);

    // ── Extensions ────────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddExtension()
    {
        var ext = NewExtension.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext)) return;
        if (!ext.StartsWith('.')) ext = "." + ext;
        if (!AllowedExtensions.Contains(ext))
            AllowedExtensions.Add(ext);
        NewExtension = string.Empty;
    }

    [RelayCommand]
    private void RemoveExtension(string ext) =>
        AllowedExtensions.Remove(ext);

    // ── Media Player ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void BrowseMediaPlayer()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Media Player Executable",
            Filter = "Executables (*.exe)|*.exe",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() == true)
        {
            MediaPlayerPath = dialog.FileName;
            UseWindowsDefault = false;
        }
    }

    [RelayCommand]
    private void ClearMediaPlayer()
    {
        MediaPlayerPath = string.Empty;
        UseWindowsDefault = true;
    }

    // ── Database Path ─────────────────────────────────────────────────────────

    [RelayCommand]
    private void BrowseDatabasePath()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select or specify a catalog.db file",
            Filter = "SQLite Database (*.db)|*.db|All files (*.*)|*.*",
            CheckFileExists = false
        };
        if (dialog.ShowDialog() == true)
            DatabasePath = dialog.FileName;
    }

    [RelayCommand]
    private void ClearDatabasePath() => DatabasePath = string.Empty;

    // ── TMDB Key Visibility ───────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleTmdbKeyVisibility() =>
        TmdbKeyVisible = !TmdbKeyVisible;

    // ── Color resets ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void ResetColors()
    {
        var defaults = SelectedTheme switch
        {
            AppTheme.Light => ("#f5f7fa", "#ffffff", "#D9652B", "#1a73e8"),
            _              => ("#1a1a2e", "#16213e", "#D9652B", "#0f3460")
        };
        (BackgroundColor, SurfaceColor, AccentColor, SecondaryColor) = defaults;
    }

    // ── Check for Update (GitHub) ─────────────────────────────────────────────

    [RelayCommand]
    private async Task CheckForUpdateAsync()
    {
        if (IsCheckingUpdate) return;
        IsCheckingUpdate = true;
        UpdateStatus = "Checking for updates…";

        var current = Assembly.GetExecutingAssembly()
                              .GetName().Version?.ToString(3) ?? "0.0.0";

        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("VidBrary-UpdateCheck/1.0");

            var response = await http.GetAsync(
                "https://api.github.com/repos/Auseroth/VidBrary/releases/latest");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                UpdateStatus = $"✅ Cannot reach Repo for update ( you are on v{current})";
                return;
            }

            response.EnsureSuccessStatusCode();

            var release = await response.Content
                .ReadFromJsonAsync<GitHubRelease>();

            if (release is null)
            {
                UpdateStatus = "⚠ Could not read release info.";
                return;
            }

            var latestTag = release.TagName?.TrimStart('v') ?? string.Empty;

            if (Version.TryParse(latestTag, out var latest) &&
                Version.TryParse(current,   out var running) &&
                latest > running)
            {
                UpdateStatus = $"🆕 Update available: v{latestTag}  (you have v{current})  —  {release.HtmlUrl}";
            }
            else
            {
                UpdateStatus = $"✅ No new version available  (latest: v{latestTag}  |  you have v{current})";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update check failed");
            UpdateStatus = $"⚠ Update check failed: {ex.Message}";
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    // ── Version info (displayed in footer) ───────────────────────────────────

    public string AppVersion =>
        "v" + (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0");

    // ── Purge Stale Data ──────────────────────────────────────────────────────

    [RelayCommand]
    private async Task PurgeStaleDataAsync()
    {
        if (IsDbBusy) return;
        IsDbBusy = true;
        DbOperationStatus = "Removing stale entries…";

        try
        {
            var result = await scannerService.PurgeStaleDataAsync();
            DbOperationStatus =
                $"✅ Removed {result.MoviesUpdated} movie(s), " +
                $"{result.EpisodesAdded} episode(s), " +
                $"{result.ShowsAdded} show(s) with no remaining episodes.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PurgeStaleData failed. Inner: {Inner}", ex.InnerException?.ToString() ?? "none");
            DbOperationStatus = $"⚠ Purge failed: {ex.Message} | Inner: {ex.InnerException?.Message ?? "none"}";
        }
        finally
        {
            IsDbBusy = false;
            await Task.Delay(6000);
            DbOperationStatus = null;
        }
    }

    // ── Delete Database File ──────────────────────────────────────────────────

    [RelayCommand]
    private async Task DeleteDatabaseAsync()
    {
        var dbPath = string.IsNullOrWhiteSpace(settingsService.Current.DatabasePath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "VidBrary", "catalog.db")
            : settingsService.Current.DatabasePath;

        if (IsDbBusy) return;

        var confirm = System.Windows.MessageBox.Show(
            $"This will permanently delete the database file:\n\n{dbPath}\n\n" +
            "The app will need to be restarted. Are you sure?",
            "Delete Database",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsDbBusy = true;
        DbOperationStatus = "Deleting database…";

        try
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            if (File.Exists(dbPath))
                File.Delete(dbPath);

            DbOperationStatus = "✅ Database deleted. Please restart VidBrary.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DeleteDatabase failed. Inner: {Inner}", ex.InnerException?.ToString() ?? "none");
            DbOperationStatus = $"⚠ Delete failed: {ex.Message} | Inner: {ex.InnerException?.Message ?? "none"}";
            IsDbBusy = false;
        }
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveAsync()
    {
        var s = settingsService.Current;

        var previousKey = s.TmdbApiKey;

        s.MovieDirectories       = [.. MovieDirectories];
        s.TvShowDirectories      = [.. TvShowDirectories];
        s.AllowedExtensions      = [.. AllowedExtensions];
        s.TmdbApiKey             = string.IsNullOrWhiteSpace(TmdbApiKey) ? null : TmdbApiKey.Trim();
        s.DefaultMediaPlayerPath = UseWindowsDefault ? null : MediaPlayerPath;
        s.ScanOnLaunch           = ScanOnLaunch;
        s.Theme                  = SelectedTheme;
        s.DefaultViewMode        = SelectedViewMode;
        s.DefaultSeasonOrderMode = SelectedSeasonOrderMode?.Mode ?? SeasonOrderMode.TmdbAuto;
        s.DatabasePath           = string.IsNullOrWhiteSpace(DatabasePath) ? null : DatabasePath.Trim();

        s.BackgroundColor = BackgroundColor;
        s.SurfaceColor    = SurfaceColor;
        s.AccentColor     = AccentColor;
        s.SecondaryColor  = SecondaryColor;

        await settingsService.SaveAsync();

        ThemeService.Apply(s);

        // If a TMDB key was just added for the first time, kick off enrichment
        // in the background using a dedicated scope so it never touches the UI DbContext
        var keyAdded = string.IsNullOrWhiteSpace(previousKey)
                    && !string.IsNullOrWhiteSpace(s.TmdbApiKey);
        if (keyAdded)
        {
            SaveConfirmation = "Settings saved ✓  —  Starting TMDB enrichment…";
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = App.Services.CreateScope();
                    var tmdb = scope.ServiceProvider.GetRequiredService<ITmdbService>();
                    await tmdb.EnrichAllMoviesAsync();
                    await tmdb.EnrichAllShowsAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Background TMDB enrichment after key save failed");
                }
            });
        }
        else
        {
            SaveConfirmation = "Settings saved ✓";
        }

        await Task.Delay(3000);
        SaveConfirmation = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? BrowseForFolder()
    {
        var dialog = new OpenFolderDialog { Title = "Select Folder" };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    // ── GitHub release DTO ────────────────────────────────────────────────────

    private sealed record GitHubRelease(
        [property: System.Text.Json.Serialization.JsonPropertyName("tag_name")]  string? TagName,
        [property: System.Text.Json.Serialization.JsonPropertyName("html_url")]  string? HtmlUrl);
}