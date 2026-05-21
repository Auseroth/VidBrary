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
    public string DirectoryPath { get; init; } = string.Empty;
    public ObservableCollection<EpisodeViewModel> Episodes { get; init; } = [];

    [ObservableProperty] private bool _isExpanded;

    public string EpisodeCountDisplay =>
        $"{Episodes.Count} episode{(Episodes.Count != 1 ? "s" : "")}";

    public static SeasonViewModel FromSeason(Season season, SeasonOrderMode orderMode = SeasonOrderMode.TmdbAuto)
    {
        // Episode ordering: TmdbAuto uses EpisodeNumber (SxxExx); ManualFolder also uses
        // EpisodeNumber which now comes from leading-number filenames too.
        // In both cases EpisodeNumber is the right sort key — ManualFolder just ensures
        // the season itself is sorted by folder name at the parent level.
        var orderedEpisodes = season.Episodes
            .OrderBy(e => e.EpisodeNumber)
            .ThenBy(e => e.FileName)
            .Select(EpisodeViewModel.FromEpisode);

        return new SeasonViewModel
        {
            Id            = season.Id,
            SeasonNumber  = season.SeasonNumber,
            Name          = season.Name ?? $"Season {season.SeasonNumber}",
            Overview      = season.Overview,
            PosterPath    = season.PosterPath,
            AirYear       = season.AirYear,
            DirectoryPath = season.DirectoryPath,
            Episodes      = new ObservableCollection<EpisodeViewModel>(orderedEpisodes)
        };
    }
}