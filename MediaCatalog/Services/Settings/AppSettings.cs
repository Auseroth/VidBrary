using MediaCatalog.Models;

namespace MediaCatalog.Services.Settings;

public class AppSettings
{
    public List<string> MovieDirectories { get; set; } = [];
    public List<string> TvShowDirectories { get; set; } = [];
    public List<string> AllowedExtensions { get; set; } = [".mp4", ".mkv", ".iso"];
    public string? TmdbApiKey { get; set; }
    public string? DefaultMediaPlayerPath { get; set; }   // null = use Windows default
    public bool ScanOnLaunch { get; set; } = true;
    public AppTheme Theme { get; set; } = AppTheme.FollowWindows;
    public ViewMode DefaultViewMode { get; set; } = ViewMode.ComfortableGrid;
    public int ActiveProfileId { get; set; } = 1;
    public List<string> VisibleColumns { get; set; } =
    [
        "Title", "Year", "Genre", "Rating", "Runtime", "MatchStatus", "Collection", "Tags"
    ];
}