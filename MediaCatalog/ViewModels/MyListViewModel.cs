using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VidBrary.Data;
using VidBrary.Models;
using VidBrary.Services.Navigation;
using VidBrary.Services.Settings;
using VidBrary.ViewModels;
using VidBrary.ViewModels.Base;
using Microsoft.EntityFrameworkCore;

public partial class MyListViewModel(
    VidBraryDbContext db,
    ISettingsService settings,
    INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<MyListItemViewModel> _items = [];

    public override async Task OnNavigatedToAsync(object? parameter = null) =>
        await LoadAsync();

    private async Task LoadAsync()
    {
        IsBusy = true;
        var profileId = settings.Current.ActiveProfileId;

        var raw = await db.MyList
            .Where(m => m.UserProfileId == profileId)
            .Include(m => m.Movie)
            .Include(m => m.TvShow)
            .OrderByDescending(m => m.AddedAt)
            .AsNoTracking()
            .ToListAsync();

        Items = new ObservableCollection<MyListItemViewModel>(
            raw.Select(r => new MyListItemViewModel(r)));

        IsBusy = false;
    }

    [RelayCommand]
    private void OpenItem(MyListItemViewModel item)
    {
        if (item.MovieId.HasValue)
            navigation.NavigateTo<MovieDetailViewModel>(item.MovieId.Value);
        else if (item.TvShowId.HasValue)
            navigation.NavigateTo<TvShowDetailViewModel>(item.TvShowId.Value);
    }

    [RelayCommand]
    private async Task RemoveItemAsync(MyListItemViewModel item)
    {
        var entity = await db.MyList.FindAsync(item.Id);
        if (entity is not null)
        {
            db.MyList.Remove(entity);
            await db.SaveChangesAsync();
            Items.Remove(item);
        }
    }
}

public class MyListItemViewModel
{
    public int Id { get; }
    public int? MovieId { get; }
    public int? TvShowId { get; }
    public string Title { get; }
    public string? PosterPath { get; }
    public string TypeLabel { get; }
    public string AddedDisplay { get; }

    public MyListItemViewModel(MyListItem item)
    {
        Id           = item.Id;
        AddedDisplay = item.AddedAt.ToString("MMM d, yyyy");
        if (item.Movie is not null)
        {
            MovieId    = item.Movie.Id;
            Title      = item.Movie.Title ?? item.Movie.FileName;
            PosterPath = item.Movie.PosterPath;
            TypeLabel  = "🎬 Movie";
        }
        else if (item.TvShow is not null)
        {
            TvShowId   = item.TvShow.Id;
            Title      = item.TvShow.Title ?? item.TvShow.FolderName;
            PosterPath = item.TvShow.PosterPath;
            TypeLabel  = "📺 TV Show";
        }
        else
        {
            Title     = "Unknown";
            TypeLabel = string.Empty;
        }
    }
}