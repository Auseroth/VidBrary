namespace MediaCatalog.Models;

public class UserRating
{
    public int Id { get; set; }
    public int UserProfileId { get; set; }
    public UserProfile UserProfile { get; set; } = null!;

    public int? MovieId { get; set; }
    public Movie? Movie { get; set; }

    public int? EpisodeId { get; set; }
    public Episode? Episode { get; set; }

    /// <summary>1–10 user rating.</summary>
    public int Rating { get; set; }
    public DateTime RatedAt { get; set; } = DateTime.UtcNow;
}