using System.IO;
using VidBrary.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VidBrary.Data;

public class VidBraryDbContextFactory : IDesignTimeDbContextFactory<VidBraryDbContext>
{
    public VidBraryDbContext CreateDbContext(string[] args)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "VidBrary",
            "catalog.db");

        var options = new DbContextOptionsBuilder<VidBraryDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new VidBraryDbContext(options);
    }
}