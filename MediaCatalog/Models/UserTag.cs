namespace MediaCatalog.Models;

public class UserTag
{
    public int Id { get; set; }
    public required string Name { get; set; }

    /// <summary>Resource key for a built-in icon, e.g. "icon_4k", "icon_favorite"</summary>
    public string? IconKey { get; set; }
    public string? ColorHex { get; set; }

    public ICollection<MovieTag> MovieTags { get; set; } = [];
    public ICollection<TvShowTag> TvShowTags { get; set; } = [];
}