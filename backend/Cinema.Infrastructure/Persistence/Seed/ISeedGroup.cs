namespace Cinema.Infrastructure.Persistence.Seed;

internal interface ISeedGroup
{
    string Name { get; }

    Task SeedAsync(SeedContext context, CancellationToken cancellationToken);
}
