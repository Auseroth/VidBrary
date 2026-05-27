using CommunityToolkit.Mvvm.ComponentModel;
using VidBrary.Models;

namespace VidBrary.ViewModels;

/// <summary>Lightweight wrapper around Movie for list display.</summary>
public partial class MovieListItemViewModel : ObservableObject
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public int? Year { get; init; }
    public int? RuntimeMinutes { get; init; }
    public double? TmdbRating { get; init; }
    public string? PosterPath { get; init; }
    public string? Collection { get; init; }
    public string Genres { get; init; } = string.Empty;
    public MatchStatus MatchStatus { get; init; }
    public int TmdbCandidateCount { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];

    // ── Derived display values ────────────────────────────────────────────────

    public string RuntimeDisplay => RuntimeMinutes.HasValue
        ? $"{RuntimeMinutes / 60}h {RuntimeMinutes % 60}m" : "—";

    public string RatingDisplay => TmdbRating.HasValue
        ? $"★ {TmdbRating:F1}" : "—";

    public string YearDisplay => Year?.ToString() ?? "—";

    /// Human-readable match badge text shown in the column
    public string MatchBadgeText => MatchStatus switch
    {
        MatchStatus.NoResults        => "No Match Found",
        MatchStatus.AutoMatched      => "Matched",
        MatchStatus.ManualMatched    => $"Matched ({TmdbCandidateCount})",
        MatchStatus.PendingSelection => $"{TmdbCandidateCount} Matches — Action Needed",
        MatchStatus.ManuallyUnmatched => $"{TmdbCandidateCount} Available — Unmatched",
        _                            => string.Empty
    };

    /// Hex color for the match badge
    public string MatchBadgeColor => MatchStatus switch
    {
        MatchStatus.NoResults         => "#6c757d",
        MatchStatus.AutoMatched       => "#1a73e8",
        MatchStatus.ManualMatched     => "#1a73e8",
        MatchStatus.PendingSelection  => "#e94560",  // red
        MatchStatus.ManuallyUnmatched => "#D9652B",  // orange
        _                             => "#6c757d"
    };

    public static MovieListItemViewModel FromMovie(Movie movie) => new()
    {
        Id = movie.Id,
        Title = movie.Title ?? movie.FileName,
        Year = movie.Year,
        RuntimeMinutes = movie.RuntimeMinutes,
        TmdbRating = movie.TmdbRating,
        PosterPath = movie.PosterPath,
        Collection = movie.Collection?.Name,
        Genres = string.Join(", ", movie.Genres.Select(g => g.Genre.Name)),
        MatchStatus = movie.MatchStatus,
        TmdbCandidateCount = movie.TmdbCandidateCount,
        Tags = movie.Tags.Select(t => t.UserTag.Name).ToList()
    };
}