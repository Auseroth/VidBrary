namespace MediaCatalog.Models;

public class TvShow
{
    public int Id { get; set; }

    // --- Directory Info ---
    public required string DirectoryPath { get; set; }
    public required string FolderName { get; set; }
    public DateTime LastScanned { get; set; }

    // --- TMDB Match ---
    public int? TmdbId { get; set; }
    public MatchStatus MatchStatus { get; set; } = MatchStatus.NoResults;
    public int TmdbCandidateCount { get; set; }

    // --- TMDB Metadata ---
    public string? Title { get; set; }
    public string? OriginalTitle { get; set; }
    public int? FirstAirYear { get; set; }
    public int? LastAirYear { get; set; }
    public string? Overview { get; set; }
    public double? TmdbRating { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public string? Status { get; set; }                // Ended, Returning, etc.
    public string? OriginalLanguage { get; set; }
    public int? TotalEpisodes { get; set; }
    public int? TotalSeasons { get; set; }

    // --- Relations ---
    public ICollection<Season> Seasons { get; set; } = [];
    public ICollection<TvShowGenre> Genres { get; set; } = [];
    public ICollection<TvShowCast> Cast { get; set; } = [];
    public ICollection<TvShowTag> Tags { get; set; } = [];
    public ICollection<TmdbCandidate> TmdbCandidates { get; set; } = [];
}