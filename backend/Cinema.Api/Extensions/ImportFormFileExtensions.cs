using Cinema.Application.Utils;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;

namespace Cinema.Api.Extensions;

public static class ImportFormFileExtensions
{
    public static ImportFileInput ToImportInput(this IFormFile? file)
    {
        if (file is null)
            throw new ValidationAppException(
                Messages.Invalid_UploadFileMissing,
                new Dictionary<string, string[]> { ["file"] = [Messages.Invalid_FileEmpty] }
            );

        return new ImportFileInput(
            file.FileName,
            file.ContentType,
            file.Length,
            file.OpenReadStream()
        );
    }
}
