using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VidBrary.Models;
using VidBrary.Services.Settings;
using VidBrary.Services.Theme;
using VidBrary.ViewModels.Base;
using Microsoft.Win32;

namespace VidBrary.ViewModels;

public partial class SettingsViewModel(ISettingsService settingsService) : ViewModelBase
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
    [ObservableProperty] private string _accentColor     = "#e94560";
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

    // ── Confirmation ──────────────────────────────────────────────────────────
    [ObservableProperty] private string? _saveConfirmation;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override Task OnNavigatedToAsync(object? parameter = null)
    {
        var s = settingsService.Current;

        MovieDirectories  = new ObservableCollection<string>(s.MovieDirectories);
        TvShowDirectories = new ObservableCollection<string>(s.TvShowDirectories);
        AllowedExtensions = new ObservableCollection<string>(s.AllowedExtensions);

        TmdbApiKey       = s.TmdbApiKey ?? string.Empty;
        MediaPlayerPath  = s.DefaultMediaPlayerPath ?? string.Empty;
        UseWindowsDefault = string.IsNullOrWhiteSpace(s.DefaultMediaPlayerPath);
        ScanOnLaunch     = s.ScanOnLaunch;
        SelectedTheme    = s.Theme;
        SelectedViewMode = s.DefaultViewMode;

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

    // ── TMDB Key Visibility ───────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleTmdbKeyVisibility() =>
        TmdbKeyVisible = !TmdbKeyVisible;

    // ── Color resets ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void ResetColors()
    {
        var s = settingsService.Current;
        // Reset to theme defaults by clearing overrides
        var defaults = SelectedTheme switch
        {
            AppTheme.Light => ("#f5f7fa", "#ffffff", "#e94560", "#1a73e8"),
            _              => ("#1a1a2e", "#16213e", "#e94560", "#0f3460")
        };
        (BackgroundColor, SurfaceColor, AccentColor, SecondaryColor) = defaults;
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveAsync()
    {
        var s = settingsService.Current;

        s.MovieDirectories  = [.. MovieDirectories];
        s.TvShowDirectories = [.. TvShowDirectories];
        s.AllowedExtensions = [.. AllowedExtensions];
        s.TmdbApiKey        = string.IsNullOrWhiteSpace(TmdbApiKey) ? null : TmdbApiKey.Trim();
        s.DefaultMediaPlayerPath = UseWindowsDefault ? null : MediaPlayerPath;
        s.ScanOnLaunch      = ScanOnLaunch;
        s.Theme             = SelectedTheme;
        s.DefaultViewMode   = SelectedViewMode;
        s.DefaultSeasonOrderMode = SelectedSeasonOrderMode?.Mode ?? SeasonOrderMode.TmdbAuto;

        s.BackgroundColor = BackgroundColor;
        s.SurfaceColor    = SurfaceColor;
        s.AccentColor     = AccentColor;
        s.SecondaryColor  = SecondaryColor;

        await settingsService.SaveAsync();

        // Apply the new theme immediately — no restart needed
        ThemeService.Apply(s);

        SaveConfirmation = "Settings saved ✓";
        await Task.Delay(3000);
        SaveConfirmation = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? BrowseForFolder()
    {
        var dialog = new OpenFolderDialog { Title = "Select Folder" };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}