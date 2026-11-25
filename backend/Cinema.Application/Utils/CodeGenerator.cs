using Cinema.Application.Options;

namespace Cinema.Application.Utils;

public static class CodeGenerator
{
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private static string _bookingCodePrefix = BrandOptions.DefaultBookingCodePrefix;

    public static void Configure(string bookingCodePrefix)
    {
        if (!string.IsNullOrWhiteSpace(bookingCodePrefix))
            _bookingCodePrefix = bookingCodePrefix.Trim().ToUpperInvariant();
    }

    public static string GenerateBookingCode()
    {
        var suffix = new string(
            Enumerable.Range(0, 4).Select(_ => Chars[Random.Shared.Next(Chars.Length)]).ToArray()
        );

        return $"{_bookingCodePrefix}{BusinessTime.Today():yyyyMMdd}{suffix}";
    }

    public static string GenerateTicketQrPayload(
        string bookingCode,
        string seatRow,
        int seatNumber
    ) => $"{bookingCode}-{seatRow}{seatNumber}";

    public static string GeneratePaymentTransactionId(string bookingCode) =>
        $"{bookingCode}{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

    public static string GenerateCounterTransactionCode(string orderCode) =>
        $"{orderCode}{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    public static string GenerateGiftCardTransactionCode(string orderCode) =>
        $"{orderCode}{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}G{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    public static string GenerateRefundTransactionCode(string chargeTransactionCode) =>
        $"{chargeTransactionCode}R{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
}
