using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VidBrary.Data;
using VidBrary.Models;
using VidBrary.ViewModels.Base;
using Microsoft.EntityFrameworkCore;

namespace VidBrary.ViewModels;

public partial class TagsViewModel(VidBraryDbContext db) : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<TagEditorItemViewModel> _tags = [];

    [ObservableProperty] private bool _showCreateDialog;
    [ObservableProperty] private string _newTagName = string.Empty;
    [ObservableProperty] private string _newTagColor = "#0f3460";
    [ObservableProperty] private string? _newTagIcon;
    [ObservableProperty] private string? _createError;

    public IReadOnlyList<string> PresetColors { get; } =
    [
        "#e94560", "#1a73e8", "#7c3aed", "#0f9d58",
        "#f59e0b", "#0f3460", "#6c757d", "#16213e"
    ];

    public IReadOnlyList<string> PresetIcons { get; } =
    [
        "⭐", "❤️", "🔥", "✅", "🎯", "📌", "🏆", "🎬",
        "👁", "🔖", "🎭", "📺", "🌍", "4K", "HDR", "👨‍👩‍👧"
    ];

    public override async Task OnNavigatedToAsync(object? parameter = null) =>
        await LoadAsync();

    private async Task LoadAsync()
    {
        IsBusy = true;
        var tags = await db.Tags.AsNoTracking().ToListAsync();
        Tags = new ObservableCollection<TagEditorItemViewModel>(
            tags.Select(t => new TagEditorItemViewModel
            {
                Id      = t.Id,
                Name    = t.Name,
                Color   = t.ColorHex ?? "#0f3460",
                IconKey = t.IconKey
            }));
        IsBusy = false;
    }

    [RelayCommand] private void SelectColor(string color) => NewTagColor = color;
    [RelayCommand] private void SelectIcon(string icon)   =>
        NewTagIcon = NewTagIcon == icon ? null : icon;   // toggle off if same

    // ── Create ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenCreateDialog()
    {
        NewTagName  = string.Empty;
        NewTagColor = "#0f3460";
        NewTagIcon  = null;
        CreateError = null;
        ShowCreateDialog = true;
    }

    [RelayCommand]
    private void CloseCreateDialog() => ShowCreateDialog = false;

    [RelayCommand]
    private async Task CreateTagAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTagName))
        {
            CreateError = "Tag name is required.";
            return;
        }

        if (await db.Tags.AnyAsync(t => t.Name == NewTagName.Trim()))
        {
            CreateError = "A tag with that name already exists.";
            return;
        }

        db.Tags.Add(new UserTag
        {
            Name     = NewTagName.Trim(),
            ColorHex = NewTagColor,
            IconKey  = string.IsNullOrWhiteSpace(NewTagIcon) ? null : NewTagIcon.Trim()
        });
        await db.SaveChangesAsync();

        ShowCreateDialog = false;
        await LoadAsync();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task DeleteTagAsync(TagEditorItemViewModel tag)
    {
        var entity = await db.Tags.FindAsync(tag.Id);
        if (entity is null) return;

        db.Tags.Remove(entity);
        await db.SaveChangesAsync();
        await LoadAsync();
    }
}

public partial class TagEditorItemViewModel : ObservableObject
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Color { get; init; } = "#0f3460";
    public string? IconKey { get; init; }
    public string Display => IconKey is not null ? $"{IconKey}  {Name}" : Name;
}