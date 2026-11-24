using Cinema.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cinema.Application.Services.Reports.Finance;

public static class ReportBlockRegistration
{
    public static IServiceCollection AddReportBlock<TBlock>(this IServiceCollection services)
        where TBlock : class, IReportBlock
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IReportBlock, TBlock>());
        return services;
    }
}
