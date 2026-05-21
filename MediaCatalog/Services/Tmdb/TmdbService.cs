using MediaCatalog.Data;
using MediaCatalog.Models;
using MediaCatalog.Services.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;
using TmdbMovie = TMDbLib.Objects.Movies.Movie;
using TmdbTvShow = TMDbLib.Objects.TvShows.TvShow;
using Movie = MediaCatalog.Models.Movie;
using TvShow = MediaCatalog.Models.TvShow;

namespace MediaCatalog.Services.Tmdb;

public class TmdbService(
    MediaCatalogDbContext db,
    ISettingsService settingsService,
    ILogger<TmdbService> logger) : ITmdbService
{
    // TMDB image base — poster sizes: w185, w342, w500, w780, original
    private const string ImageBase = "https://image.tmdb.org/t/p/";
    private const string PosterSize = "w342";
    private const string BackdropSize = "w780";
    private const string ProfileSize = "w185";

    // Polite delay between API calls to respect rate limits
    private static readonly TimeSpan RateDelay = TimeSpan.FromMilliseconds(250);

    private TMDbClient CreateClient()
    {
        var key = settingsService.Current.TmdbApiKey;
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                "TMDB API key is not configured. Add it in Settings.");
        return new TMDbClient(key);
    }

    // ── Bulk Enrichment ───────────────────────────────────────────────────────

    public async Task EnrichAllMoviesAsync(IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Only process movies that haven't been searched yet
        var movies = await db.Movies
            .Where(m => m.MatchStatus == MatchStatus.NoResults && m.TmdbCandidateCount == 0)
            .ToListAsync(cancellationToken);

        logger.LogInformation("Enriching {Count} unmatched movies via TMDB", movies.Count);

        using var client = CreateClient();
        foreach (var movie in movies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"Searching TMDB: {movie.Title}");

            await SearchMovieCandidatesAsync(client, movie, cancellationToken);
            await Task.Delay(RateDelay, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Movie TMDB enrichment complete");
    }

    public async Task EnrichAllShowsAsync(IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var shows = await db.TvShows
            .Where(s => s.MatchStatus == MatchStatus.NoResults && s.TmdbCandidateCount == 0)
            .ToListAsync(cancellationToken);

        logger.LogInformation("Enriching {Count} unmatched TV shows via TMDB", shows.Count);

        using var client = CreateClient();
        foreach (var show in shows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"Searching TMDB: {show.Title}");

            await SearchShowCandidatesAsync(client, show, cancellationToken);
            await Task.Delay(RateDelay, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("TV show TMDB enrichment complete");
    }

    // ── Candidate Search ──────────────────────────────────────────────────────

    private async Task SearchMovieCandidatesAsync(TMDbClient client, Movie movie,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = await client.SearchMovieAsync(
                movie.Title ?? movie.FileName,
                year: movie.Year ?? 0,
                cancellationToken: cancellationToken);

            var hits = results?.Results;
            if (hits is null || hits.Count == 0)
            {
                movie.MatchStatus = MatchStatus.NoResults;
                movie.TmdbCandidateCount = 0;
                logger.LogDebug("No TMDB results for movie: {Title}", movie.Title);
                return;
            }

            // Store all candidates
            foreach (var hit in hits)
            {
                db.TmdbCandidates.Add(new TmdbCandidate
                {
                    TmdbId = hit.Id,
                    MediaType = Models.MediaType.Movie,
                    Title = hit.Title ?? string.Empty,
                    Year = hit.ReleaseDate?.Year,
                    Overview = hit.Overview,
                    PosterPath = BuildImageUrl(hit.PosterPath, PosterSize),
                    TmdbRating = hit.VoteAverage,
                    MovieId = movie.Id
                });
            }

            movie.TmdbCandidateCount = hits.Count;

            if (hits.Count == 1)
            {
                // Auto-select the single result
                movie.TmdbId = hits[0].Id;
                movie.MatchStatus = MatchStatus.AutoMatched;
                await ApplyMovieMetadataAsync(client, movie, hits[0].Id, cancellationToken);
            }
            else
            {
                movie.MatchStatus = MatchStatus.PendingSelection;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TMDB search failed for movie: {Title}", movie.Title);
        }
    }

    private async Task SearchShowCandidatesAsync(TMDbClient client, TvShow show,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = await client.SearchTvShowAsync(
                show.Title ?? show.FolderName,
                cancellationToken: cancellationToken);

            var hits = results?.Results;
            if (hits is null || hits.Count == 0)
            {
                show.MatchStatus = MatchStatus.NoResults;
                show.TmdbCandidateCount = 0;
                return;
            }

            foreach (var hit in hits)
            {
                db.TmdbCandidates.Add(new TmdbCandidate
                {
                    TmdbId = hit.Id,
                    MediaType = Models.MediaType.TvShow,
                    Title = hit.Name ?? string.Empty,
                    Year = hit.FirstAirDate?.Year,
                    Overview = hit.Overview,
                    PosterPath = BuildImageUrl(hit.PosterPath, PosterSize),
                    TmdbRating = hit.VoteAverage,
                    TvShowId = show.Id
                });
            }

            show.TmdbCandidateCount = hits.Count;

            if (hits.Count == 1)
            {
                show.TmdbId = hits[0].Id;
                show.MatchStatus = MatchStatus.AutoMatched;
                await ApplyShowMetadataAsync(client, show, hits[0].Id, cancellationToken);
            }
            else
            {
                show.MatchStatus = MatchStatus.PendingSelection;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TMDB search failed for show: {Title}", show.Title);
        }
    }

    // ── Full Detail Fetch ─────────────────────────────────────────────────────

    public async Task FetchMovieDetailsAsync(int movieId,
        CancellationToken cancellationToken = default)
    {
        var movie = await db.Movies.FindAsync([movieId], cancellationToken)
            ?? throw new ArgumentException($"Movie {movieId} not found");

        if (movie.TmdbId is null) return;

        using var client = CreateClient();
        await ApplyMovieMetadataAsync(client, movie, movie.TmdbId.Value, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task FetchShowDetailsAsync(int tvShowId,
        CancellationToken cancellationToken = default)
    {
        var show = await db.TvShows.FindAsync([tvShowId], cancellationToken)
            ?? throw new ArgumentException($"TvShow {tvShowId} not found");

        if (show.TmdbId is null) return;

        using var client = CreateClient();
        await ApplyShowMetadataAsync(client, show, show.TmdbId.Value, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    // ── Match Management ──────────────────────────────────────────────────────

    public async Task ApplyMovieMatchAsync(int movieId, int tmdbCandidateId)
    {
        var movie = await db.Movies.FindAsync(movieId)
            ?? throw new ArgumentException($"Movie {movieId} not found");

        var candidate = await db.TmdbCandidates.FindAsync(tmdbCandidateId)
            ?? throw new ArgumentException($"Candidate {tmdbCandidateId} not found");

        movie.TmdbId = candidate.TmdbId;
        movie.MatchStatus = MatchStatus.ManualMatched;

        using var client = CreateClient();
        await ApplyMovieMetadataAsync(client, movie, candidate.TmdbId);
        await db.SaveChangesAsync();
    }

    public async Task ApplyShowMatchAsync(int tvShowId, int tmdbCandidateId)
    {
        var show = await db.TvShows.FindAsync(tvShowId)
            ?? throw new ArgumentException($"TvShow {tvShowId} not found");

        var candidate = await db.TmdbCandidates.FindAsync(tmdbCandidateId)
            ?? throw new ArgumentException($"Candidate {tmdbCandidateId} not found");

        show.TmdbId = candidate.TmdbId;
        show.MatchStatus = MatchStatus.ManualMatched;

        using var client = CreateClient();
        await ApplyShowMetadataAsync(client, show, candidate.TmdbId);
        await db.SaveChangesAsync();
    }

    public async Task ClearMovieMatchAsync(int movieId)
    {
        var movie = await db.Movies.FindAsync(movieId)
            ?? throw new ArgumentException($"Movie {movieId} not found");

        movie.TmdbId = null;
        movie.MatchStatus = movie.TmdbCandidateCount > 0
            ? MatchStatus.ManuallyUnmatched
            : MatchStatus.NoResults;

        await db.SaveChangesAsync();
    }

    public async Task ClearShowMatchAsync(int tvShowId)
    {
        var show = await db.TvShows.FindAsync(tvShowId)
            ?? throw new ArgumentException($"TvShow {tvShowId} not found");

        show.TmdbId = null;
        show.MatchStatus = show.TmdbCandidateCount > 0
            ? MatchStatus.ManuallyUnmatched
            : MatchStatus.NoResults;

        await db.SaveChangesAsync();
    }

    // ── Metadata Application ──────────────────────────────────────────────────

    private async Task ApplyMovieMetadataAsync(TMDbClient client, Movie movie, int tmdbId,
        CancellationToken cancellationToken = default)
    {
        var details = await client.GetMovieAsync(
            tmdbId,
            MovieMethods.Credits | MovieMethods.ReleaseDates,
            cancellationToken);

        if (details is null) return;

        movie.Title = details.Title;
        movie.OriginalTitle = details.OriginalTitle;
        movie.Year = details.ReleaseDate?.Year;
        movie.Overview = details.Overview;
        movie.TmdbRating = details.VoteAverage;
        movie.RuntimeMinutes = details.Runtime;
        movie.PosterPath = BuildImageUrl(details.PosterPath, PosterSize);
        movie.BackdropPath = BuildImageUrl(details.BackdropPath, BackdropSize);
        movie.OriginalLanguage = details.OriginalLanguage;

        // Certification (US rating like PG-13)
        var usRelease = details.ReleaseDates?.Results
            ?.FirstOrDefault(r => r.Iso_3166_1 == "US");
        movie.CertificationRating = usRelease?.ReleaseDates
            ?.FirstOrDefault(d => !string.IsNullOrEmpty(d.Certification))?.Certification;

        // Genres
        await UpsertMovieGenresAsync(movie, details.Genres ?? []);

        // Collection
        if (details.BelongsToCollection is not null)
            await UpsertCollectionAsync(client, movie, details.BelongsToCollection);

        // Cast & Crew (top 20 cast, directors + writers)
        if (details.Credits is not null)
            await UpsertMovieCreditsAsync(movie, details.Credits);

        logger.LogDebug("Applied TMDB metadata for movie: {Title} ({Year})", movie.Title, movie.Year);
    }

    private async Task ApplyShowMetadataAsync(TMDbClient client, TvShow show, int tmdbId,
        CancellationToken cancellationToken = default)
    {
        var details = await client.GetTvShowAsync(
            tmdbId,
            TvShowMethods.Credits,
            cancellationToken: cancellationToken);

        if (details is null) return;

        show.Title = details.Name;
        show.OriginalTitle = details.OriginalName;
        show.FirstAirYear = details.FirstAirDate?.Year;
        show.LastAirYear = details.LastAirDate?.Year;
        show.Overview = details.Overview;
        show.TmdbRating = details.VoteAverage;
        show.PosterPath = BuildImageUrl(details.PosterPath, PosterSize);
        show.BackdropPath = BuildImageUrl(details.BackdropPath, BackdropSize);
        show.Status = details.Status;
        show.OriginalLanguage = details.OriginalLanguage;
        show.TotalSeasons = details.NumberOfSeasons;
        show.TotalEpisodes = details.NumberOfEpisodes;

        await UpsertShowGenresAsync(show, details.Genres ?? []);

        if (details.Credits is not null)
            await UpsertShowCreditsAsync(show, details.Credits);

        // Enrich each season's episodes from TMDB
        await EnrichEpisodesAsync(client, show, cancellationToken);

        logger.LogDebug("Applied TMDB metadata for show: {Title}", show.Title);
    }

    private async Task EnrichEpisodesAsync(TMDbClient client, TvShow show,
        CancellationToken cancellationToken)
    {
        var seasons = await db.Seasons
            .Include(s => s.Episodes)
            .Where(s => s.TvShowId == show.Id)
            .ToListAsync(cancellationToken);

        foreach (var season in seasons)
        {
            var seasonDetails = await client.GetTvSeasonAsync(
                show.TmdbId!.Value, season.SeasonNumber,
                cancellationToken: cancellationToken);

            if (seasonDetails is null) continue;

            season.Name = seasonDetails.Name ?? season.Name;
            season.Overview = seasonDetails.Overview;
            season.PosterPath = BuildImageUrl(seasonDetails.PosterPath, PosterSize);
            season.AirYear = seasonDetails.AirDate?.Year;

            foreach (var ep in season.Episodes)
            {
                var tmdbEp = seasonDetails.Episodes?
                    .FirstOrDefault(e => e.EpisodeNumber == ep.EpisodeNumber);
                if (tmdbEp is null) continue;

                ep.Title = tmdbEp.Name;
                ep.Overview = tmdbEp.Overview;
                ep.AirDate = tmdbEp.AirDate.HasValue
                    ? DateOnly.FromDateTime(tmdbEp.AirDate.Value) : null;
                ep.RuntimeMinutes = tmdbEp.Runtime;
                ep.TmdbRating = tmdbEp.VoteAverage;
                ep.StillPath = BuildImageUrl(tmdbEp.StillPath, "w300");
            }

            await Task.Delay(RateDelay, cancellationToken);
        }
    }

    // ── Genre Helpers ─────────────────────────────────────────────────────────

    private async Task UpsertMovieGenresAsync(Movie movie,
        IEnumerable<TMDbLib.Objects.General.Genre> tmdbGenres)
    {
        // Remove existing genre links for this movie
        var existing = db.Set<MovieGenre>().Where(mg => mg.MovieId == movie.Id);
        db.Set<MovieGenre>().RemoveRange(existing);

        foreach (var g in tmdbGenres)
        {
            var genre = await db.Genres.FirstOrDefaultAsync(x => x.TmdbGenreId == g.Id)
                ?? new Genre { TmdbGenreId = g.Id, Name = g.Name ?? string.Empty };

            if (genre.Id == 0) db.Genres.Add(genre);
            await db.SaveChangesAsync();

            db.Set<MovieGenre>().Add(new MovieGenre { MovieId = movie.Id, GenreId = genre.Id });
        }
    }

    private async Task UpsertShowGenresAsync(TvShow show,
        IEnumerable<TMDbLib.Objects.General.Genre> tmdbGenres)
    {
        var existing = db.Set<TvShowGenre>().Where(sg => sg.TvShowId == show.Id);
        db.Set<TvShowGenre>().RemoveRange(existing);

        foreach (var g in tmdbGenres)
        {
            var genre = await db.Genres.FirstOrDefaultAsync(x => x.TmdbGenreId == g.Id)
                ?? new Genre { TmdbGenreId = g.Id, Name = g.Name ?? string.Empty };

            if (genre.Id == 0) db.Genres.Add(genre);
            await db.SaveChangesAsync();

            db.Set<TvShowGenre>().Add(new TvShowGenre { TvShowId = show.Id, GenreId = genre.Id });
        }
    }

    // ── Collection Helper ─────────────────────────────────────────────────────

    private async Task UpsertCollectionAsync(TMDbClient client, Movie movie,
        SearchCollection tmdbCollection)
    {
        var collection = await db.Collections
            .FirstOrDefaultAsync(c => c.TmdbCollectionId == tmdbCollection.Id);

        if (collection is null)
        {
            // Fetch full collection details for overview + backdrop
            var details = await client.GetCollectionAsync(tmdbCollection.Id);
            collection = new MediaCollection
            {
                Name = tmdbCollection.Name ?? string.Empty,
                TmdbCollectionId = tmdbCollection.Id,
                IsUserCreated = false,
                PosterPath = BuildImageUrl(tmdbCollection.PosterPath, PosterSize),
                BackdropPath = BuildImageUrl(tmdbCollection.BackdropPath, BackdropSize),
                Overview = details?.Overview
            };
            db.Collections.Add(collection);
            await db.SaveChangesAsync();
        }

        movie.CollectionId = collection.Id;
    }

    // ── Credits Helpers ───────────────────────────────────────────────────────

    private async Task UpsertMovieCreditsAsync(Movie movie,
        TMDbLib.Objects.Movies.Credits credits)
    {
        // Top 20 cast members
        foreach (var c in (credits.Cast ?? []).Take(20))
        {
            var person = await UpsertPersonAsync(c.Id, c.Name ?? string.Empty, c.ProfilePath);
            if (db.Set<MovieCast>().Any(x => x.MovieId == movie.Id && x.PersonId == person.Id))
                continue;

            db.Set<MovieCast>().Add(new MovieCast
            {
                MovieId = movie.Id,
                PersonId = person.Id,
                Character = c.Character,
                Order = c.Order
            });
        }

        // Directors and writers only from crew
        foreach (var c in (credits.Crew ?? []).Where(c =>
            c.Job is "Director" or "Writer" or "Screenplay" or "Story"))
        {
            var person = await UpsertPersonAsync(c.Id, c.Name ?? string.Empty, c.ProfilePath);
            if (db.Set<MovieCrew>().Any(x => x.MovieId == movie.Id
                && x.PersonId == person.Id && x.Job == c.Job)) continue;

            db.Set<MovieCrew>().Add(new MovieCrew
            {
                MovieId = movie.Id,
                PersonId = person.Id,
                Job = c.Job ?? string.Empty,
                Department = c.Department ?? string.Empty
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task UpsertShowCreditsAsync(TvShow show,
        TMDbLib.Objects.TvShows.Credits credits)
    {
        foreach (var c in (credits.Cast ?? []).Take(20))
        {
            var person = await UpsertPersonAsync(c.Id, c.Name ?? string.Empty, c.ProfilePath);
            if (db.Set<TvShowCast>().Any(x => x.TvShowId == show.Id && x.PersonId == person.Id))
                continue;

            db.Set<TvShowCast>().Add(new TvShowCast
            {
                TvShowId = show.Id,
                PersonId = person.Id,
                Character = c.Character,
                Order = c.Order
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task<Person> UpsertPersonAsync(int tmdbPersonId, string name,
        string? profilePath)
    {
        var person = await db.People.FirstOrDefaultAsync(p => p.TmdbPersonId == tmdbPersonId);
        if (person is not null) return person;

        person = new Person
        {
            TmdbPersonId = tmdbPersonId,
            Name = name,
            ProfilePath = BuildImageUrl(profilePath, ProfileSize)
        };
        db.People.Add(person);
        await db.SaveChangesAsync();
        return person;
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private static string? BuildImageUrl(string? path, string size) =>
        string.IsNullOrWhiteSpace(path) ? null : $"{ImageBase}{size}{path}";
}