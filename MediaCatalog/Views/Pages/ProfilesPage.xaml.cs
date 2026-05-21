using System.Windows.Controls;
using MediaCatalog.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MediaCatalog.Views.Pages;

public partial class ProfilesPage : Page
{
    public ProfilesPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<ProfilesViewModel>();
    }
}