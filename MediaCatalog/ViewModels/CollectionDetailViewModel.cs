using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VidBrary.Data;
using VidBrary.Services.Navigation;
using VidBrary.ViewModels.Base;
using Microsoft.EntityFrameworkCore;

namespace VidBrary.ViewModels;

public partial class CollectionDetailViewModel(
    VidBraryDbContext db,
    INavigationService navigation) : ViewModelBase
{
    private int _collectionId;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _overview;
    [ObservableProperty] private string? _posterPath;
    [ObservableProperty] private string? _backdropPath;
    [ObservableProperty] private bool _isUserCreated;
    [ObservableProperty] private ObservableCollection<MovieListItemViewModel> _movies = [];

    // ── Edit (user collections only) ──────────────────────────────────────────
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _editName = string.Empty;

    public override async Task OnNavigatedToAsync(object? parameter = null)
    {
        if (parameter is not int id) return;
        _collectionId = id;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;

        var collection = await db.Collections
            .Include(c => c.Movies)
                .ThenInclude(m => m.Genres).ThenInclude(g => g.Genre)
            .Include(c => c.Movies)
                .ThenInclude(m => m.Tags).ThenInclude(t => t.UserTag)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == _collectionId);

        if (collection is null) { IsBusy = false; return; }

        Name          = collection.Name;
        Overview      = collection.Overview;
        PosterPath    = collection.PosterPath;
        BackdropPath  = collection.BackdropPath;
        IsUserCreated = collection.IsUserCreated;

        Movies = new ObservableCollection<MovieListItemViewModel>(
            collection.Movies
                .OrderBy(m => m.Year)
                .Select(MovieListItemViewModel.FromMovie));

        IsBusy = false;
    }

    [RelayCommand]
    private void OpenMovie(MovieListItemViewModel item) =>
        navigation.NavigateTo<MovieDetailViewModel>(item.Id);

    // ── Edit collection name (user-created only) ──────────────────────────────

    [RelayCommand]
    private void StartEdit()
    {
        EditName  = Name;
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName)) return;

        var collection = await db.Collections.FindAsync(_collectionId);
        if (collection is null) return;

        collection.Name = EditName.Trim();
        await db.SaveChangesAsync();

        IsEditing = false;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteCollectionAsync()
    {
        var collection = await db.Collections.FindAsync(_collectionId);
        if (collection is null) return;

        // Unlink movies but don't delete them
        var movies = await db.Movies
            .Where(m => m.CollectionId == _collectionId)
            .ToListAsync();
        foreach (var m in movies) m.CollectionId = null;

        db.Collections.Remove(collection);
        await db.SaveChangesAsync();

        navigation.GoBack();
    }
}