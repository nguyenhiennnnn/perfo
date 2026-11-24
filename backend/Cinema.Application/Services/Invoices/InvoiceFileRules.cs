using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.Invoices;

public static class InvoiceFileRules
{
    public const long MaxBytes = 10 * 1024 * 1024;
    public const string Folder = "invoices";
    public const string ContentType = "application/pdf";

    public static void Ensure(string fileName, long length)
    {
        if (length <= 0)
            throw new ValidationAppException(Messages.Invalid_FileEmpty);
        if (!string.Equals(Path.GetExtension(fileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ValidationAppException(Messages.Invalid_InvoiceFileType);
        if (length > MaxBytes)
            throw new ValidationAppException(
                string.Format(Messages.Invalid_FileTooLarge, MaxBytes / (1024 * 1024))
            );
    }
}
