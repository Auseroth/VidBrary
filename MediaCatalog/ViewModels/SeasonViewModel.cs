using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MediaCatalog.Models;

namespace MediaCatalog.ViewModels;

public partial class SeasonViewModel : ObservableObject
{
    public int Id { get; init; }
    public int SeasonNumber { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Overview { get; init; }
    public string? PosterPath { get; init; }
    public int? AirYear { get; init; }
    public ObservableCollection<EpisodeViewModel> Episodes { get; init; } = [];

    [ObservableProperty] private bool _isExpanded;

    public string EpisodeCountDisplay =>
        $"{Episodes.Count} episode{(Episodes.Count != 1 ? "s" : "")}";

    public static SeasonViewModel FromSeason(Season season) => new()
    {
        Id           = season.Id,
        SeasonNumber = season.SeasonNumber,
        Name         = season.Name ?? $"Season {season.SeasonNumber}",
        Overview     = season.Overview,
        PosterPath   = season.PosterPath,
        AirYear      = season.AirYear,
        Episodes     = new ObservableCollection<EpisodeViewModel>(
            season.Episodes.OrderBy(e => e.EpisodeNumber)
                           .Select(EpisodeViewModel.FromEpisode))
    };
}