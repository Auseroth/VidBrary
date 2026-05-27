namespace VidBrary.Models;

public enum MatchStatus
{
    NoResults,        // Grey  — TMDB returned nothing
    AutoMatched,      // Blue  — exactly 1 result, auto-selected
    ManualMatched,    // Blue  — user picked from multiple
    PendingSelection, // Red   — multiple results, user hasn't acted
    ManuallyUnmatched // Yellow — results exist but user chose none
}

public enum MediaType
{
    Movie,
    TvShow
}

public enum ViewMode
{
    DetailList,
    ComfortableGrid,
    CompactGrid,
    BannerView
}

public enum AppTheme
{
    FollowWindows,
    Light,
    Dark
}