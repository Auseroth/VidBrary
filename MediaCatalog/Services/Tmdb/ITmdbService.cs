using VidBrary.Models;

namespace VidBrary.Services.Tmdb;

public interface ITmdbService
{
    /// <summary>Search and store candidates for all unmatched movies.</summary>
    Task EnrichAllMoviesAsync(IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>Search and store candidates for all unmatched TV shows.</summary>
    Task EnrichAllShowsAsync(IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>Fetch and store full metadata for a movie that already has a confirmed TmdbId.</summary>
    Task FetchMovieDetailsAsync(int movieId, CancellationToken cancellationToken = default);

    /// <summary>Fetch and store full metadata for a TV show that already has a confirmed TmdbId.</summary>
    Task FetchShowDetailsAsync(int tvShowId, CancellationToken cancellationToken = default);

    /// <summary>Apply a user-selected candidate as the confirmed match for a movie.</summary>
    Task ApplyMovieMatchAsync(int movieId, int tmdbCandidateId);

    /// <summary>Apply a user-selected candidate as the confirmed match for a TV show.</summary>
    Task ApplyShowMatchAsync(int tvShowId, int tmdbCandidateId);

    /// <summary>Clear the confirmed match for a movie, leaving candidates intact.</summary>
    Task ClearMovieMatchAsync(int movieId);

    /// <summary>Clear the confirmed match for a TV show, leaving candidates intact.</summary>
    Task ClearShowMatchAsync(int tvShowId);

    /// <summary>Reset a movie back to PendingSelection (undo "No Match").</summary>
    Task ResetMovieToPendingAsync(int movieId);

    /// <summary>Reset a TV show back to PendingSelection (undo "No Match").</summary>
    Task ResetShowToPendingAsync(int tvShowId);
}