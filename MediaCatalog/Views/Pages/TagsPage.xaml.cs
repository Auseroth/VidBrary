using System.Windows.Controls;
using VidBrary.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace VidBrary.Views.Pages;

public partial class TagsPage : Page
{
    public TagsPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<TagsViewModel>();
    }
}