using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VidBrary.Data;
using VidBrary.Models;
using VidBrary.Services.Settings;
using VidBrary.ViewModels.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace VidBrary.ViewModels;

public partial class ProfilesViewModel(
    IServiceScopeFactory scopeFactory,
    ISettingsService settingsService) : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<ProfileItemViewModel> _profiles = [];
    [ObservableProperty] private ProfileItemViewModel? _activeProfile;

    // ── Create dialog ─────────────────────────────────────────────────────────
    [ObservableProperty] private bool _showCreateDialog;
    [ObservableProperty] private string _newProfileName = string.Empty;
    [ObservableProperty] private string? _createError;

    public override async Task OnNavigatedToAsync(object? parameter = null) =>
        await LoadAsync();

    public async Task LoadAsync()
    {
        IsBusy = true;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VidBraryDbContext>();

        var profiles = await db.Profiles.AsNoTracking().ToListAsync();
        var activeId = settingsService.Current.ActiveProfileId;

        Profiles = new ObservableCollection<ProfileItemViewModel>(
            profiles.Select(p => new ProfileItemViewModel
            {
                Id        = p.Id,
                Name      = p.Name,
                IsActive  = p.Id == activeId,
                CreatedAt = p.CreatedAt.ToLocalTime().ToString("MMM d, yyyy")
            }));

        ActiveProfile = Profiles.FirstOrDefault(p => p.IsActive);
        IsBusy = false;
    }

    [RelayCommand]
    private async Task SwitchProfileAsync(ProfileItemViewModel profile)
    {
        settingsService.Current.ActiveProfileId = profile.Id;
        await settingsService.SaveAsync();

        foreach (var p in Profiles) p.IsActive = p.Id == profile.Id;
        ActiveProfile = profile;
    }

    [RelayCommand]
    private void OpenCreateDialog()
    {
        NewProfileName = string.Empty;
        CreateError    = null;
        ShowCreateDialog = true;
    }

    [RelayCommand]
    private void CloseCreateDialog() => ShowCreateDialog = false;

    [RelayCommand]
    private async Task CreateProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProfileName))
        {
            CreateError = "Please enter a profile name.";
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VidBraryDbContext>();

        db.Profiles.Add(new UserProfile { Name = NewProfileName.Trim() });
        await db.SaveChangesAsync();

        ShowCreateDialog = false;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteProfileAsync(ProfileItemViewModel profile)
    {
        if (profile.IsActive) return;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VidBraryDbContext>();

        var entity = await db.Profiles.FindAsync(profile.Id);
        if (entity is null) return;

        db.Profiles.Remove(entity);
        await db.SaveChangesAsync();
        await LoadAsync();
    }
}

public partial class ProfileItemViewModel : ObservableObject
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    [ObservableProperty] private bool _isActive;
}