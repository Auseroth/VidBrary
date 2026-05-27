namespace VidBrary.Models;

public class MediaCollection
{
    public int Id { get; set; }
    public required string Name { get; set; }

    /// <summary>Null if user-created; set if sourced from TMDB.</summary>
    public int? TmdbCollectionId { get; set; }
    public bool IsUserCreated { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public string? Overview { get; set; }

    public ICollection<Movie> Movies { get; set; } = [];
}