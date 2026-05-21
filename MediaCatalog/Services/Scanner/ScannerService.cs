using System.Diagnostics;
using System.IO;
using MediaCatalog.Data;
using MediaCatalog.Models;
using MediaCatalog.Services.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediaCatalog.Services.Scanner;

public class ScannerService(
    MediaCatalogDbContext db,
    ISettingsService settingsService,
    ILogger<ScannerService> logger) : IScannerService
{
    public event EventHandler<string>? ProgressChanged;

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<ScanResult> ScanAllAsync(CancellationToken cancellationToken = default)
    {
        var result = new ScanResult();
        var sw = Stopwatch.StartNew();
        var settings = settingsService.Current;

        foreach (var dir in settings.MovieDirectories)
        {
            if (!Directory.Exists(dir))
            {
                result.Warnings.Add($"Movie directory not found: {dir}");
                logger.LogWarning("Movie directory not found: {Dir}", dir);
                continue;
            }

            var partial = await ScanDirectoryAsync(dir, isTvDirectory: false, cancellationToken);
            MergeResult(result, partial);
        }

        foreach (var dir in settings.TvShowDirectories)
        {
            if (!Directory.Exists(dir))
            {
                result.Warnings.Add($"TV directory not found: {dir}");
                logger.LogWarning("TV directory not found: {Dir}", dir);
                continue;
            }

            var partial = await ScanDirectoryAsync(dir, isTvDirectory: true, cancellationToken);
            MergeResult(result, partial);
        }

        sw.Stop();
        result.Duration = sw.Elapsed;
        logger.LogInformation("{Result}", result);
        return result;
    }

    public async Task<ScanResult> ScanDirectoryAsync(string path, bool isTvDirectory,
        CancellationToken cancellationToken = default)
    {
        var result = new ScanResult();

        if (isTvDirectory)
            await ScanTvDirectoryAsync(path, result, cancellationToken);
        else
            await ScanMovieDirectoryAsync(path, result, cancellationToken);

        return result;
    }

    // ── Movie Scanning ────────────────────────────────────────────────────────

    private async Task ScanMovieDirectoryAsync(string rootPath, ScanResult result,
        CancellationToken cancellationToken)
    {
        var extensions = settingsService.Current.AllowedExtensions;

        // Movies are media files directly in the root OR one level deep in their own folder
        var files = Directory
            .EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

        foreach (var filePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip files more than 1 level deep — those belong to TV shows
            var relativePath = Path.GetRelativePath(rootPath, filePath);
            if (relativePath.Split(Path.DirectorySeparatorChar).Length > 2)
            {
                result.FilesSkipped++;
                continue;
            }

            await UpsertMovieAsync(filePath, result);
        }
    }

    private async Task UpsertMovieAsync(string filePath, ScanResult result)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var existing = await db.Movies.FirstOrDefaultAsync(m => m.FilePath == filePath);

        if (existing is not null)
        {
            existing.FileLastScanned = DateTime.UtcNow;
            existing.FileSizeBytes = new FileInfo(filePath).Length;
            result.MoviesUpdated++;
            Report($"Updated movie: {existing.Title ?? fileName}");
        }
        else
        {
            var parsed = FileNameParser.Parse(fileName);
            var movie = new Movie
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSizeBytes = new FileInfo(filePath).Length,
                FileLastScanned = DateTime.UtcNow,
                Title = parsed.CleanTitle ?? fileName,
                Year = parsed.Year,
                MatchStatus = MatchStatus.NoResults
            };

            db.Movies.Add(movie);
            result.MoviesAdded++;
            Report($"Found movie: {movie.Title}");
        }

        await db.SaveChangesAsync();
    }

    // ── TV Scanning ───────────────────────────────────────────────────────────

    private async Task ScanTvDirectoryAsync(string rootPath, ScanResult result,
        CancellationToken cancellationToken)
    {
        // Each immediate subdirectory of rootPath is a show
        foreach (var showDir in Directory.EnumerateDirectories(rootPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UpsertShowAsync(showDir, result, cancellationToken);
        }
    }

    private async Task UpsertShowAsync(string showDir, ScanResult result,
        CancellationToken cancellationToken)
    {
        var folderName = Path.GetFileName(showDir)!;
        var parsed = FileNameParser.Parse(folderName);

        var show = await db.TvShows
            .Include(s => s.Seasons).ThenInclude(s => s.Episodes)
            .FirstOrDefaultAsync(s => s.DirectoryPath == showDir);

        if (show is null)
        {
            show = new TvShow
            {
                DirectoryPath = showDir,
                FolderName = folderName,
                Title = parsed.CleanTitle ?? folderName,
                FirstAirYear = parsed.Year,
                LastScanned = DateTime.UtcNow,
                MatchStatus = MatchStatus.NoResults
            };
            db.TvShows.Add(show);
            await db.SaveChangesAsync();
            result.ShowsAdded++;
            Report($"Found show: {show.Title}");
        }
        else
        {
            show.LastScanned = DateTime.UtcNow;
        }

        // Each subdirectory of the show dir is a season
        foreach (var seasonDir in Directory.EnumerateDirectories(showDir))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UpsertSeasonAsync(seasonDir, show, result, cancellationToken);
        }

        await db.SaveChangesAsync();
    }

    private async Task UpsertSeasonAsync(string seasonDir, TvShow show, ScanResult result,
        CancellationToken cancellationToken)
    {
        var seasonNumber = ParseSeasonNumber(Path.GetFileName(seasonDir)!);

        var season = show.Seasons.FirstOrDefault(s => s.DirectoryPath == seasonDir)
            ?? show.Seasons.FirstOrDefault(s => s.SeasonNumber == seasonNumber);

        if (season is null)
        {
            season = new Season
            {
                TvShowId = show.Id,
                SeasonNumber = seasonNumber,
                DirectoryPath = seasonDir,
                Name = $"Season {seasonNumber}"
            };
            db.Seasons.Add(season);
            await db.SaveChangesAsync();
        }

        var extensions = settingsService.Current.AllowedExtensions;
        var episodeFiles = Directory
            .EnumerateFiles(seasonDir, "*", SearchOption.TopDirectoryOnly)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

        foreach (var filePath in episodeFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UpsertEpisodeAsync(filePath, season, result);
        }
    }

    private async Task UpsertEpisodeAsync(string filePath, Season season, ScanResult result)
    {
        var existing = await db.Episodes.FirstOrDefaultAsync(e => e.FilePath == filePath);
        if (existing is not null) return;

        var parsed = FileNameParser.Parse(Path.GetFileNameWithoutExtension(filePath));
        var episode = new Episode
        {
            SeasonId = season.Id,
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            FileSizeBytes = new FileInfo(filePath).Length,
            EpisodeNumber = parsed.Episode ?? 0,
            Title = parsed.CleanTitle
        };

        db.Episodes.Add(episode);
        result.EpisodesAdded++;
        Report($"  + Episode S{season.SeasonNumber:D2}E{episode.EpisodeNumber:D2}: {filePath}");

        await db.SaveChangesAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int ParseSeasonNumber(string folderName)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            folderName, @"\b(\d+)\b");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    private static void MergeResult(ScanResult target, ScanResult source)
    {
        target.MoviesAdded += source.MoviesAdded;
        target.MoviesUpdated += source.MoviesUpdated;
        target.ShowsAdded += source.ShowsAdded;
        target.EpisodesAdded += source.EpisodesAdded;
        target.FilesSkipped += source.FilesSkipped;
        target.Warnings.AddRange(source.Warnings);
    }

    private void Report(string message)
    {
        logger.LogDebug("{Message}", message);
        ProgressChanged?.Invoke(this, message);
    }
}