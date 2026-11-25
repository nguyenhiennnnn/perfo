using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Import;
using Cinema.Application.DTOs.Rooms;
using Cinema.Application.Utils;

namespace Cinema.Application.Interfaces.Bookings;

public interface ISeatLayoutImportService
{
    Task<ExportFileDto> GetTemplateAsync(int roomId, CancellationToken cancellationToken);

    Task<ImportPreviewResponse> PreviewAsync(
        int roomId,
        ActorContext actor,
        ImportFileInput file,
        CancellationToken cancellationToken
    );

    Task<UpdateSeatLayoutResponse> ApplyAsync(
        int roomId,
        ActorContext actor,
        ImportApplyRequest request,
        CancellationToken cancellationToken
    );

    Task<ExportFileDto> GetErrorFileAsync(
        int roomId,
        int userId,
        string previewId,
        CancellationToken cancellationToken
    );

    Task<ExportFileDto> ExportAsync(
        int roomId,
        ActorContext actor,
        ExportFormat format,
        CancellationToken cancellationToken
    );
}
