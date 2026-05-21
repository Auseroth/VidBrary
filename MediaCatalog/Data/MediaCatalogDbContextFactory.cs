using System.IO;
using MediaCatalog.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MediaCatalog.Data;

public class MediaCatalogDbContextFactory : IDesignTimeDbContextFactory<MediaCatalogDbContext>
{
    public MediaCatalogDbContext CreateDbContext(string[] args)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "NasCastr",
            "catalog.db");

        var options = new DbContextOptionsBuilder<MediaCatalogDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new MediaCatalogDbContext(options);
    }
}