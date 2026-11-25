using System.Security.Cryptography;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Utils;

public static class GiftCardCodes
{
    public const int Length = FieldLengths.GiftCardCode;
    public const int GroupSize = 4;

    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(Length);
        return string.Create(
            Length,
            bytes,
            static (span, source) =>
            {
                for (var i = 0; i < span.Length; i++)
                    span[i] = Alphabet[source[i] & (Alphabet.Length - 1)];
            }
        );
    }

    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var code = new string(
            input.Where(c => c != '-' && !char.IsWhiteSpace(c)).ToArray()
        ).ToUpperInvariant();

        return code.Length == Length && code.All(c => Alphabet.Contains(c)) ? code : null;
    }

    public static string Mask(string code) =>
        Format(
            string.Concat(
                new string('*', Math.Max(0, code.Length - GroupSize)),
                code[^Math.Min(GroupSize, code.Length)..]
            )
        );

    public static string Format(string code) =>
        string.Join(
            '-',
            Enumerable
                .Range(0, (code.Length + GroupSize - 1) / GroupSize)
                .Select(i => code.Substring(i * GroupSize, Math.Min(GroupSize, code.Length - i * GroupSize)))
        );

    public static async Task<string> GenerateUniqueAsync(
        IAppDbContext db,
        int maxAttempts,
        CancellationToken cancellationToken
    )
    {
        for (var attempt = 0; attempt < Math.Max(1, maxAttempts); attempt++)
        {
            var code = Generate();
            if (!await db.GiftCards.AnyAsync(c => c.Code == code, cancellationToken))
                return code;
        }

        throw new BusinessRuleException(Messages.Rule_GiftCardCodeGenerationFailed);
    }
}
