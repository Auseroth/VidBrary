namespace MediaCatalog.Services.Scanner;

public class ScanResult
{
    public int MoviesAdded { get; set; }
    public int MoviesUpdated { get; set; }
    public int ShowsAdded { get; set; }
    public int EpisodesAdded { get; set; }
    public int FilesSkipped { get; set; }
    public List<string> Warnings { get; set; } = [];
    public TimeSpan Duration { get; set; }

    public override string ToString() =>
        $"Scan complete in {Duration.TotalSeconds:F1}s — " +
        $"{MoviesAdded} movies added, {ShowsAdded} shows added, " +
        $"{EpisodesAdded} episodes added, {FilesSkipped} skipped.";
}