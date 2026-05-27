using VidBrary.Models;

public class RecentlyWatchedViewModel
{
    public int? MovieId { get; }
    public int? TvShowId { get; }
    public string Title { get; }
    public string? PosterPath { get; }
    public string SubLabel { get; }
    public DateTime WatchedAt { get; }

    public RecentlyWatchedViewModel(WatchHistory w)
    {
        WatchedAt = w.WatchedAt;
        if (w.Movie is not null)
        {
            MovieId = w.Movie.Id;
            Title = w.Movie.Title ?? w.Movie.FileName;
            PosterPath = w.Movie.PosterPath;
            SubLabel = "Movie";
        }
        else if (w.Episode?.Season?.TvShow is { } show)
        {
            TvShowId = show.Id;
            Title = show.Title ?? show.FolderName;
            PosterPath = show.PosterPath;
            SubLabel = $"S{w.Episode.Season.SeasonNumber:D2}E{w.Episode.EpisodeNumber:D2}";
        }
        else
        {
            Title = "Unknown";
            SubLabel = string.Empty;
        }
    }
}