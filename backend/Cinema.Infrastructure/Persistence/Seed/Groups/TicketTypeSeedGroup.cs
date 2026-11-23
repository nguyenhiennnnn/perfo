namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class TicketTypeSeedGroup : ISeedGroup
{
    public string Name => "ticket-types";

    public Task SeedAsync(SeedContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
