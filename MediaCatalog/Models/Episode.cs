namespace VidBrary.Models;

public class Episode
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;

    // --- File Info ---
    public required string FilePath { get; set; }
    public required string FileName { get; set; }
    public long FileSizeBytes { get; set; }

    // --- Parsed / TMDB ---
    public int EpisodeNumber { get; set; }
    public string? Title { get; set; }
    public string? Overview { get; set; }
    public DateOnly? AirDate { get; set; }
    public int? RuntimeMinutes { get; set; }
    public double? TmdbRating { get; set; }
    public string? StillPath { get; set; }            // Episode thumbnail from TMDB

    // --- MediaInfo ---
    public string? VideoCodec { get; set; }
    public string? VideoResolution { get; set; }
    public string? AudioCodec { get; set; }
    public int? AudioChannels { get; set; }
    public string? SubtitleTracks { get; set; }
    public string? AudioTracks { get; set; }

    public ICollection<WatchHistory> WatchHistory { get; set; } = [];
    public ICollection<UserRating> Ratings { get; set; } = [];
}