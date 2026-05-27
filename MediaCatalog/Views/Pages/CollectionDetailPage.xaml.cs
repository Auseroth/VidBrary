using System.Windows.Controls;
using VidBrary.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace VidBrary.Views.Pages;

public partial class CollectionDetailPage : Page
{
    public CollectionDetailPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<CollectionDetailViewModel>();
    }
}