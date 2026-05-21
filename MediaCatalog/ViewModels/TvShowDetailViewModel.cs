using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaCatalog.Data;
using MediaCatalog.Models;
using MediaCatalog.Services.Settings;
using MediaCatalog.Services.Tmdb;
using MediaCatalog.ViewModels.Base;
using Microsoft.EntityFrameworkCore;

namespace MediaCatalog.ViewModels;

public partial class TvShowDetailViewModel(
    MediaCatalogDbContext db,
    ITmdbService tmdb,
    ISettingsService settings) : ViewModelBase
{
    private int _showId;

    // ── Metadata ──────────────────────────────────────────────────────────────
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string? _originalTitle;
    [ObservableProperty] private string? _overview;
    [ObservableProperty] private double? _tmdbRating;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private string? _originalLanguage;
    [ObservableProperty] private string? _posterPath;
    [ObservableProperty] private string? _backdropPath;
    [ObservableProperty] private string _genres = string.Empty;
    [ObservableProperty] private int? _totalSeasons;
    [ObservableProperty] private int? _totalEpisodes;
    [ObservableProperty] private string _yearRange = string.Empty;

    // ── Cast ──────────────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<CastMemberViewModel> _cast = [];

    // ── Seasons ───────────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<SeasonViewModel> _seasons = [];

    // ── Tags ──────────────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<TagViewModel> _tags = [];
    [ObservableProperty] private ObservableCollection<TagViewModel> _availableTags = [];
    [ObservableProperty] private bool _showTagPicker;

    // ── Match ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private MatchStatus _matchStatus;
    [ObservableProperty] private int _candidateCount;
    [ObservableProperty] private bool _showMatchDialog;
    [ObservableProperty] private ObservableCollection<TmdbCandidateViewModel> _candidates = [];

    // ── Derived ───────────────────────────────────────────────────────────────
    public string RatingDisplay => TmdbRating.HasValue ? $"★ {TmdbRating:F1}" : "—";

    public string MatchButtonLabel => MatchStatus switch
    {
        MatchStatus.NoResults         => "No Results",
        MatchStatus.AutoMatched       => "Matched (1)",
        MatchStatus.ManualMatched     => $"Matched ({CandidateCount})",
        MatchStatus.PendingSelection  => $"Select Match ({CandidateCount} Results)",
        MatchStatus.ManuallyUnmatched => $"Unmatched ({CandidateCount} Available)",
        _                             => "Unknown"
    };

    public string MatchButtonColor => MatchStatus switch
    {
        MatchStatus.NoResults         => "#6c757d",
        MatchStatus.AutoMatched       => "#1a73e8",
        MatchStatus.ManualMatched     => "#1a73e8",
        MatchStatus.PendingSelection  => "#e94560",
        MatchStatus.ManuallyUnmatched => "#f59e0b",
        _                             => "#6c757d"
    };

    public bool MatchButtonEnabled => MatchStatus != MatchStatus.NoResults;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override async Task OnNavigatedToAsync(object? parameter = null)
    {
        if (parameter is not int showId) return;
        _showId = showId;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;

        var show = await db.TvShows
            .Include(s => s.Genres).ThenInclude(g => g.Genre)
            .Include(s => s.Tags).ThenInclude(t => t.UserTag)
            .Include(s => s.Cast).ThenInclude(c => c.Person)
            .Include(s => s.Seasons).ThenInclude(s => s.Episodes)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == _showId);

        if (show is null) { IsBusy = false; return; }

        Title           = show.Title ?? show.FolderName;
        OriginalTitle   = show.OriginalTitle;
        Overview        = show.Overview ?? "No overview available.";
        TmdbRating      = show.TmdbRating;
        Status          = show.Status;
        OriginalLanguage = show.OriginalLanguage?.ToUpperInvariant();
        PosterPath      = show.PosterPath;
        BackdropPath    = show.BackdropPath;
        Genres          = string.Join(" · ", show.Genres.Select(g => g.Genre.Name));
        TotalSeasons    = show.TotalSeasons;
        TotalEpisodes   = show.TotalEpisodes;
        MatchStatus     = show.MatchStatus;
        CandidateCount  = show.TmdbCandidateCount;

        YearRange = (show.FirstAirYear, show.LastAirYear) switch
        {
            (int f, int l) when f == l => $"{f}",
            (int f, int l)             => $"{f}–{l}",
            (int f, null)              => $"{f}–Present",
            _                          => "—"
        };

        Cast = new ObservableCollection<CastMemberViewModel>(
            show.Cast.OrderBy(c => c.Order).Take(20)
                .Select(c => new CastMemberViewModel
                {
                    PersonId  = c.PersonId,
                    Name      = c.Person.Name,
                    Character = c.Character ?? "—",
                    PhotoPath = c.Person.ProfilePath
                }));

        Seasons = new ObservableCollection<SeasonViewModel>(
            show.Seasons.OrderBy(s => s.SeasonNumber)
                        .Select(SeasonViewModel.FromSeason));

        Tags = new ObservableCollection<TagViewModel>(
            show.Tags.Select(t => new TagViewModel
            {
                Id      = t.UserTag.Id,
                Name    = t.UserTag.Name,
                IconKey = t.UserTag.IconKey,
                Color   = t.UserTag.ColorHex ?? "#0f3460"
            }));

        var allTags = await db.Tags.AsNoTracking().ToListAsync();
        AvailableTags = new ObservableCollection<TagViewModel>(
            allTags.Select(t => new TagViewModel
            {
                Id      = t.Id,
                Name    = t.Name,
                IconKey = t.IconKey,
                Color   = t.ColorHex ?? "#0f3460"
            }));

        OnPropertyChanged(nameof(RatingDisplay));
        OnPropertyChanged(nameof(MatchButtonLabel));
        OnPropertyChanged(nameof(MatchButtonColor));
        OnPropertyChanged(nameof(MatchButtonEnabled));

        IsBusy = false;
    }

    // ── Season accordion ──────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleSeason(SeasonViewModel season) =>
        season.IsExpanded = !season.IsExpanded;

    // ── Episode expand / play ─────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleEpisode(EpisodeViewModel episode) =>
        episode.IsExpanded = !episode.IsExpanded;

    [RelayCommand]
    private void PlayEpisode(EpisodeViewModel episode)
    {
        var player = settings.Current.DefaultMediaPlayerPath;
        var psi = string.IsNullOrWhiteSpace(player)
            ? new ProcessStartInfo(episode.FilePath) { UseShellExecute = true }
            : new ProcessStartInfo(player, $"\"{episode.FilePath}\"") { UseShellExecute = true };

        try { Process.Start(psi); }
        catch (Exception ex) { ErrorMessage = $"Could not launch player: {ex.Message}"; }
    }

    // ── Match dialog ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task OpenMatchDialogAsync()
    {
        if (!MatchButtonEnabled) return;

        var raw = await db.TmdbCandidates
            .Where(c => c.TvShowId == _showId)
            .AsNoTracking()
            .ToListAsync();

        Candidates = new ObservableCollection<TmdbCandidateViewModel>(
            raw.Select(TmdbCandidateViewModel.FromCandidate));

        if (MatchStatus is MatchStatus.AutoMatched or MatchStatus.ManualMatched)
        {
            var show = await db.TvShows.FindAsync(_showId);
            var current = Candidates.FirstOrDefault(c => c.TmdbId == show?.TmdbId);
            if (current is not null) current.IsSelected = true;
        }

        ShowMatchDialog = true;
    }

    [RelayCommand]
    private void ToggleCandidate(TmdbCandidateViewModel candidate)
    {
        if (!candidate.IsExpanded)
        {
            foreach (var c in Candidates) c.IsExpanded = false;
            candidate.IsExpanded = true;
        }
        else
        {
            var wasSelected = candidate.IsSelected;
            foreach (var c in Candidates) c.IsSelected = false;
            candidate.IsSelected = !wasSelected;
        }
    }

    [RelayCommand]
    private async Task SaveMatchAsync()
    {
        var selected = Candidates.FirstOrDefault(c => c.IsSelected);
        if (selected is null)
            await tmdb.ClearShowMatchAsync(_showId);
        else
            await tmdb.ApplyShowMatchAsync(_showId, selected.Id);

        ShowMatchDialog = false;
        await LoadAsync();
    }

    [RelayCommand]
    private void CloseMatchDialog() => ShowMatchDialog = false;

    // ── Tags ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleTagPicker() => ShowTagPicker = !ShowTagPicker;

    [RelayCommand]
    private async Task ToggleTagAsync(TagViewModel tag)
    {
        var existing = await db.Set<TvShowTag>()
            .FirstOrDefaultAsync(t => t.TvShowId == _showId && t.UserTagId == tag.Id);

        if (existing is null)
            db.Set<TvShowTag>().Add(new TvShowTag { TvShowId = _showId, UserTagId = tag.Id });
        else
            db.Set<TvShowTag>().Remove(existing);

        await db.SaveChangesAsync();
        await LoadAsync();
    }
}