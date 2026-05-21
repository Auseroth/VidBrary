using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaCatalog.Data;
using MediaCatalog.Services.Navigation;
using MediaCatalog.Services.Settings;
using MediaCatalog.ViewModels.Base;
using Microsoft.EntityFrameworkCore;

namespace MediaCatalog.ViewModels;

public partial class HomeViewModel(
    MediaCatalogDbContext db,
    ISettingsService settingsService,
    INavigationService navigation,
    ScanViewModel scanViewModel) : ViewModelBase
{
    // ── Stats ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private int _totalMovies;
    [ObservableProperty] private int _totalShows;
    [ObservableProperty] private int _totalEpisodes;
    [ObservableProperty] private string _totalRuntime = "—";
    [ObservableProperty] private int _pendingMatches;

    // ── Recently Added ────────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<MovieListItemViewModel> _recentMovies = [];

    [ObservableProperty]
    private ObservableCollection<TvShowListItemViewModel> _recentShows = [];

    // ── Random pick ───────────────────────────────────────────────────────────
    [ObservableProperty] private MovieListItemViewModel? _randomMovie;
    [ObservableProperty] private bool _showRandomMovie;

    public ScanViewModel ScanViewModel => scanViewModel;

    public override async Task OnNavigatedToAsync(object? parameter = null) =>
        await LoadAsync();

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        IsBusy = true;

        TotalMovies   = await db.Movies.CountAsync();
        TotalShows    = await db.TvShows.CountAsync();
        TotalEpisodes = await db.Episodes.CountAsync();
        PendingMatches = await db.Movies.CountAsync(
                             m => m.MatchStatus == Models.MatchStatus.PendingSelection)
                       + await db.TvShows.CountAsync(
                             s => s.MatchStatus == Models.MatchStatus.PendingSelection);

        var movieMinutes = await db.Movies.SumAsync(m => (int?)m.RuntimeMinutes ?? 0);
        var epMinutes    = await db.Episodes.SumAsync(e => (int?)e.RuntimeMinutes ?? 0);
        var span = TimeSpan.FromMinutes(movieMinutes + epMinutes);
        TotalRuntime = span.TotalDays >= 1
            ? $"{(int)span.TotalDays}d {span.Hours}h"
            : $"{span.Hours}h {span.Minutes}m";

        // Recently added — last 12 movies
        var recentMovies = await db.Movies
            .Include(m => m.Genres).ThenInclude(g => g.Genre)
            .Include(m => m.Tags).ThenInclude(t => t.UserTag)
            .Include(m => m.Collection)
            .OrderByDescending(m => m.FileLastScanned)
            .Take(12)
            .AsNoTracking()
            .ToListAsync();
        RecentMovies = new ObservableCollection<MovieListItemViewModel>(
            recentMovies.Select(MovieListItemViewModel.FromMovie));

        // Recently added — last 8 shows
        var recentShows = await db.TvShows
            .Include(s => s.Genres).ThenInclude(g => g.Genre)
            .Include(s => s.Tags).ThenInclude(t => t.UserTag)
            .OrderByDescending(s => s.LastScanned)
            .Take(8)
            .AsNoTracking()
            .ToListAsync();
        RecentShows = new ObservableCollection<TvShowListItemViewModel>(
            recentShows.Select(TvShowListItemViewModel.FromShow));

        ShowRandomMovie = false;
        IsBusy = false;
    }

    // ── Random pick ───────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task PickRandomMovieAsync()
    {
        var count = await db.Movies.CountAsync();
        if (count == 0) return;

        var skip = Random.Shared.Next(count);
        var movie = await db.Movies
            .Include(m => m.Genres).ThenInclude(g => g.Genre)
            .Include(m => m.Tags).ThenInclude(t => t.UserTag)
            .Include(m => m.Collection)
            .AsNoTracking()
            .Skip(skip)
            .FirstOrDefaultAsync();

        if (movie is null) return;
        RandomMovie    = MovieListItemViewModel.FromMovie(movie);
        ShowRandomMovie = true;
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenMovie(MovieListItemViewModel item) =>
        navigation.NavigateTo<MovieDetailViewModel>(item.Id);

    [RelayCommand]
    private void OpenShow(TvShowListItemViewModel item) =>
        navigation.NavigateTo<TvShowDetailViewModel>(item.Id);

    [RelayCommand]
    private void GoToMovies() => navigation.NavigateTo<MoviesViewModel>();

    [RelayCommand]
    private void GoToShows() => navigation.NavigateTo<TvShowsViewModel>();

    [RelayCommand]
    private void GoToActionNeeded() =>
        navigation.NavigateTo<MoviesViewModel>("filter:pending");
}