using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaCatalog.ViewModels.Base;

/// <summary>
/// Base class for all ViewModels in the application.
/// Provides property change notification and error handling hooks.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Called by the navigation service when this page becomes active.</summary>
    public virtual Task OnNavigatedToAsync(object? parameter = null) => Task.CompletedTask;

    /// <summary>Called by the navigation service when leaving this page.</summary>
    public virtual Task OnNavigatedFromAsync() => Task.CompletedTask;
}