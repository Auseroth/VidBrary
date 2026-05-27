    namespace VidBrary.Models;

public class UserProfile
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? AvatarPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WatchHistory> WatchHistory { get; set; } = [];
    public ICollection<UserRating> Ratings { get; set; } = [];
    public ICollection<MyListItem> MyList { get; set; } = [];
}