namespace MediaCatalog.Models;

public class Season
{
    public int Id { get; set; }
    public int TvShowId { get; set; }
    public TvShow TvShow { get; set; } = null!;

    public int SeasonNumber { get; set; }
    public required string DirectoryPath { get; set; }

    // --- TMDB Metadata ---
    public string? Name { get; set; }
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public int? AirYear { get; set; }

    public ICollection<Episode> Episodes { get; set; } = [];
}