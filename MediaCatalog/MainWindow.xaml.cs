using System.Diagnostics;
using System.Windows;
using VidBrary.Services.Navigation;
using VidBrary.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace VidBrary;

public partial class MainWindow : Window
{
    public MainViewModel MainViewModel { get; }
    public ScanViewModel ScanViewModel { get; }
    public ProfilesViewModel ProfilesViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        MainViewModel     = App.Services.GetRequiredService<MainViewModel>();
        ScanViewModel     = App.Services.GetRequiredService<ScanViewModel>();
        ProfilesViewModel = App.Services.GetRequiredService<ProfilesViewModel>();

        DataContext = this;

        var nav = App.Services.GetRequiredService<INavigationService>() as NavigationService;
        nav?.SetFrame(MainFrame);

        MainViewModel.Initialize();
        MainViewModel.NavigateHomeCommand.Execute(null);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Close();
}