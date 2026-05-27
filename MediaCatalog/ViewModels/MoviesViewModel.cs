using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VidBrary.Data;
using VidBrary.Models;
using VidBrary.Services.Navigation;
using VidBrary.ViewModels.Base;
using Microsoft.EntityFrameworkCore;

namespace VidBrary.ViewModels;

public partial class MoviesViewModel(
    VidBraryDbContext db,
    INavigationService navigation) : ViewModelBase
{
    private List<MovieListItemViewModel> _allMovies = [];

    [ObservableProperty] private ObservableCollection<MovieListItemViewModel> _movies = [];
    [ObservableProperty] private MovieListItemViewModel? _selectedMovie;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedGenre = "All";
    [ObservableProperty] private string _selectedMatchFilter = "All";
    [ObservableProperty] private string _selectedSort = "Title A–Z";
    [ObservableProperty] private ObservableCollection<string> _availableGenres = ["All"];
    [ObservableProperty] private ViewMode _viewMode = ViewMode.ComfortableGrid;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _filteredCount;

    public IReadOnlyList<string> MatchFilters { get; } =
    [
        "All", "Matched", "Action Needed", "No Match", "Unmatched"
    ];

    public IReadOnlyList<string> SortOptions { get; } =
    [
        "Title A–Z", "Title Z–A", "Year ↑", "Year ↓",
        "Rating ↓", "Rating ↑", "Runtime ↓"
    ];

    public IReadOnlyList<ViewMode> ViewModes { get; } = Enum.GetValues<ViewMode>().ToList();

    public override async Task OnNavigatedToAsync(object? parameter = null) =>
        await LoadMoviesAsync();

    [RelayCommand]
    private async Task RefreshAsync() => await LoadMoviesAsync();

    private async Task LoadMoviesAsync()
    {
        IsBusy = true;

        var movies = await db.Movies
            .Include(m => m.Genres).ThenInclude(g => g.Genre)
            .Include(m => m.Tags).ThenInclude(t => t.UserTag)
            .Include(m => m.Collection)
            .AsNoTracking()
            .ToListAsync();

        _allMovies = movies.Select(MovieListItemViewModel.FromMovie).ToList();
        TotalCount = _allMovies.Count;

        var genres = _allMovies
            .SelectMany(m => m.Genres.Split(", ", StringSplitOptions.RemoveEmptyEntries))
            .Distinct().OrderBy(g => g).Prepend("All");
        AvailableGenres = new ObservableCollection<string>(genres);

        ApplyFilters();
        IsBusy = false;
    }

    // Navigates when the user selects a row in Detail List view
    partial void OnSelectedMovieChanged(MovieListItemViewModel? value)
    {
        if (value is null) return;
        navigation.NavigateTo<MovieDetailViewModel>(value.Id);
        SelectedMovie = null; // reset so the same row can be re-selected
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedGenreChanged(string value) => ApplyFilters();
    partial void OnSelectedMatchFilterChanged(string value) => ApplyFilters();
    partial void OnSelectedSortChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var query = _allMovies.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim().ToLowerInvariant();
            query = query.Where(m =>
                m.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (m.Collection?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                m.Genres.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedGenre != "All")
            query = query.Where(m =>
                m.Genres.Contains(SelectedGenre, StringComparison.OrdinalIgnoreCase));

        query = SelectedMatchFilter switch
        {
            "Matched"       => query.Where(m => m.MatchStatus is MatchStatus.AutoMatched
                                                              or MatchStatus.ManualMatched),
            "Action Needed" => query.Where(m => m.MatchStatus == MatchStatus.PendingSelection),
            "No Match"      => query.Where(m => m.MatchStatus == MatchStatus.NoResults),
            "Unmatched"     => query.Where(m => m.MatchStatus == MatchStatus.ManuallyUnmatched),
            _               => query
        };

        query = SelectedSort switch
        {
            "Title A–Z"  => query.OrderBy(m => m.Title),
            "Title Z–A"  => query.OrderByDescending(m => m.Title),
            "Year ↑"     => query.OrderBy(m => m.Year ?? 0),
            "Year ↓"     => query.OrderByDescending(m => m.Year ?? 0),
            "Rating ↓"   => query.OrderByDescending(m => m.TmdbRating ?? 0),
            "Rating ↑"   => query.OrderBy(m => m.TmdbRating ?? 0),
            "Runtime ↓"  => query.OrderByDescending(m => m.RuntimeMinutes ?? 0),
            _            => query.OrderBy(m => m.Title)
        };

        Movies = new ObservableCollection<MovieListItemViewModel>(query);
        FilteredCount = Movies.Count;
    }

    [RelayCommand]
    private void OpenMovie(MovieListItemViewModel item) =>
        navigation.NavigateTo<MovieDetailViewModel>(item.Id);

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        SelectedGenre = "All";
        SelectedMatchFilter = "All";
    }

    [RelayCommand]
    private void ClearMatchFilter() => SelectedMatchFilter = "All";
}