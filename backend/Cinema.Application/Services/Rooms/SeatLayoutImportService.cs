using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Import;
using Cinema.Application.DTOs.Rooms;
using Cinema.Application.Interfaces.Bookings;
using Cinema.Application.Interfaces.Import;
using Cinema.Application.Interfaces.ImportExport;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Rooms;
using Cinema.Application.Services.Import;
using Cinema.Application.Services.ImportExport;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Rooms;

public class SeatLayoutImportService(
    IImportFileReader fileReader,
    IAppDbContext db,
    IRoomService roomService,
    IExportWriter exportWriter,
    IImportSessionStore sessions,
    IExportJobService exportJobService,
    IImportExportJobTracker jobTracker,
    IImportExportConcurrencyGuard concurrencyGuard,
    ILogger<SeatLayoutImportService> logger
) : ISeatLayoutImportService
{
    public async Task<ExportFileDto> GetTemplateAsync(
        int roomId,
        CancellationToken cancellationToken
    )
    {
        var processor = CreateProcessor(roomId);
        var content = await processor.BuildTemplateAsync(cancellationToken);
        return new ExportFileDto(
            content,
            ExportFormats.ContentType(ExportFormat.Xlsx),
            ExportFileName.Create(
                $"{processor.Module}-room-{roomId}-import-template",
                ExportFormats.Extension(ExportFormat.Xlsx)
            )
        );
    }

    public async Task<ImportPreviewResponse> PreviewAsync(
        int roomId,
        ActorContext actor,
        ImportFileInput file,
        CancellationToken cancellationToken
    )
    {
        var processor = CreateProcessor(roomId);
        using var upload = await ReadUploadAsync(actor, processor.Module, file, cancellationToken);

        var parsed = await processor.ParseAsync(upload.Content, upload.Format, cancellationToken);
        if (parsed.Rows.Count == 0)
            throw new ValidationAppException(
                Messages.Invalid_ImportNoDataRows,
                new Dictionary<string, string[]> { ["file"] = [Messages.Invalid_ImportNoDataRows] }
            );

        var context = await processor.CreateContextAsync(parsed.Rows, cancellationToken);
        var validated = parsed.Rows.Select(row => processor.ValidateRow(row, context)).ToList();

        var session = sessions.Create(
            actor.UserId,
            upload.FileName,
            upload.Format,
            parsed.Headers,
            validated
        );
        var validCount = validated.Count(row => row.IsValid);

        ImportAuditLog.Previewed(
            logger,
            actor,
            processor.Module,
            upload.FileName,
            upload.Content.Length,
            validated.Count,
            validCount
        );

        return new ImportPreviewResponse(
            session.PreviewId,
            validated.Count,
            validCount,
            validated.Count - validCount,
            validated
                .Select(row => new ImportPreviewRow(
                    row.RowNumber,
                    row.Values,
                    row.Errors.ToList()
                ))
                .ToList(),
            processor.BuildWarnings(validated, context)
        );
    }

    public async Task<UpdateSeatLayoutResponse> ApplyAsync(
        int roomId,
        ActorContext actor,
        ImportApplyRequest request,
        CancellationToken cancellationToken
    )
    {
        var processor = CreateProcessor(roomId);
        var session = GetSession(roomId, actor.UserId, request.PreviewId);

        var invalidRows = session.Rows.Where(row => !row.IsValid).ToList();
        if (invalidRows.Count > 0 && !request.SkipInvalidRows)
            throw new BusinessRuleException(
                string.Format(Messages.Rule_ImportHasInvalidRows, invalidRows.Count)
            );

        var validRows = session.Rows.Where(row => row.IsValid).ToList();
        if (validRows.Count == 0)
            throw new BusinessRuleException(Messages.Rule_ImportNoValidRows);

        var parameters = ImportExportJson.Serialize(
            new
            {
                roomId,
                skipInvalidRows = request.SkipInvalidRows,
                totalRows = session.Rows.Count,
                validRows = validRows.Count,
                invalidRows = invalidRows.Count,
            }
        );
        var formatName = session.Format.ToString().ToLowerInvariant();

        using var lease = await concurrencyGuard.AcquireAsync(actor.UserId, cancellationToken);

        if (!session.TryBeginApply())
            throw new ConflictAppException(Messages.Conflict_ImportApplyInProgress);

        try
        {
            var response = await processor.ApplyLayoutAsync(validRows, cancellationToken);
            sessions.Remove(session.PreviewId);

            await jobTracker.RecordCompletedAsync(
                ImportExportJobType.Import,
                processor.Module,
                formatName,
                actor,
                session.FileName,
                session.Rows.Count,
                validRows.Count,
                invalidRows.Count,
                parameters,
                null,
                CancellationToken.None
            );

            return response;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await jobTracker.RecordFailedAsync(
                ImportExportJobType.Import,
                processor.Module,
                formatName,
                actor,
                session.FileName,
                parameters,
                ImportExportJobErrors.Describe(ex),
                CancellationToken.None
            );
            throw;
        }
        finally
        {
            session.EndApply();
        }
    }

    public async Task<ExportFileDto> GetErrorFileAsync(
        int roomId,
        int userId,
        string previewId,
        CancellationToken cancellationToken
    )
    {
        var processor = CreateProcessor(roomId);
        var session = GetSession(roomId, userId, previewId);
        if (session.Rows.All(row => row.IsValid))
            throw new BusinessRuleException(Messages.Rule_ImportNoErrors);

        var columns = new List<ExportColumn<ImportRow<SeatLayoutImportRow>>>(
            session.Headers.Count + 1
        );
        for (var i = 0; i < session.Headers.Count; i++)
        {
            var index = i;
            columns.Add(
                new ExportColumn<ImportRow<SeatLayoutImportRow>>(
                    session.Headers[index],
                    row => index < row.Cells.Count ? row.Cells[index] : null
                )
            );
        }

        columns.Add(
            new ExportColumn<ImportRow<SeatLayoutImportRow>>(
                Messages.Import_ColErrorReason,
                row =>
                    row.Errors.Count == 0
                        ? null
                        : string.Join(
                            "; ",
                            row.Errors.Select(error => $"{error.Column}: {error.Message}")
                        )
            )
        );

        var content = await exportWriter.WriteXlsxAsync(
            processor.SheetName,
            columns,
            session.Rows,
            null,
            cancellationToken
        );

        return new ExportFileDto(
            content,
            ExportFormats.ContentType(ExportFormat.Xlsx),
            ExportFileName.Create(
                $"{processor.Module}-room-{roomId}-import-errors",
                ExportFormats.Extension(ExportFormat.Xlsx)
            )
        );
    }

    public Task<ExportFileDto> ExportAsync(
        int roomId,
        ActorContext actor,
        ExportFormat format,
        CancellationToken cancellationToken
    ) =>
        exportJobService.RunTrackedAsync(
            actor,
            ImportExportModules.SeatLayout,
            format,
            new { roomId },
            (_, counter) => BuildExportAsync(roomId, format, counter, cancellationToken),
            cancellationToken
        );

    private async Task<ExportFileDto> BuildExportAsync(
        int roomId,
        ExportFormat format,
        IJobProgressReporter counter,
        CancellationToken cancellationToken
    )
    {
        if (!await db.CinemaRooms.AnyAsync(r => r.RoomId == roomId, cancellationToken))
            throw new NotFoundException(Messages.NotFound_Room);

        var seats = (
            await db
                .Seats.AsNoTracking()
                .Where(s => s.RoomId == roomId)
                .Select(s => new
                {
                    s.SeatRow,
                    s.SeatNumber,
                    SeatTypeName = s.SeatType != null ? s.SeatType.Name : null,
                    s.IsActive,
                    s.IsAisle,
                    s.CoupleId,
                })
                .ToListAsync(cancellationToken)
        )
            .OrderBy(s => s.SeatRow.Length)
            .ThenBy(s => s.SeatRow, StringComparer.Ordinal)
            .ThenBy(s => s.SeatNumber)
            .ToList();

        var coupleCodes = new Dictionary<Guid, string>();
        var rows = new List<SeatLayoutExportRow>(seats.Count);
        foreach (var seat in seats)
        {
            string? coupleGroup = null;
            if (seat.CoupleId is { } coupleId)
            {
                if (!coupleCodes.TryGetValue(coupleId, out coupleGroup))
                {
                    coupleGroup = $"C{coupleCodes.Count + 1}";
                    coupleCodes[coupleId] = coupleGroup;
                }
            }

            rows.Add(
                new SeatLayoutExportRow(
                    seat.SeatRow,
                    seat.SeatNumber,
                    seat.SeatTypeName,
                    seat.IsActive,
                    seat.IsAisle,
                    coupleGroup
                )
            );
        }

        await counter.ReportAsync(rows.Count, cancellationToken);

        var columns = new List<ExportColumn<SeatLayoutExportRow>>
        {
            new(SeatLayoutImportColumns.SeatRow, r => r.SeatRow),
            new(SeatLayoutImportColumns.SeatNumber, r => r.SeatNumber, ExportColumnType.Number),
            new(SeatLayoutImportColumns.SeatType, r => r.SeatType),
            new(SeatLayoutImportColumns.IsActive, r => YesNo(r.IsActive)),
            new(SeatLayoutImportColumns.IsAisle, r => YesNo(r.IsAisle)),
            new(SeatLayoutImportColumns.CoupleGroup, r => r.CoupleGroup),
        };

        var content =
            format == ExportFormat.Csv
                ? await exportWriter.WriteCsvAsync(columns, rows, cancellationToken)
                : await exportWriter.WriteXlsxAsync(
                    Messages.Import_SheetSeatLayout,
                    columns,
                    rows,
                    null,
                    cancellationToken
                );

        return new ExportFileDto(
            content,
            ExportFormats.ContentType(format),
            ExportFileName.Create(
                $"{ImportExportModules.SeatLayout}-room-{roomId}",
                ExportFormats.Extension(format)
            )
        );
    }

    private async Task<ImportUpload> ReadUploadAsync(
        ActorContext actor,
        string module,
        ImportFileInput file,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await ImportFileGuard.ReadAsync(file, cancellationToken);
        }
        catch (ValidationAppException ex)
        {
            ImportAuditLog.Rejected(logger, actor, module, file.FileName, ex.Message);
            throw;
        }
    }

    private SeatLayoutImportProcessor CreateProcessor(int roomId) =>
        new(roomId, fileReader, db, roomService, exportWriter);

    private ImportSession<SeatLayoutImportRow> GetSession(
        int roomId,
        int userId,
        string previewId
    )
    {
        var session = sessions.Get<SeatLayoutImportRow>(previewId, userId);
        if (session.Rows.Count == 0 || session.Rows.Any(row => row.Data.RoomId != roomId))
            throw new NotFoundException(Messages.NotFound_ImportPreview);

        return session;
    }

    private static string YesNo(bool value) => value ? Messages.Import_Yes : Messages.Import_No;

    private sealed record SeatLayoutExportRow(
        string SeatRow,
        int SeatNumber,
        string? SeatType,
        bool IsActive,
        bool IsAisle,
        string? CoupleGroup
    );
}
