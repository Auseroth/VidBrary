namespace MediaCatalog.Models;

// Many-to-many joins

public class MovieGenre
{
    public int MovieId { get; set; }
    public Movie Movie { get; set; } = null!;
    public int GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
}

public class TvShowGenre
{
    public int TvShowId { get; set; }
    public TvShow TvShow { get; set; } = null!;
    public int GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
}

public class MovieTag
{
    public int MovieId { get; set; }
    public Movie Movie { get; set; } = null!;
    public int UserTagId { get; set; }
    public UserTag UserTag { get; set; } = null!;
}

public class TvShowTag
{
    public int TvShowId { get; set; }
    public TvShow TvShow { get; set; } = null!;
    public int UserTagId { get; set; }
    public UserTag UserTag { get; set; } = null!;
}

public class MovieCast
{
    public int MovieId { get; set; }
    public Movie Movie { get; set; } = null!;
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;
    public string? Character { get; set; }
    public int Order { get; set; }
}

public class MovieCrew
{
    public int MovieId { get; set; }
    public Movie Movie { get; set; } = null!;
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;
    public required string Job { get; set; }           // e.g. Director, Writer
    public required string Department { get; set; }
}

public class TvShowCast
{
    public int TvShowId { get; set; }
    public TvShow TvShow { get; set; } = null!;
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;
    public string? Character { get; set; }
    public int Order { get; set; }
}