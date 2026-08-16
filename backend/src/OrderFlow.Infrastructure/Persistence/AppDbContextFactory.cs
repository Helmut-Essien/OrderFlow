using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef</c>. Uses <see cref="NullCurrentUser"/> so query filters do not hide tables.</summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>Builds a context for <c>dotnet ef</c> using <c>DB_CONNECTION</c> or the local Docker defaults.</summary>
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=orderflow_db;Username=orderflow;Password=orderflow_dev";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options, new NullCurrentUser());
    }
}
