namespace VidBrary.Models;

public class WatchHistory
{
    public int Id { get; set; }
    public int UserProfileId { get; set; }
    public UserProfile UserProfile { get; set; } = null!;

    public int? MovieId { get; set; }
    public Movie? Movie { get; set; }

    public int? EpisodeId { get; set; }
    public Episode? Episode { get; set; }

    public DateTime WatchedAt { get; set; } = DateTime.UtcNow;
    public bool Completed { get; set; }

    /// <summary>Resume position in seconds.</summary>
    public int? ResumePositionSeconds { get; set; }
}