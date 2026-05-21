using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaCatalog.Models;
using MediaCatalog.Services.Settings;
using MediaCatalog.ViewModels.Base;
using Microsoft.Win32;

namespace MediaCatalog.ViewModels;

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

        MovieDirectories = new ObservableCollection<string>(s.MovieDirectories);
        TvShowDirectories = new ObservableCollection<string>(s.TvShowDirectories);
        AllowedExtensions = new ObservableCollection<string>(s.AllowedExtensions);

        TmdbApiKey = s.TmdbApiKey ?? string.Empty;
        MediaPlayerPath = s.DefaultMediaPlayerPath ?? string.Empty;
        UseWindowsDefault = string.IsNullOrWhiteSpace(s.DefaultMediaPlayerPath);
        ScanOnLaunch = s.ScanOnLaunch;
        SelectedTheme = s.Theme;
        SelectedViewMode = s.DefaultViewMode;

        // Season order — map the stored enum to the option object
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

    // ── Save ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveAsync()
    {
        var s = settingsService.Current;

        s.MovieDirectories = [.. MovieDirectories];
        s.TvShowDirectories = [.. TvShowDirectories];
        s.AllowedExtensions = [.. AllowedExtensions];
        s.TmdbApiKey = string.IsNullOrWhiteSpace(TmdbApiKey) ? null : TmdbApiKey.Trim();
        s.DefaultMediaPlayerPath = UseWindowsDefault ? null : MediaPlayerPath;
        s.ScanOnLaunch = ScanOnLaunch;
        s.Theme = SelectedTheme;
        s.DefaultViewMode = SelectedViewMode;
        s.DefaultSeasonOrderMode = SelectedSeasonOrderMode?.Mode ?? SeasonOrderMode.TmdbAuto;

        await settingsService.SaveAsync();

        SaveConfirmation = "Settings saved ✓";
        await Task.Delay(3000);
        SaveConfirmation = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? BrowseForFolder()
    {
        // WPF has no built-in folder picker — use OpenFileDialog pointed at a folder
        var dialog = new OpenFolderDialog { Title = "Select Folder" };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}