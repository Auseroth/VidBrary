using CommunityToolkit.Mvvm.ComponentModel;
using VidBrary.Models;

namespace VidBrary.ViewModels;

public partial class EpisodeViewModel : ObservableObject
{
    public int Id { get; init; }
    public int EpisodeNumber { get; init; }
    public string? Title { get; init; }
    public string? Overview { get; init; }
    public DateOnly? AirDate { get; init; }
    public int? RuntimeMinutes { get; init; }
    public double? TmdbRating { get; init; }
    public string? StillPath { get; init; }
    public string FilePath { get; init; } = string.Empty;

    [ObservableProperty] private bool _isExpanded;

    public string EpisodeLabel => $"E{EpisodeNumber:D2}";
    public string TitleDisplay => string.IsNullOrWhiteSpace(Title) ? $"Episode {EpisodeNumber}" : Title;
    public string AirDateDisplay => AirDate?.ToString("MMM d, yyyy") ?? "—";
    public string RuntimeDisplay => RuntimeMinutes.HasValue
        ? $"{RuntimeMinutes}m" : "—";
    public string RatingDisplay => TmdbRating.HasValue ? $"★ {TmdbRating:F1}" : "—";

    public static EpisodeViewModel FromEpisode(Episode ep) => new()
    {
        Id            = ep.Id,
        EpisodeNumber = ep.EpisodeNumber,
        Title         = ep.Title,
        Overview      = ep.Overview,
        AirDate       = ep.AirDate,
        RuntimeMinutes = ep.RuntimeMinutes,
        TmdbRating    = ep.TmdbRating,
        StillPath     = ep.StillPath,
        FilePath      = ep.FilePath
    };
}