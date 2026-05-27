using System.Windows.Controls;
using VidBrary.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace VidBrary.Views.Pages;

public partial class PeoplePage : Page
{
    public PeoplePage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<PeopleViewModel>();
    }
}