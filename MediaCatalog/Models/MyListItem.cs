using VidBrary.Models;

public class MyListItem
{
    public int Id { get; set; }
    public int UserProfileId { get; set; }
    public UserProfile UserProfile { get; set; } = null!;

    public int? MovieId { get; set; }
    public Movie? Movie { get; set; }

    public int? TvShowId { get; set; }
    public TvShow? TvShow { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}