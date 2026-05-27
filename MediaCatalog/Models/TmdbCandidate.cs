namespace VidBrary.Models;

/// <summary>
/// Stores all raw TMDB search results for a media item
/// so the user can review and pick the correct match later.
/// </summary>
public class TmdbCandidate
{
    public int Id { get; set; }
    public int TmdbId { get; set; }
    public MediaType MediaType { get; set; }
    public required string Title { get; set; }
    public int? Year { get; set; }
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public double? TmdbRating { get; set; }

    public int? MovieId { get; set; }
    public Movie? Movie { get; set; }

    public int? TvShowId { get; set; }
    public TvShow? TvShow { get; set; }
}