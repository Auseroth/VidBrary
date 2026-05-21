using MediaCatalog.Models;
using Microsoft.EntityFrameworkCore;

namespace MediaCatalog.Data;

public class MediaCatalogDbContext(DbContextOptions<MediaCatalogDbContext> options) : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<TvShow> TvShows => Set<TvShow>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<MediaCollection> Collections => Set<MediaCollection>();
    public DbSet<UserTag> Tags => Set<UserTag>();
    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<WatchHistory> WatchHistory => Set<WatchHistory>();
    public DbSet<UserRating> Ratings => Set<UserRating>();
    public DbSet<TmdbCandidate> TmdbCandidates => Set<TmdbCandidate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite keys for join tables
        modelBuilder.Entity<MovieGenre>().HasKey(e => new { e.MovieId, e.GenreId });
        modelBuilder.Entity<TvShowGenre>().HasKey(e => new { e.TvShowId, e.GenreId });
        modelBuilder.Entity<MovieTag>().HasKey(e => new { e.MovieId, e.UserTagId });
        modelBuilder.Entity<TvShowTag>().HasKey(e => new { e.TvShowId, e.UserTagId });
        modelBuilder.Entity<MovieCast>().HasKey(e => new { e.MovieId, e.PersonId });
        modelBuilder.Entity<MovieCrew>().HasKey(e => new { e.MovieId, e.PersonId, e.Job });
        modelBuilder.Entity<TvShowCast>().HasKey(e => new { e.TvShowId, e.PersonId });

        // Prevent cascade delete cycles
        modelBuilder.Entity<WatchHistory>()
            .HasOne(w => w.Movie).WithMany(m => m.WatchHistory)
            .HasForeignKey(w => w.MovieId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<WatchHistory>()
            .HasOne(w => w.Episode).WithMany(e => e.WatchHistory)
            .HasForeignKey(w => w.EpisodeId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<UserRating>()
            .HasOne(r => r.Movie).WithMany(m => m.Ratings)
            .HasForeignKey(r => r.MovieId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<UserRating>()
            .HasOne(r => r.Episode).WithMany(e => e.Ratings)
            .HasForeignKey(r => r.EpisodeId).OnDelete(DeleteBehavior.SetNull);
    }
}