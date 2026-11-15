using Cinema.Application.DTOs.Common;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.ImportExport;

public static class ImportAuditLog
{
    public static void Previewed(
        ILogger logger,
        ActorContext actor,
        string module,
        string fileName,
        long fileSizeBytes,
        int totalRows,
        int validRows
    ) =>
        logger.LogInformation(
            "ImportAudit Preview Module {Module} UserId {UserId} Role {Role} Ip {IpAddress} UserAgent {UserAgent} "
                + "FileName {FileName} FileSizeBytes {FileSizeBytes} TotalRows {TotalRows} ValidRows {ValidRows}",
            module,
            actor.UserId,
            actor.Role,
            actor.IpAddress,
            actor.UserAgent,
            fileName,
            fileSizeBytes,
            totalRows,
            validRows
        );

    public static void Rejected(
        ILogger logger,
        ActorContext actor,
        string module,
        string? fileName,
        string reason
    ) =>
        logger.LogWarning(
            "ImportAudit UploadRejected Module {Module} UserId {UserId} Role {Role} Ip {IpAddress} UserAgent {UserAgent} "
                + "FileName {FileName} Reason {Reason}",
            module,
            actor.UserId,
            actor.Role,
            actor.IpAddress,
            actor.UserAgent,
            fileName,
            reason
        );
}
