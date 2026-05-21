using MediaCatalog.Models;

namespace MediaCatalog.ViewModels;

public class CollectionListItemViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? PosterPath { get; init; }
    public string? Overview { get; init; }
    public bool IsUserCreated { get; init; }
    public int MovieCount { get; init; }

    public string MovieCountDisplay =>
        $"{MovieCount} movie{(MovieCount != 1 ? "s" : "")}";

    public string SourceLabel => IsUserCreated ? "Custom" : "TMDB";
    public string SourceColor => IsUserCreated ? "#7c3aed" : "#0f3460";

    public static CollectionListItemViewModel FromCollection(MediaCollection c) => new()
    {
        Id            = c.Id,
        Name          = c.Name,
        PosterPath    = c.PosterPath,
        Overview      = c.Overview,
        IsUserCreated = c.IsUserCreated,
        MovieCount    = c.Movies.Count
    };
}