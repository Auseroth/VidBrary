/// <summary>
/// Controls how seasons and episodes are ordered for a TV show.
/// </summary>
public enum SeasonOrderMode
{
    Default,        // Use global AppSettings.DefaultSeasonOrderMode
    TmdbAuto,       // Season number from TMDB; episode number from SxxExx tag
    ManualFolder    // Season order = folder name alpha; episode order = leading number in filename
}

/// <summary>Represents one item in the season-order ComboBox.</summary>
public record SeasonOrderOption(SeasonOrderMode? Mode, string Label)
{
    public override string ToString() => Label;
}