namespace MediaCatalog.Services.Scanner;

/// <summary>
/// Parses media filenames into a clean title and optional year.
/// Handles common naming patterns like:
///   "The Dark Knight (2008).mkv"
///   "Inception.2010.1080p.BluRay.mkv"
///   "S01E01 - Pilot.mkv"
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

    public static ParsedMediaName Parse(string fileNameWithoutExtension)
    {
        var name = fileNameWithoutExtension;

        // Detect episode pattern first
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

        // Extract year before stripping tags (year might be surrounded by dots/brackets)
        int? year = null;
        var yearMatch = YearRegex.Match(name);
        if (yearMatch.Success)
            year = int.Parse(yearMatch.Value);

        // Remove year and everything after it (quality tags follow the year)
        if (yearMatch.Success)
            name = name[..yearMatch.Index];

        // Strip any remaining noise tags
        foreach (var tag in NoiseTags)
            name = System.Text.RegularExpressions.Regex.Replace(
                name, $@"\b{System.Text.RegularExpressions.Regex.Escape(tag)}\b", " ",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Replace dots and underscores used as spaces
        name = name.Replace('.', ' ').Replace('_', ' ');

        // Remove leftover brackets/parentheses
        name = System.Text.RegularExpressions.Regex.Replace(name, @"[\[\](){}]", " ");

        // Collapse whitespace
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ").Trim();

        return new ParsedMediaName
        {
            CleanTitle = string.IsNullOrWhiteSpace(name) ? fileNameWithoutExtension : name,
            Year = year,
            IsEpisode = false
        };
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