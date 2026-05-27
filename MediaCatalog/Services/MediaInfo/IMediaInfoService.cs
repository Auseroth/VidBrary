namespace VidBrary.Services.MediaInfo;

public interface IMediaInfoService
{
    /// <summary>Read technical file info and persist to the Movie record.</summary>
    Task EnrichMovieAsync(int movieId, CancellationToken cancellationToken = default);

    /// <summary>Read technical file info and persist to the Episode record.</summary>
    Task EnrichEpisodeAsync(int episodeId, CancellationToken cancellationToken = default);

    /// <summary>Enrich all movies and episodes that have no technical data yet.</summary>
    Task EnrichAllAsync(IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}