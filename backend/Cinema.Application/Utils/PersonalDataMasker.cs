namespace Cinema.Application.Utils;

public static class PersonalDataMasker
{
    private const char MaskChar = '*';
    private const int PhoneVisibleHead = 3;
    private const int PhoneVisibleTail = 3;
    private const int EmailVisibleHead = 1;
    private const int EmailMinMaskLength = 3;

    public static string? MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return phone;

        var value = phone.Trim();
        if (value.Length <= PhoneVisibleHead + PhoneVisibleTail)
            return new string(MaskChar, value.Length);

        return value[..PhoneVisibleHead]
            + new string(MaskChar, value.Length - PhoneVisibleHead - PhoneVisibleTail)
            + value[^PhoneVisibleTail..];
    }

    public static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return email;

        var value = email.Trim();
        var at = value.LastIndexOf('@');
        if (at <= 0)
            return new string(MaskChar, value.Length);

        var local = value[..at];
        var domain = value[at..];
        var maskLength = Math.Max(local.Length - EmailVisibleHead, EmailMinMaskLength);
        return local[..EmailVisibleHead] + new string(MaskChar, maskLength) + domain;
    }
}
