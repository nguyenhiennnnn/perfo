using Cinema.Application.Interfaces.Ai;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Import;
using Cinema.Application.Interfaces.ImportExport;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Payments;
using Cinema.Application.Options;
using Cinema.Infrastructure.Ai;
using Cinema.Infrastructure.Auth;
using Cinema.Infrastructure.Email;
using Cinema.Infrastructure.Export;
using Cinema.Infrastructure.FileStorage;
using Cinema.Infrastructure.Import;
using Cinema.Infrastructure.Payments;
using Cinema.Infrastructure.Pdf;
using Cinema.Infrastructure.Persistence;
using Cinema.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace Cinema.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetPostgresConnectionString();

        var env = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
        var isDev = string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);

        services.AddDbContext<AppDbContext>(
            (sp, options) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var conn = config.GetPostgresConnectionString();

                var env = config["ASPNETCORE_ENVIRONMENT"] ?? "Development";
                var isDev = string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);

                options.UseNpgsql(
                    conn,
                    sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                );

                options.AddInterceptors(new AuditLogImmutabilityInterceptor());

                options.ConfigureWarnings(w =>
                    w.Ignore(
                        Microsoft
                            .EntityFrameworkCore
                            .Diagnostics
                            .RelationalEventId
                            .PendingModelChangesWarning
                    )
                );

                if (isDev)
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            }
        );

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IFileStorage, CloudinaryFileStorage>();
        services.AddSingleton<IExportWriter, ExportWriter>();
        services.AddSingleton<IImportFileReader, ImportFileReader>();

        QuestPDF.Settings.License = LicenseType.Community;
        services.AddSingleton<IPdfGenerator, QuestPdfGenerator>();

        services.AddHttpClient("Resend");
        services.AddHttpClient(CloudinaryFileStorage.HttpClientName);
        services.AddScoped<ResendEmailService>();
        services.AddScoped<MailKitEmailService>();
        services.AddScoped<IEmailService, FallbackEmailService>();

        services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
        services.AddHttpClient<IAiModelClient, GeminiAiModelClient>();

        // Mỗi gateway một HttpClient riêng (trước đây cùng dùng tên "IPaymentGateway"), có
        // User-Agent, timeout 30s và không tự follow redirect.
        services.AddGatewayHttpClient<VnPayGateway>();
        services.AddTransient<IPaymentGateway>(sp => sp.GetRequiredService<VnPayGateway>());
        services.AddGatewayHttpClient<MomoGateway>();
        services.AddTransient<IPaymentGateway>(sp => sp.GetRequiredService<MomoGateway>());
        services.AddScoped<IPaymentGateway, StripeGateway>();

        return services;
    }
}
