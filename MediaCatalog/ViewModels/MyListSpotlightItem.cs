public class MyListSpotlightItem
{
    public int? MovieId { get; }
    public int? TvShowId { get; }
    public string Title { get; }
    public string? PosterPath { get; }
    public string TypeLabel { get; }

    public MyListSpotlightItem(MyListItem item)
    {
        if (item.Movie is not null)
        {
            MovieId    = item.Movie.Id;
            Title      = item.Movie.Title ?? item.Movie.FileName;
            PosterPath = item.Movie.PosterPath;
            TypeLabel  = "Movie";
        }
        else if (item.TvShow is not null)
        {
            TvShowId   = item.TvShow.Id;
            Title      = item.TvShow.Title ?? item.TvShow.FolderName;
            PosterPath = item.TvShow.PosterPath;
            TypeLabel  = "TV Show";
        }
        else
        {
            Title     = "Unknown";
            TypeLabel = string.Empty;
        }
    }
}