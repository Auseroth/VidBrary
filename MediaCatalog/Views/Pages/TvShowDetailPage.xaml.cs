using System.Windows.Controls;
using VidBrary.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace VidBrary.Views.Pages;

public partial class TvShowDetailPage : Page
{
    public TvShowDetailPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<TvShowDetailViewModel>();
    }
}