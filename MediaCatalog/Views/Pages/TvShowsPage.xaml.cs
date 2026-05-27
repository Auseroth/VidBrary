using System.Windows.Controls;
using VidBrary.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace VidBrary.Views.Pages;

public partial class TvShowsPage : Page
{
    public TvShowsPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<TvShowsViewModel>();
    }
}