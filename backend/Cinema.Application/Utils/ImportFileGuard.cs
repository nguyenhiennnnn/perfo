using System.IO.Compression;
using System.Text;
using Cinema.Application.Interfaces;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;

namespace Cinema.Application.Utils;

public sealed record ImportFileInput(
    string? FileName,
    string? ContentType,
    long Length,
    Stream Content
) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}

public sealed record ImportUpload(MemoryStream Content, ImportFileFormat Format, string FileName)
    : IDisposable
{
    public void Dispose() => Content.Dispose();
}

public static class ImportFileGuard
{
    private const int TextProbeBytes = 8192;
    private const int ReadChunkBytes = 81920;
    private const string ContentTypesEntry = "[Content_Types].xml";
    private const string WorkbookEntry = "xl/workbook.xml";
    private const string MacroContentTypeMarker = "macroEnabled";
    private const string MacroExtension = ".xlsm";

    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];

    private static readonly string[] MacroEntryMarkers =
    [
        "xl/vbaproject.bin",
        "xl/macrosheets/",
        "xl/embeddings/",
    ];

    private static readonly HashSet<string> XlsxContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/octet-stream",
    };

    private static readonly HashSet<string> CsvContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/csv",
        "application/csv",
        "text/plain",
        "application/vnd.ms-excel",
        "application/octet-stream",
    };

    private static readonly byte[][] BinarySignatures =
    [
        [0x50, 0x4B],
        [0xD0, 0xCF, 0x11, 0xE0],
        [0x25, 0x50, 0x44, 0x46],
        [0x4D, 0x5A],
        [0x7F, 0x45, 0x4C, 0x46],
        [0x89, 0x50, 0x4E, 0x47],
        [0xFF, 0xD8, 0xFF],
        [0x47, 0x49, 0x46, 0x38],
        [0x52, 0x61, 0x72, 0x21],
        [0x1F, 0x8B],
    ];

    public static async Task<ImportUpload> ReadAsync(
        ImportFileInput input,
        CancellationToken cancellationToken
    )
    {
        var format = ResolveFormat(input.FileName);
        EnsureDeclaredLength(input.Length);
        EnsureContentType(format, input.ContentType);

        var buffer = new MemoryStream();

        try
        {
            var chunk = new byte[ReadChunkBytes];
            int read;
            while (
                (read = await input.Content.ReadAsync(chunk.AsMemory(), cancellationToken)) > 0
            )
            {
                if (buffer.Length + read > ImportLimits.MaxFileBytes)
                    throw TooLarge();

                buffer.Write(chunk, 0, read);
            }

            if (buffer.Length == 0)
                throw Invalid(Messages.Invalid_FileEmpty);

            if (format == ImportFileFormat.Xlsx)
                EnsureWorkbook(buffer);
            else
                EnsureText(buffer);

            buffer.Position = 0;
            return new ImportUpload(buffer, format, SafeFileName(input.FileName));
        }
        catch
        {
            buffer.Dispose();
            throw;
        }
    }

    private static ImportFileFormat ResolveFormat(string? fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();

        if (extension == MacroExtension)
            throw Invalid(Messages.Invalid_ImportMacroNotAllowed);

        return extension switch
        {
            ".xlsx" => ImportFileFormat.Xlsx,
            ".csv" => ImportFileFormat.Csv,
            _ => throw Invalid(string.Format(Messages.Invalid_FileExtensionNotAllowed, ".xlsx, .csv")),
        };
    }

    private static void EnsureDeclaredLength(long length)
    {
        if (length <= 0)
            throw Invalid(Messages.Invalid_FileEmpty);

        if (length > ImportLimits.MaxFileBytes)
            throw TooLarge();
    }

    private static void EnsureContentType(ImportFileFormat format, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return;

        var mediaType = contentType.Split(';', 2)[0].Trim();
        var allowed = format == ImportFileFormat.Xlsx ? XlsxContentTypes : CsvContentTypes;
        if (!allowed.Contains(mediaType))
            throw Invalid(Messages.Invalid_FileContentTypeNotAllowed);
    }

    private static void EnsureWorkbook(MemoryStream buffer)
    {
        var bytes = buffer.GetBuffer();
        if (buffer.Length < ZipSignature.Length || !StartsWith(bytes, ZipSignature))
            throw Invalid(Messages.Invalid_ImportFileContent);

        buffer.Position = 0;
        try
        {
            using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: true);

            if (archive.Entries.Count > ImportLimits.MaxArchiveEntries)
                throw Invalid(Messages.Invalid_ImportFileContent);

            var hasContentTypes = false;
            var hasWorkbook = false;
            long declaredTotal = 0;

            foreach (var entry in archive.Entries)
            {
                declaredTotal += entry.Length;
                if (declaredTotal > ImportLimits.MaxUncompressedBytes)
                    throw Invalid(Messages.Invalid_ImportFileExpandedTooLarge);

                if (IsSuspiciousRatio(entry))
                    throw Invalid(Messages.Invalid_ImportFileExpandedTooLarge);

                if (IsMacroEntry(entry.FullName))
                    throw Invalid(Messages.Invalid_ImportMacroNotAllowed);

                if (entry.FullName == ContentTypesEntry)
                    hasContentTypes = true;
                else if (entry.FullName == WorkbookEntry)
                    hasWorkbook = true;
            }

            if (!hasContentTypes || !hasWorkbook)
                throw Invalid(Messages.Invalid_ImportFileContent);

            MeasureActualSize(archive);
        }
        catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException)
        {
            throw Invalid(Messages.Invalid_ImportFileContent);
        }
    }

    private static void MeasureActualSize(ZipArchive archive)
    {
        var chunk = new byte[ReadChunkBytes];
        long actualTotal = 0;

        foreach (var entry in archive.Entries)
        {
            if (entry.Length == 0)
                continue;

            var isContentTypes = entry.FullName == ContentTypesEntry;
            if (isContentTypes && entry.Length > ImportLimits.MaxContentTypesBytes)
                throw Invalid(Messages.Invalid_ImportFileContent);

            using var stream = entry.Open();
            using var contentTypes = isContentTypes ? new MemoryStream() : null;

            long entryTotal = 0;
            int read;
            while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
            {
                entryTotal += read;
                actualTotal += read;

                if (entryTotal > entry.Length || actualTotal > ImportLimits.MaxUncompressedBytes)
                    throw Invalid(Messages.Invalid_ImportFileExpandedTooLarge);

                contentTypes?.Write(chunk, 0, read);
            }

            if (entryTotal != entry.Length)
                throw Invalid(Messages.Invalid_ImportFileContent);

            if (
                contentTypes is not null
                && Encoding
                    .UTF8.GetString(contentTypes.GetBuffer(), 0, (int)contentTypes.Length)
                    .Contains(MacroContentTypeMarker, StringComparison.OrdinalIgnoreCase)
            )
                throw Invalid(Messages.Invalid_ImportMacroNotAllowed);
        }
    }

    private static bool IsSuspiciousRatio(ZipArchiveEntry entry) =>
        entry.Length > ImportLimits.RatioCheckMinBytes
        && (
            entry.CompressedLength <= 0
            || entry.Length / entry.CompressedLength > ImportLimits.MaxCompressionRatio
        );

    private static bool IsMacroEntry(string fullName)
    {
        var name = fullName.Replace('\\', '/');
        return MacroEntryMarkers.Any(marker =>
            name.StartsWith(marker, StringComparison.OrdinalIgnoreCase)
        );
    }

    private static void EnsureText(MemoryStream buffer)
    {
        var bytes = buffer.GetBuffer();
        var probe = (int)Math.Min(buffer.Length, TextProbeBytes);

        if (BinarySignatures.Any(signature => StartsWith(bytes, signature, probe)))
            throw Invalid(Messages.Invalid_ImportFileContent);

        if (Array.IndexOf(bytes, (byte)0, 0, probe) >= 0)
            throw Invalid(Messages.Invalid_ImportFileContent);
    }

    private static string SafeFileName(string? fileName)
    {
        var name = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/'));
        var cleaned = new string(name.Where(ch => !char.IsControl(ch)).ToArray()).Trim();
        if (cleaned.Length == 0)
            return "import";

        return cleaned.Length > ImportLimits.MaxFileNameLength
            ? cleaned[..ImportLimits.MaxFileNameLength]
            : cleaned;
    }

    private static bool StartsWith(byte[] bytes, byte[] signature, int? available = null)
    {
        var length = available ?? bytes.Length;
        if (length < signature.Length)
            return false;

        for (var i = 0; i < signature.Length; i++)
        {
            if (bytes[i] != signature[i])
                return false;
        }

        return true;
    }

    private static ValidationAppException TooLarge() =>
        Invalid(
            string.Format(Messages.Invalid_FileTooLarge, ImportLimits.MaxFileBytes / (1024 * 1024))
        );

    private static ValidationAppException Invalid(string message) =>
        new(message, new Dictionary<string, string[]> { ["file"] = [message] });
}
