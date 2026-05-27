namespace VidBrary.Models;

public class Movie
{
    public int Id { get; set; }

    // --- File Info ---
    public required string FilePath { get; set; }
    public required string FileName { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime FileLastScanned { get; set; }

    // --- TMDB Match ---
    public int? TmdbId { get; set; }
    public MatchStatus MatchStatus { get; set; } = MatchStatus.NoResults;
    public int TmdbCandidateCount { get; set; }

    // --- TMDB Metadata ---
    public string? Title { get; set; }
    public string? OriginalTitle { get; set; }
    public int? Year { get; set; }
    public string? Overview { get; set; }
    public double? TmdbRating { get; set; }
    public int? RuntimeMinutes { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public string? CertificationRating { get; set; }   // e.g. PG-13
    public string? OriginalLanguage { get; set; }

    // --- MediaInfo (from file) ---
    public string? VideoCodec { get; set; }
    public string? VideoResolution { get; set; }       // e.g. 1920x1080
    public string? AudioCodec { get; set; }
    public int? AudioChannels { get; set; }
    public string? SubtitleTracks { get; set; }        // JSON array of language codes
    public string? AudioTracks { get; set; }           // JSON array of language codes

    // --- Relations ---
    public int? CollectionId { get; set; }
    public MediaCollection? Collection { get; set; }

    public ICollection<MovieGenre> Genres { get; set; } = [];
    public ICollection<MovieCast> Cast { get; set; } = [];
    public ICollection<MovieCrew> Crew { get; set; } = [];
    public ICollection<MovieTag> Tags { get; set; } = [];
    public ICollection<WatchHistory> WatchHistory { get; set; } = [];
    public ICollection<UserRating> Ratings { get; set; } = [];
    public ICollection<TmdbCandidate> TmdbCandidates { get; set; } = [];
}