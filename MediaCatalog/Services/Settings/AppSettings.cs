using VidBrary.Models;

namespace VidBrary.Services.Settings;

public class AppSettings
{
    public List<string> MovieDirectories { get; set; } = [];
    public List<string> TvShowDirectories { get; set; } = [];
    public List<string> AllowedExtensions { get; set; } = [".mp4", ".mkv", ".iso"];
    public string? TmdbApiKey { get; set; }
    public string? DefaultMediaPlayerPath { get; set; }
    public bool ScanOnLaunch { get; set; } = true;
    public AppTheme Theme { get; set; } = AppTheme.Dark;
    public ViewMode DefaultViewMode { get; set; } = ViewMode.ComfortableGrid;
    public int ActiveProfileId { get; set; } = 1;
    public List<string> VisibleColumns { get; set; } =
    [
        "Title", "Year", "Genre", "Rating", "Runtime", "MatchStatus", "Collection", "Tags"
    ];
    public SeasonOrderMode DefaultSeasonOrderMode { get; set; } = SeasonOrderMode.TmdbAuto;

    // ── Custom colours ────────────────────────────────────────────────────────
    /// <summary>Main page/window background. Default: #1a1a2e</summary>
    public string BackgroundColor { get; set; } = "#1a1a2e";
    /// <summary>Sidebar and card surface. Default: #16213e</summary>
    public string SurfaceColor { get; set; } = "#16213e";
    /// <summary>Primary accent (buttons, highlights). Default: #e94560</summary>
    public string AccentColor { get; set; } = "#e94560";
    /// <summary>Secondary accent (borders, hover). Default: #0f3460</summary>
    public string SecondaryColor { get; set; } = "#0f3460";
}