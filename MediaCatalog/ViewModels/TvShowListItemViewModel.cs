using CommunityToolkit.Mvvm.ComponentModel;
using VidBrary.Models;

namespace VidBrary.ViewModels;

public partial class TvShowListItemViewModel : ObservableObject
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public int? FirstAirYear { get; init; }
    public int? LastAirYear { get; init; }
    public double? TmdbRating { get; init; }
    public string? PosterPath { get; init; }
    public string Genres { get; init; } = string.Empty;
    public string? Status { get; init; }
    public int? TotalSeasons { get; init; }
    public int? TotalEpisodes { get; init; }
    public MatchStatus MatchStatus { get; init; }
    public int TmdbCandidateCount { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];

    public string YearDisplay => (FirstAirYear, LastAirYear) switch
    {
        (int f, int l) when f == l => $"{f}",
        (int f, int l)             => $"{f}–{l}",
        (int f, null)              => $"{f}–",
        _                          => "—"
    };

    public string RatingDisplay  => TmdbRating.HasValue ? $"★ {TmdbRating:F1}" : "—";
    public string SeasonsDisplay => TotalSeasons.HasValue ? $"{TotalSeasons} Season{(TotalSeasons > 1 ? "s" : "")}" : "—";

    public string MatchBadgeText => MatchStatus switch
    {
        MatchStatus.NoResults         => "No Match Found",
        MatchStatus.AutoMatched       => "Matched",
        MatchStatus.ManualMatched     => $"Matched ({TmdbCandidateCount})",
        MatchStatus.PendingSelection  => $"{TmdbCandidateCount} Matches — Action Needed",
        MatchStatus.ManuallyUnmatched => $"{TmdbCandidateCount} Available — Unmatched",
        _                             => string.Empty
    };

    public string MatchBadgeColor => MatchStatus switch
    {
        MatchStatus.NoResults         => "#6c757d",
        MatchStatus.AutoMatched       => "#1a73e8",
        MatchStatus.ManualMatched     => "#1a73e8",
        MatchStatus.PendingSelection  => "#e94560",  // red
        MatchStatus.ManuallyUnmatched => "#D9652B",  // orange
        _                             => "#6c757d"
    };

    public static TvShowListItemViewModel FromShow(TvShow show) => new()
    {
        Id                 = show.Id,
        Title              = show.Title ?? show.FolderName,
        FirstAirYear       = show.FirstAirYear,
        LastAirYear        = show.LastAirYear,
        TmdbRating         = show.TmdbRating,
        PosterPath         = show.PosterPath,
        Genres             = string.Join(", ", show.Genres.Select(g => g.Genre.Name)),
        Status             = show.Status,
        TotalSeasons       = show.TotalSeasons,
        TotalEpisodes      = show.TotalEpisodes,
        MatchStatus        = show.MatchStatus,
        TmdbCandidateCount = show.TmdbCandidateCount,
        Tags               = show.Tags.Select(t => t.UserTag.Name).ToList()
    };
}