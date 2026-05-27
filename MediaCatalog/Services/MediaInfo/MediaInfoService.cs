using VidBrary.Data;
using MediaInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MI = MediaInfo.MediaInfo;

namespace VidBrary.Services.MediaInfo;

public class MediaInfoService(
    VidBraryDbContext db,
    ILogger<MediaInfoService> logger) : IMediaInfoService
{
    public async Task EnrichAllAsync(IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var movies = await db.Movies
            .Where(m => m.VideoResolution == null)
            .ToListAsync(cancellationToken);

        foreach (var movie in movies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"Reading file info: {movie.Title ?? movie.FileName}");
            ApplyFileInfo(movie.FilePath, out var info);
            movie.VideoCodec      = info.VideoCodec;
            movie.VideoResolution = info.VideoResolution;
            movie.AudioCodec      = info.AudioCodec;
            movie.AudioChannels   = info.AudioChannels;
            movie.AudioTracks     = info.AudioTracks;
            movie.SubtitleTracks  = info.SubtitleTracks;
        }

        await db.SaveChangesAsync(cancellationToken);

        var episodes = await db.Episodes
            .Where(e => e.VideoResolution == null)
            .ToListAsync(cancellationToken);

        foreach (var ep in episodes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ApplyFileInfo(ep.FilePath, out var info);
            ep.VideoCodec      = info.VideoCodec;
            ep.VideoResolution = info.VideoResolution;
            ep.AudioCodec      = info.AudioCodec;
            ep.AudioChannels   = info.AudioChannels;
            ep.AudioTracks     = info.AudioTracks;
            ep.SubtitleTracks  = info.SubtitleTracks;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("MediaInfo enrichment complete — {M} movies, {E} episodes",
            movies.Count, episodes.Count);
    }

    public async Task EnrichMovieAsync(int movieId,
        CancellationToken cancellationToken = default)
    {
        var movie = await db.Movies.FindAsync([movieId], cancellationToken);
        if (movie is null) return;

        ApplyFileInfo(movie.FilePath, out var info);
        movie.VideoCodec      = info.VideoCodec;
        movie.VideoResolution = info.VideoResolution;
        movie.AudioCodec      = info.AudioCodec;
        movie.AudioChannels   = info.AudioChannels;
        movie.AudioTracks     = info.AudioTracks;
        movie.SubtitleTracks  = info.SubtitleTracks;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task EnrichEpisodeAsync(int episodeId,
        CancellationToken cancellationToken = default)
    {
        var ep = await db.Episodes.FindAsync([episodeId], cancellationToken);
        if (ep is null) return;

        ApplyFileInfo(ep.FilePath, out var info);
        ep.VideoCodec      = info.VideoCodec;
        ep.VideoResolution = info.VideoResolution;
        ep.AudioCodec      = info.AudioCodec;
        ep.AudioChannels   = info.AudioChannels;
        ep.AudioTracks     = info.AudioTracks;
        ep.SubtitleTracks  = info.SubtitleTracks;

        await db.SaveChangesAsync(cancellationToken);
    }

    // ── Core extraction ───────────────────────────────────────────────────────

    private void ApplyFileInfo(string filePath, out FileMediaInfo result)
    {
        result = new FileMediaInfo();

        if (!System.IO.File.Exists(filePath))
        {
            logger.LogWarning("MediaInfo: file not found: {Path}", filePath);
            return;
        }

        try
        {
            using var mi = new MI();
            mi.Open(filePath);

            result.VideoCodec      = Clean(mi.Get(StreamKind.Video, 0, "Format"));
            result.VideoResolution = BuildResolution(mi);
            result.AudioCodec      = Clean(mi.Get(StreamKind.Audio, 0, "Format"));
            result.AudioChannels   = TryParseInt(mi.Get(StreamKind.Audio, 0, "Channel(s)"));

            var audioCount = TryParseInt(mi.Get(StreamKind.Audio, 0, "StreamCount")) ?? 0;
            var audioLangs = Enumerable.Range(0, audioCount)
                .Select(i => Clean(mi.Get(StreamKind.Audio, i, "Language/String")))
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();
            result.AudioTracks = audioLangs.Count > 0
                ? string.Join(", ", audioLangs) : null;

            var subCount = TryParseInt(mi.Get(StreamKind.Text, 0, "StreamCount")) ?? 0;
            var subLangs = Enumerable.Range(0, subCount)
                .Select(i => Clean(mi.Get(StreamKind.Text, i, "Language/String")))
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();
            result.SubtitleTracks = subLangs.Count > 0
                ? string.Join(", ", subLangs) : null;

            mi.Close();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MediaInfo failed for: {Path}", filePath);
        }
    }

    private static string BuildResolution(MI mi)
    {
        var w = mi.Get(StreamKind.Video, 0, "Width");
        var h = mi.Get(StreamKind.Video, 0, "Height");
        if (string.IsNullOrEmpty(w) || string.IsNullOrEmpty(h)) return string.Empty;

        return int.TryParse(h, out var height) ? height switch
        {
            >= 2160 => $"{w}×{h} (4K)",
            >= 1080 => $"{w}×{h} (1080p)",
            >= 720  => $"{w}×{h} (720p)",
            >= 480  => $"{w}×{h} (480p)",
            _       => $"{w}×{h}"
        } : $"{w}×{h}";
    }

    private static string? Clean(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int? TryParseInt(string value) =>
        int.TryParse(value, out var n) ? n : null;
}

internal class FileMediaInfo
{
    public string? VideoCodec { get; set; }
    public string? VideoResolution { get; set; }
    public string? AudioCodec { get; set; }
    public int? AudioChannels { get; set; }
    public string? AudioTracks { get; set; }
    public string? SubtitleTracks { get; set; }
}