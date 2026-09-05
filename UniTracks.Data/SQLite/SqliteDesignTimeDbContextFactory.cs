using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UniTracks.Data.SQLite;

/// <summary>Design-time factory so the dotnet-ef CLI can build the DbContext.</summary>
public class SqliteDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqliteDBContext>
{
    public SqliteDBContext CreateDbContext(string[] args)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "unittracks-design.db");
        var options = new DbContextOptionsBuilder<SqliteDBContext>()
            .UseSqlite($"Filename={databasePath}")
            .Options;
        return new SqliteDBContext(options);
    }
}
