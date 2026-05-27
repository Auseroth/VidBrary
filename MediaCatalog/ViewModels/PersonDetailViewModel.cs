using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VidBrary.Data;
using VidBrary.Services.Navigation;
using VidBrary.ViewModels.Base;
using Microsoft.EntityFrameworkCore;

namespace VidBrary.ViewModels;

public partial class PersonDetailViewModel(
    VidBraryDbContext db,
    INavigationService navigation) : ViewModelBase
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _photoPath;
    [ObservableProperty] private string? _biography;
    [ObservableProperty] private string? _birthday;
    [ObservableProperty] private string? _placeOfBirth;
    [ObservableProperty] private ObservableCollection<PersonCreditViewModel> _movieCredits = [];
    [ObservableProperty] private ObservableCollection<PersonCreditViewModel> _showCredits = [];

    public override async Task OnNavigatedToAsync(object? parameter = null)
    {
        if (parameter is not int id) return;
        IsBusy = true;

        var person = await db.People
            .Include(p => p.MovieCast).ThenInclude(c => c.Movie)
            .Include(p => p.MovieCrew).ThenInclude(c => c.Movie)
            .Include(p => p.TvShowCast).ThenInclude(c => c.TvShow)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (person is null) { IsBusy = false; return; }

        Name         = person.Name;
        PhotoPath    = person.ProfilePath;
        Biography    = person.Biography;
        Birthday     = person.Birthday?.ToString("MMMM d, yyyy");
        PlaceOfBirth = person.PlaceOfBirth;

        // Merge cast + crew movie credits, deduplicate by movie
        var movieCredits = person.MovieCast
            .Select(c => new PersonCreditViewModel
            {
                MediaId    = c.MovieId,
                MediaType  = "Movie",
                Title      = c.Movie.Title ?? c.Movie.FileName,
                Year       = c.Movie.Year,
                PosterPath = c.Movie.PosterPath,
                Role       = c.Character ?? "—",
                Job        = "Actor"
            })
            .Concat(person.MovieCrew.Select(c => new PersonCreditViewModel
            {
                MediaId    = c.MovieId,
                MediaType  = "Movie",
                Title      = c.Movie.Title ?? c.Movie.FileName,
                Year       = c.Movie.Year,
                PosterPath = c.Movie.PosterPath,
                Role       = c.Job,
                Job        = c.Job
            }))
            .GroupBy(c => c.MediaId)
            .Select(g => g.First())
            .OrderByDescending(c => c.Year ?? 0);

        MovieCredits = new ObservableCollection<PersonCreditViewModel>(movieCredits);

        var showCredits = person.TvShowCast
            .Select(c => new PersonCreditViewModel
            {
                MediaId    = c.TvShowId,
                MediaType  = "TvShow",
                Title      = c.TvShow.Title ?? c.TvShow.FolderName,
                Year       = c.TvShow.FirstAirYear,
                PosterPath = c.TvShow.PosterPath,
                Role       = c.Character ?? "—",
                Job        = "Actor"
            })
            .OrderByDescending(c => c.Year ?? 0);

        ShowCredits = new ObservableCollection<PersonCreditViewModel>(showCredits);

        IsBusy = false;
    }

    [RelayCommand]
    private void OpenCredit(PersonCreditViewModel credit)
    {
        if (credit.MediaType == "Movie")
            navigation.NavigateTo<MovieDetailViewModel>(credit.MediaId);
        else
            navigation.NavigateTo<TvShowDetailViewModel>(credit.MediaId);
    }
}

public class PersonCreditViewModel
{
    public int MediaId { get; init; }
    public string MediaType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int? Year { get; init; }
    public string? PosterPath { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Job { get; init; } = string.Empty;
    public string YearDisplay => Year?.ToString() ?? "—";
}