using CommunityToolkit.Mvvm.ComponentModel;
using MediaCatalog.Models;

namespace MediaCatalog.ViewModels;

public partial class TmdbCandidateViewModel : ObservableObject
{
    public int Id { get; init; }
    public int TmdbId { get; init; }
    public string Title { get; init; } = string.Empty;
    public int? Year { get; init; }
    public string? Overview { get; init; }
    public string? PosterPath { get; init; }
    public double? TmdbRating { get; init; }

    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private bool _isSelected;

    public string YearDisplay => Year?.ToString() ?? "Unknown";
    public string RatingDisplay => TmdbRating.HasValue ? $"★ {TmdbRating:F1}" : "No rating";

    public static TmdbCandidateViewModel FromCandidate(TmdbCandidate c) => new()
    {
        Id = c.Id,
        TmdbId = c.TmdbId,
        Title = c.Title,
        Year = c.Year,
        Overview = c.Overview,
        PosterPath = c.PosterPath,
        TmdbRating = c.TmdbRating
    };
}