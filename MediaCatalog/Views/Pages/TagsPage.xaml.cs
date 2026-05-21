using System.Windows.Controls;
using MediaCatalog.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MediaCatalog.Views.Pages;

public partial class TagsPage : Page
{
    public TagsPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<TagsViewModel>();
    }
}