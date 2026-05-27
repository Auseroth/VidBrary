namespace VidBrary.Models;

public class Genre
{
    public int Id { get; set; }
    public int TmdbGenreId { get; set; }
    public required string Name { get; set; }

    public ICollection<MovieGenre> MovieGenres { get; set; } = [];
    public ICollection<TvShowGenre> TvShowGenres { get; set; } = [];
}