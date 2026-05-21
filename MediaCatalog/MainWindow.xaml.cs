using System.Windows;
using MediaCatalog.Services.Navigation;
using MediaCatalog.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MediaCatalog;

public partial class MainWindow : Window
{
    public MainViewModel MainViewModel { get; }
    public ScanViewModel ScanViewModel { get; }
    public ProfilesViewModel ProfilesViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        MainViewModel    = App.Services.GetRequiredService<MainViewModel>();
        ScanViewModel    = App.Services.GetRequiredService<ScanViewModel>();
        ProfilesViewModel = App.Services.GetRequiredService<ProfilesViewModel>();

        DataContext = this;

        // Wire the navigation frame
        var nav = App.Services.GetRequiredService<INavigationService>() as Services.Navigation.NavigationService;
        nav?.SetFrame(MainFrame);

        // Start on Home
        MainViewModel.NavigateHomeCommand.Execute(null);
    }
}