using System.Windows.Controls;
using MediaCatalog.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MediaCatalog.Views.Pages;

public partial class CollectionDetailPage : Page
{
    public CollectionDetailPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<CollectionDetailViewModel>();
    }
}