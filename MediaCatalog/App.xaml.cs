using System.IO;
using System.Windows;
using MediaCatalog.Data;
using MediaCatalog.Services.MediaInfo;
using MediaCatalog.Services.Navigation;
using MediaCatalog.Services.Scanner;
using MediaCatalog.Services.Settings;
using MediaCatalog.Services.Theme;
using MediaCatalog.Services.Tmdb;
using MediaCatalog.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MediaCatalog;

public partial class App : Application
{
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "NasCastr");

    public static IServiceProvider Services { get; private set; } = null!;
    private readonly IHost _host;

    public App()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(
                Path.Combine(DataDirectory, "logs", "app_.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(ConfigureServices)
            .Build();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var dbPath = Path.Combine(DataDirectory, "catalog.db");
        Directory.CreateDirectory(DataDirectory);

        services.AddDbContext<MediaCatalogDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<INavigationService, NavigationService>();

        services.AddScoped<IScannerService, ScannerService>();
        services.AddScoped<ITmdbService, TmdbService>();
        services.AddScoped<IMediaInfoService, MediaInfoService>();
        services.AddSingleton<ScanViewModel>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<MoviesViewModel>();
        services.AddTransient<MovieDetailViewModel>();
        services.AddTransient<TvShowsViewModel>();
        services.AddTransient<TvShowDetailViewModel>();
        services.AddTransient<CollectionsViewModel>();
        services.AddTransient<CollectionDetailViewModel>();
        services.AddTransient<PeopleViewModel>();
        services.AddTransient<PersonDetailViewModel>();
        services.AddTransient<TagsViewModel>();
        services.AddSingleton<ProfilesViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<MyListViewModel>();

        services.AddTransient<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        Services = _host.Services;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MediaCatalogDbContext>();
        await db.Database.MigrateAsync();

        // Seed a default profile if none exist
        if (!db.Profiles.Any())
        {
            db.Profiles.Add(new Models.UserProfile { Name = "Default" });
            await db.SaveChangesAsync();
        }

        var settings = Services.GetRequiredService<ISettingsService>();
        await settings.LoadAsync();

        // Apply the saved theme immediately so the window opens with the correct colors
        ThemeService.Apply(settings.Current);

        // Load profiles into the singleton so the sidebar can display the active one
        await Services.GetRequiredService<ProfilesViewModel>().LoadAsync();

        // Auto-scan on launch if configured
        if (settings.Current.ScanOnLaunch)
            _ = Services.GetRequiredService<ScanViewModel>().ScanCommand.ExecuteAsync(null);

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
