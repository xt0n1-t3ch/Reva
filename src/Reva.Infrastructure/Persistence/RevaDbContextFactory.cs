using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reva.Infrastructure.Persistence;

// Design-time factory so `dotnet ef migrations` can build the model without booting the web host.
public sealed class RevaDbContextFactory : IDesignTimeDbContextFactory<RevaDbContext>
{
    public RevaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RevaDbContext>()
            .UseSqlite(RevaDatabaseProviders.DefaultSqliteConnection)
            .Options;

        return new RevaDbContext(options);
    }
}
