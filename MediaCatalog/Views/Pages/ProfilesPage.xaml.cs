using System.Windows.Controls;
using VidBrary.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace VidBrary.Views.Pages;

public partial class ProfilesPage : Page
{
    public ProfilesPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<ProfilesViewModel>();
    }
}