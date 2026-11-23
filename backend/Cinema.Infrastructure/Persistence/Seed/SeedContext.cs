using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed;

internal sealed record SeedContext(AppDbContext Db, ILogger Logger, bool SeedDemoScenarios);
