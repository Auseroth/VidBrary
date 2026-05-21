namespace MediaCatalog.Services.Scanner;

/// <summary>
/// Parses media filenames into a clean title and optional year / episode info.
/// Handles common naming patterns like:
///   "The Dark Knight (2008).mkv"
///   "Inception.2010.1080p.BluRay.mkv"
///   "S01E01 - Pilot.mkv"
///   "01 - Pilot.mkv"  ← manual / Plex-style leading number
/// </summary>
public static class FileNameParser
{
    // Tags to strip from filenames before using as a title
    private static readonly string[] NoiseTags =
    [
        "1080p", "720p", "480p", "2160p", "4k", "uhd",
        "bluray", "blu-ray", "bdrip", "brrip", "webrip", "web-dl", "hdtv", "dvdrip",
        "x264", "x265", "h264", "h265", "hevc", "avc",
        "aac", "ac3", "dts", "truehd", "atmos",
        "extended", "remastered", "theatrical", "directors.cut",
        "yify", "yts", "rarbg", "ettv"
    ];

    private static readonly System.Text.RegularExpressions.Regex YearRegex =
        new(@"\b(19[0-9]{2}|20[0-9]{2})\b",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex EpisodeRegex =
        new(@"[Ss](\d{1,2})[Ee](\d{1,2})",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    // Matches "01 - Title", "01. Title", "01 Title" at the start of a filename
    private static readonly System.Text.RegularExpressions.Regex LeadingNumberRegex =
        new(@"^(\d{1,3})\s*[-.\s]\s*",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    public static ParsedMediaName Parse(string fileNameWithoutExtension)
    {
        var name = fileNameWithoutExtension;

        // 1. Detect SxxExx episode pattern
        var episodeMatch = EpisodeRegex.Match(name);
        if (episodeMatch.Success)
        {
            return new ParsedMediaName
            {
                CleanTitle = null,   // title comes from the parent folder (show name)
                Season = int.Parse(episodeMatch.Groups[1].Value),
                Episode = int.Parse(episodeMatch.Groups[2].Value),
                IsEpisode = true
            };
        }

        // 2. Detect leading-number episode pattern (e.g. "01 - Pilot")
        var leadingMatch = LeadingNumberRegex.Match(name);
        if (leadingMatch.Success)
        {
            var episodeNumber = int.Parse(leadingMatch.Groups[1].Value);
            // Strip the leading number + separator, then clean the remainder as a title
            var remainder = name[leadingMatch.Length..];
            var cleanTitle = CleanupTitle(remainder);
            return new ParsedMediaName
            {
                CleanTitle = string.IsNullOrWhiteSpace(cleanTitle) ? remainder : cleanTitle,
                Episode = episodeNumber,
                IsEpisode = true
            };
        }

        // 3. Standard movie / show folder parsing
        int? year = null;
        var yearMatch = YearRegex.Match(name);
        if (yearMatch.Success)
        {
            year = int.Parse(yearMatch.Value);
            name = name[..yearMatch.Index];
        }

        name = CleanupTitle(name);

        return new ParsedMediaName
        {
            CleanTitle = string.IsNullOrWhiteSpace(name) ? fileNameWithoutExtension : name,
            Year = year,
            IsEpisode = false
        };
    }

    private static string CleanupTitle(string name)
    {
        foreach (var tag in NoiseTags)
            name = System.Text.RegularExpressions.Regex.Replace(
                name, $@"\b{System.Text.RegularExpressions.Regex.Escape(tag)}\b", " ",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        name = name.Replace('.', ' ').Replace('_', ' ');
        name = System.Text.RegularExpressions.Regex.Replace(name, @"[\[\](){}]", " ");
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ").Trim();
        return name;
    }
}

public class ParsedMediaName
{
    public string? CleanTitle { get; init; }
    public int? Year { get; init; }
    public bool IsEpisode { get; init; }
    public int? Season { get; init; }
    public int? Episode { get; init; }
}