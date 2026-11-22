namespace Cinema.Application.Utils;

public static class ReferralCodeGenerator
{
    public const int Length = 8;

    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate() =>
        string.Create(
            Length,
            0,
            static (span, _) =>
            {
                for (var i = 0; i < span.Length; i++)
                    span[i] = Chars[Random.Shared.Next(Chars.Length)];
            }
        );
}
