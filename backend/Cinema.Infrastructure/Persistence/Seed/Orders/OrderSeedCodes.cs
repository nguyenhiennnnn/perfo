namespace Cinema.Infrastructure.Persistence.Seed.Orders;

internal static class OrderSeedCodes
{
    public const string OnlineConfirmedTier = "DEMOCONF01";
    public const string OnlineConfirmedImax = "DEMOCONF02";
    public const string OnlineExpired = "DEMOEXPR01";
    public const string OnlineCancelled = "DEMOCNCL01";
    public const string ShowtimeCancelled = "DEMOCNCL02";
    public const string PartialRefund = "DEMOREFP01";
    public const string CounterCash = "DEMOCASH01";
    public const string CounterMixed = "DEMOMIXD01";
    public const string CounterComboOnly = "DEMOCMBO01";
    public const string OnlineComboOnly = "DEMOCMBO02";
    public const string CheckedIn = "DEMOCHKN01";
    public const string PendingHold = "DEMOPEND01";
    public const string SplitGateway = "DEMOSPLT01";
    public const string ShowtimeCancelledCashPending = "DEMOCNCL03";
    public const string ShowtimeCancelledCashRefunded = "DEMOCNCL04";

    private static readonly string[] All =
    [
        OnlineConfirmedTier,
        OnlineConfirmedImax,
        OnlineExpired,
        OnlineCancelled,
        ShowtimeCancelled,
        PartialRefund,
        CounterCash,
        CounterMixed,
        CounterComboOnly,
        OnlineComboOnly,
        CheckedIn,
        PendingHold,
        SplitGateway,
        ShowtimeCancelledCashPending,
        ShowtimeCancelledCashRefunded,
    ];

    public static int Sequence(string orderCode) => Array.IndexOf(All, orderCode) + 1;

    public static string GatewayTransactionId(string orderCode, int index = 0) =>
        (14_000_000 + Sequence(orderCode) * 10 + index).ToString();
}

internal static class OrderSeedCombos
{
    public const string Solo = "Combo Solo";
    public const string Couple = "Combo Couple";
    public const string Premium = "Combo Premium";
    public const string Family = "Combo Gia Đình";
    public const string Caramel = "Combo Caramel";
}
