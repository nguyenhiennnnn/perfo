using System.Globalization;
using System.Net;
using Cinema.Application.Interfaces;
using Cinema.Application.Options;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Cinema.Application.Interfaces.Notifications;


namespace Cinema.Application.Utils;

public static class EmailTemplates
{
    private const string ColorBackground = "#f8f3eb";
    private const string ColorCard = "#fefbf7";
    private const string ColorSecondary = "#ece5dc";
    private const string ColorForeground = "#1e1311";
    private const string ColorMuted = "#6a5a51";
    private const string ColorBorder = "#dad3ca";
    private const string ColorPrimary = "#a50013";
    private const string ColorPrimaryForeground = "#fdfaf4";
    private const string ColorSuccess = "#187c49";
    private const string ColorDestructive = "#d41101";
    private const string ColorWarningForeground = "#211201";

    private const string TintSuccess = "#e7eee6";
    private const string TintDestructive = "#fae4de";
    private const string TintWarning = "#faf0df";

    private const string FontBody = "'Be Vietnam Pro', 'Segoe UI', Helvetica, Arial, sans-serif";
    private const string FontHeading = "'Archivo', 'Arial Narrow', Arial, sans-serif";
    private const string FontSymbol = "'Segoe UI Symbol', 'Apple Symbols', Arial, sans-serif";

    private const string StyleH2 =
        $"font-family:{FontHeading}; font-stretch:100%; font-weight:700; font-size:28px; line-height:1.15; letter-spacing:-0.01em;";
    private const string StyleNumeral =
        $"font-family:{FontHeading}; font-stretch:75%; font-variation-settings:'wdth' 75; font-weight:800; font-variant-numeric:tabular-nums; line-height:1.05;";
    private const string StyleCode =
        $"font-family:{FontHeading}; font-stretch:80%; font-variation-settings:'wdth' 80; font-weight:600; font-variant-numeric:tabular-nums; letter-spacing:0.04em;";

    private const string SymbolCheck = "&#10003;&#xFE0E;";
    private const string SymbolCross = "&#10005;&#xFE0E;";
    private const string SymbolAlert = "!";

    private static readonly NumberFormatInfo MoneyFormat = new() { NumberGroupSeparator = "." };

    private sealed record Tone(string Css, string Bg, string Mark, string MarkCss);

    private static readonly Tone Success = new("ok", TintSuccess, ColorSuccess, "mk-ok");
    private static readonly Tone Danger = new("err", TintDestructive, ColorDestructive, "mk-err");
    private static readonly Tone Warning = new(
        "warn",
        TintWarning,
        ColorWarningForeground,
        "mk-warn"
    );
    private static readonly Tone Neutral = new("neutral", ColorSecondary, ColorForeground, "tx");

    private static string Enc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string MoneyDigits(decimal amount) => amount.ToString("N0", MoneyFormat);

    private static string Money(decimal amount) => $"{MoneyDigits(amount)} {Messages.Common_CurrencyVnd}";

    private static string Layout(
        BrandOptions brand,
        string preheader,
        string bodyHtml,
        string ticketHtml = ""
    )
    {
        var year = BusinessTime.Today().Year;
        var name = Enc(brand.DisplayName);
        var support = string.IsNullOrWhiteSpace(brand.SupportEmail)
            ? string.Empty
            : string.Format(
                Messages.Email_SupportContact,
                $"<a href=\"mailto:{Enc(brand.SupportEmail)}\" class=\"tx\" style=\"color:{ColorForeground}; text-decoration:underline;\">{Enc(brand.SupportEmail)}</a>"
            );
        var autoSent = string.Format(Messages.Email_FooterAutoSent, name);
        var copyright = string.Format(Messages.Email_FooterCopyright, year, name);

        return $$"""
            <!DOCTYPE html>
            <html lang="vi">
              <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <meta name="color-scheme" content="light dark" />
                <meta name="supported-color-schemes" content="light dark" />
                <title>{{name}}</title>
                <!--[if !mso]><!-->
                <link href="https://fonts.googleapis.com/css2?family=Archivo:wdth,wght@75..100,500..800&family=Be+Vietnam+Pro:wght@400;500;600&display=swap" rel="stylesheet" />
                <!--<![endif]-->
                <style>
                  @media (prefers-color-scheme: dark) {
                    .bg-page { background-color:#0a0404 !important; }
                    .bg-card { background-color:#1a0e0c !important; }
                    .bg-sec { background-color:#221816 !important; }
                    .bg-pri { background-color:#d4212d !important; }
                    .tx { color:#f2eee7 !important; }
                    .tx-mut { color:#a89c92 !important; }
                    .bd { border-color:#322523 !important; }
                    .tone-ok { background-color:#1e2c1e !important; }
                    .tone-err { background-color:#441611 !important; }
                    .tone-warn { background-color:#432a11 !important; }
                    .tone-neutral { background-color:#221816 !important; }
                    .mk-ok { color:#2fa465 !important; }
                    .mk-err { color:#ed3726 !important; }
                    .mk-warn { color:#e69825 !important; }
                  }
                </style>
              </head>
              <body class="bg-page tx" style="margin:0; padding:0; background-color:{{ColorBackground}}; font-family:{{FontBody}}; font-size:15px; line-height:1.55; color:{{ColorForeground}};">
                <span style="display:none; max-height:0; overflow:hidden; opacity:0; mso-hide:all;">{{Enc(
                preheader
            )}}</span>
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" class="bg-page" style="background-color:{{ColorBackground}};">
                  <tr>
                    <td align="center" style="padding:32px 16px;">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;">
                        <tr>
                          <td class="tx" style="padding:0 0 16px 0; {{StyleNumeral}} font-size:24px; color:{{ColorForeground}};">{{name}}<span class="bg-pri" style="display:inline-block; width:7px; height:7px; border-radius:50%; background-color:{{ColorPrimary}}; margin-left:3px;"></span></td>
                        </tr>
                        <tr>
                          <td>
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                              <tr>
                                <td class="bg-card" style="background-color:{{ColorCard}}; padding:32px 28px;">
                                  {{bodyHtml}}
                                </td>
                              </tr>
                              {{ticketHtml}}
                            </table>
                          </td>
                        </tr>
                        <tr>
                          <td class="tx-mut" style="padding:24px 0 0 0; font-size:13px; line-height:1.5; font-weight:600; color:{{ColorMuted}};">
                            {{Enc(brand.Tagline)}}
                          </td>
                        </tr>
                        <tr>
                          <td class="tx-mut" style="padding:8px 0 0 0; font-size:13px; line-height:1.5; color:{{ColorMuted}};">
                            {{autoSent}}{{support}}<br />
                            {{copyright}}
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </body>
            </html>
            """;
    }

    private static string Heading(string text) =>
        $"""<h1 class="tx" style="margin:0 0 16px 0; {StyleH2} color:{ColorForeground};">{text}</h1>""";

    private static string Paragraph(string html) =>
        $"""<p class="tx" style="margin:0 0 16px 0; font-size:15px; line-height:1.55; color:{ColorForeground};">{html}</p>""";

    private static string MutedParagraph(string html) =>
        $"""<p class="tx-mut" style="margin:0; font-size:13px; line-height:1.5; color:{ColorMuted};">{html}</p>""";

    private static string Button(string label, string url) =>
        $"""
            <table role="presentation" cellpadding="0" cellspacing="0" style="margin:8px 0 20px 0;">
              <tr>
                <td class="bg-pri" style="background-color:{ColorPrimary};">
                  <a href="{Enc(
                url
            )}" target="_blank" style="display:inline-block; padding:13px 24px; font-family:{FontBody}; font-size:15px; line-height:18px; font-weight:500; color:{ColorPrimaryForeground}; text-decoration:none;">{label}</a>
                </td>
              </tr>
            </table>
            <p class="tx-mut" style="margin:0 0 20px 0; font-size:13px; line-height:1.5; color:{ColorMuted};">{Messages.Email_ButtonFallbackHint}<br /><a href="{Enc(
                url
            )}" target="_blank" class="tx" style="color:{ColorForeground}; text-decoration:underline; word-break:break-all;">{Enc(
                url
            )}</a></p>
            """;

    private static string Badge(string text, Tone tone, string symbol) =>
        $"""
            <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 0 16px 0;">
              <tr>
                <td class="tone-{tone.Css}" style="background-color:{tone.Bg}; padding:6px 12px; font-size:13px; line-height:1.4; font-weight:600; color:{ColorForeground};"><span class="{tone.MarkCss}" style="font-family:{FontSymbol}; font-weight:700; color:{tone.Mark};">{symbol}</span><span class="tx" style="padding-left:8px; color:{ColorForeground};">{text}</span></td>
              </tr>
            </table>
            """;

    private static string Callout(string html, Tone tone, string symbol) =>
        $"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" class="tone-{tone.Css}" style="background-color:{tone.Bg}; margin:0 0 20px 0;">
              <tr>
                <td class="{tone.MarkCss}" width="20" valign="top" style="width:20px; padding:14px 0 14px 16px; font-family:{FontSymbol}; font-size:15px; line-height:1.55; font-weight:700; color:{tone.Mark};">{symbol}</td>
                <td class="tx" style="padding:14px 16px 14px 8px; font-size:15px; line-height:1.55; color:{ColorForeground};">{html}</td>
              </tr>
            </table>
            """;

    private static string DetailRow(
        string label,
        string value,
        bool isLast = false,
        bool code = false
    )
    {
        var line = isLast ? string.Empty : $"border-bottom:1px solid {ColorBorder};";
        var valueStyle = code ? $"{StyleCode} font-size:15px;" : "font-size:15px; font-weight:600;";

        return $"""
            <tr>
              <td class="tx-mut bd" style="padding:12px 16px 12px 0; {line} vertical-align:baseline; font-size:13px; line-height:1.4; font-weight:500; color:{ColorMuted};">{label}</td>
              <td class="tx bd" style="padding:12px 0; {line} vertical-align:baseline; {valueStyle} line-height:1.4; color:{ColorForeground}; text-align:right;">{value}</td>
            </tr>
            """;
    }

    private static string DetailRowTotal(string label, decimal amount) =>
        $"""
            <tr>
              <td class="tx" style="padding:16px 16px 12px 0; vertical-align:baseline; font-size:15px; line-height:1.4; font-weight:600; color:{ColorForeground};">{label}</td>
              <td class="tx" style="padding:12px 0; vertical-align:baseline; white-space:nowrap; {StyleNumeral} font-size:28px; color:{ColorForeground}; text-align:right;">{MoneyDigits(
                amount
            )}<span style="font-family:{FontBody}; font-size:15px; font-weight:600; letter-spacing:0;">&nbsp;{Messages.Common_CurrencyVnd}</span></td>
            </tr>
            """;

    private static string DetailCard(params string[] rows) =>
        $"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" class="bg-sec" style="background-color:{ColorSecondary}; margin:4px 0 20px 0;">
              <tr>
                <td style="padding:4px 16px;">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                    {string.Concat(rows)}
                  </table>
                </td>
              </tr>
            </table>
            """;

    private static string TicketStub(string bookingCode) =>
        $"""
            <tr>
              <td style="padding:0;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                  <tr>
                    <td class="bg-card" width="10" style="width:10px; height:10px; padding:0; background-color:{ColorCard}; font-size:0; line-height:0;"><div class="bg-page" style="width:10px; height:10px; background-color:{ColorBackground}; border-radius:0 10px 0 0; font-size:0; line-height:0;">&nbsp;</div></td>
                    <td class="bg-card" style="height:10px; padding:0; background-color:{ColorCard}; font-size:0; line-height:0;">&nbsp;</td>
                    <td class="bg-card" width="10" style="width:10px; height:10px; padding:0; background-color:{ColorCard}; font-size:0; line-height:0;"><div class="bg-page" style="width:10px; height:10px; background-color:{ColorBackground}; border-radius:10px 0 0 0; font-size:0; line-height:0;">&nbsp;</div></td>
                  </tr>
                  <tr>
                    <td class="bg-pri" width="10" style="width:10px; height:10px; padding:0; background-color:{ColorPrimary}; font-size:0; line-height:0;"><div class="bg-page" style="width:10px; height:10px; background-color:{ColorBackground}; border-radius:0 0 10px 0; font-size:0; line-height:0;">&nbsp;</div></td>
                    <td class="bg-pri" style="height:7px; padding:3px 0 0 0; background-color:{ColorPrimary}; font-size:0; line-height:0;"><div style="height:0; border-top:3px dotted {ColorPrimaryForeground}; font-size:0; line-height:0;">&nbsp;</div></td>
                    <td class="bg-pri" width="10" style="width:10px; height:10px; padding:0; background-color:{ColorPrimary}; font-size:0; line-height:0;"><div class="bg-page" style="width:10px; height:10px; background-color:{ColorBackground}; border-radius:0 0 0 10px; font-size:0; line-height:0;">&nbsp;</div></td>
                  </tr>
                </table>
              </td>
            </tr>
            <tr>
              <td class="bg-pri" style="background-color:{ColorPrimary}; padding:8px 28px 28px 28px;">
                <p style="margin:0 0 4px 0; font-size:13px; line-height:1.5; font-weight:500; color:{ColorPrimaryForeground};">{Messages.Email_BookingCodeLabel}</p>
                <p style="margin:0; {StyleNumeral} font-size:32px; letter-spacing:0.04em; word-break:break-all; color:{ColorPrimaryForeground};">{Enc(
                bookingCode
            )}</p>
              </td>
            </tr>
            """;

    public static string ActionLink(
        BrandOptions brand,
        string fullName,
        string title,
        string introText,
        string buttonLabel,
        string link,
        string expiresIn
    )
    {
        var body = $"""
            {Heading(title)}
            {Paragraph(string.Format(Messages.Email_Greeting, Enc(fullName)))}
            {Paragraph(introText)}
            {Button(buttonLabel, link)}
            {MutedParagraph(
                string.Format(Messages.Email_LinkExpiry, expiresIn)
            )}
            """;

        return Layout(brand, title, body);
    }

    public static string BookingConfirmed(
        BrandOptions brand,
        string customerName,
        string bookingCode,
        string movieTitle,
        IReadOnlyList<OrderTicketEmailLine> tickets,
        BookingComboSuggestionEmailData? comboSuggestions = null
    )
    {
        var rows = new List<string>();
        if (!string.IsNullOrWhiteSpace(movieTitle))
            rows.Add(DetailRow(Messages.Email_MovieLabel, Enc(movieTitle)));
        rows.Add(
            DetailRow(
                Messages.Email_SeatCountLabel,
                string.Format(Messages.Email_SeatCountValue, tickets.Count),
                isLast: tickets.Count == 0
            )
        );
        rows.AddRange(TicketRows(tickets, lastIsLast: true));

        var notes = new List<string> { Messages.Email_BookingConfirmedNote };
        var cardTypes = CardTypeNames(tickets);
        if (cardTypes.Count > 0)
            notes.Add(string.Format(Messages.Email_CardPresentationNote, Enc(string.Join(", ", cardTypes))));

        var body = $"""
            {Badge(Messages.Email_BadgeConfirmed, Success, SymbolCheck)}
            {Heading(Messages.Email_BookingConfirmedHeading)}
            {Paragraph(
                string.Format(
                    Messages.Email_BookingConfirmedBody,
                    Enc(customerName)
                )
            )}
            {DetailCard([.. rows])}
            {MutedParagraph(string.Join("<br><br>", notes))}
            {BookingComboSuggestionBlock(comboSuggestions)}
            """;

        return Layout(
            brand,
            string.Format(Messages.Email_BookingConfirmedPreheader, bookingCode),
            body,
            TicketStub(bookingCode)
        );
    }

    private static string BookingComboSuggestionBlock(BookingComboSuggestionEmailData? data)
    {
        if (data is null || data.Items.Count == 0)
            return string.Empty;

        var rows = data.Items.Select(
            (item, i) =>
            {
                var price =
                    item.PreorderPrice < item.ListPrice
                        ? $"""<s class="tx-mut" style="font-weight:400; color:{ColorMuted};">{Money(item.ListPrice)}</s> {Money(item.PreorderPrice)}"""
                        : Money(item.PreorderPrice);
                return DetailRow(
                    Enc(string.Format(Messages.Email_BookingComboItemLabel, item.ComboName, item.Quantity)),
                    price,
                    isLast: i == data.Items.Count - 1
                );
            }
        );

        var validUntil = BusinessTime
            .ToLocal(data.ValidUntilUtc)
            .ToString(DateTimePatterns.DateHourMinute, CultureInfo.InvariantCulture);
        var notes = new List<string>
        {
            string.Format(Messages.Email_BookingComboValidNote, Enc(validUntil)),
        };
        if (data.DiscountPercent > 0)
            notes.Insert(0, string.Format(Messages.Email_BookingComboDiscountNote, data.DiscountPercent));

        return $"""
            <div style="height:20px; line-height:20px; font-size:0;">&nbsp;</div>
            {Heading3(Messages.Email_BookingComboHeading)}
            {Paragraph(Messages.Email_BookingComboIntro)}
            {DetailCard([.. rows])}
            {Button(Messages.Email_BookingComboButton, data.PurchaseLink)}
            {MutedParagraph(string.Join(" ", notes))}
            """;
    }

    public static string OrderExchanged(BrandOptions brand, OrderExchangeEmailData data)
    {
        var hasCredit = data.CreditAmount > 0m && !string.IsNullOrEmpty(data.CreditCode);
        var hasPaid = data.AmountPaid > 0m;
        string Local(DateTime utc) =>
            BusinessTime
                .ToLocal(utc)
                .ToString(DateTimePatterns.DateHourMinute, CultureInfo.InvariantCulture);

        var rows = new List<string>
        {
            DetailRow(Messages.Email_OrderCodeLabel, Enc(data.OrderCode), code: true),
            DetailRow(Messages.Email_MovieLabel, Enc(data.MovieTitle)),
            DetailRow(Messages.Email_ExchangeOldShowtimeLabel, Enc(Local(data.OldStartUtc))),
            DetailRow(Messages.Email_ExchangeNewShowtimeLabel, Enc(Local(data.NewStartUtc))),
            DetailRow(
                Messages.Email_RoomChangedToRoomLabel,
                Enc(data.NewRoomName),
                isLast: data.Tickets.Count == 0 && !hasPaid && !hasCredit
            ),
        };
        rows.AddRange(
            TicketRows(data.Tickets, lastIsLast: !hasPaid && !hasCredit)
        );
        if (hasPaid)
            rows.Add(DetailRow(Messages.Email_ExchangeAmountPaidLabel, Money(data.AmountPaid), isLast: !hasCredit));
        if (hasCredit)
        {
            rows.Add(DetailRow(Messages.Email_ExchangeCreditLabel, Money(data.CreditAmount)));
            rows.Add(DetailRow(Messages.Email_GiftCardCodeLabel, Enc(data.CreditCode!), code: true, isLast: data.CreditExpiresAtUtc is null));
            if (data.CreditExpiresAtUtc is { } expiresAt)
                rows.Add(DetailRow(Messages.Email_GiftCardExpiryLabel, GiftCardDate(expiresAt), isLast: true));
        }

        var notes = new List<string> { Messages.Email_ExchangeOldTicketNote };
        if (hasCredit)
            notes.Add(Messages.Email_ExchangeCreditNote);
        var cardTypes = CardTypeNames(data.Tickets);
        if (cardTypes.Count > 0)
            notes.Add(string.Format(Messages.Email_CardPresentationNote, Enc(string.Join(", ", cardTypes))));

        var body = $"""
            {Badge(Messages.Email_BadgeExchanged, Success, SymbolCheck)}
            {Heading(Messages.Email_ExchangeHeading)}
            {Paragraph(string.Format(Messages.Email_ExchangeBody, Enc(data.CustomerName), Enc(data.OrderCode)))}
            {DetailCard([.. rows])}
            {MutedParagraph(string.Join("<br><br>", notes))}
            """;

        return Layout(
            brand,
            string.Format(Messages.Email_ExchangePreheader, data.OrderCode),
            body,
            TicketStub(data.ExchangeOrderCode)
        );
    }

    public static string SupplementOrderReminder(
        BrandOptions brand,
        SupplementReminderEmailData data
    )
    {
        var rows = new List<string>
        {
            DetailRow(Messages.Email_SupplementReminderOriginalLabel, Enc(data.OriginalOrderCode), code: true),
        };
        for (var i = 0; i < data.Supplements.Count; i++)
        {
            var supplement = data.Supplements[i];
            var isLastOrder = i == data.Supplements.Count - 1;
            rows.Add(
                DetailRow(
                    Messages.Email_SupplementReminderOrderLabel,
                    Enc(supplement.OrderCode),
                    code: true
                )
            );
            for (var j = 0; j < supplement.Combos.Count; j++)
            {
                var combo = supplement.Combos[j];
                rows.Add(
                    DetailRow(
                        Enc(combo.ComboName),
                        string.Format(Messages.Email_SupplementReminderComboQuantity, combo.Remaining),
                        isLast: isLastOrder && j == supplement.Combos.Count - 1
                    )
                );
            }
        }

        var intro = data.Reason == SupplementReminderReason.OriginalExchanged
            ? Messages.Email_SupplementReminderExchangedBody
            : Messages.Email_SupplementReminderCancelledBody;

        var body = $"""
            {Badge(Messages.Email_BadgeSupplementReminder, Warning, SymbolAlert)}
            {Heading(Messages.Email_SupplementReminderHeading)}
            {Paragraph(string.Format(intro, Enc(data.CustomerName), Enc(data.OriginalOrderCode)))}
            {DetailCard([.. rows])}
            {MutedParagraph(Messages.Email_SupplementReminderActionNote)}
            """;

        return Layout(
            brand,
            string.Format(Messages.Email_SupplementReminderPreheader, data.OriginalOrderCode),
            body
        );
    }

    public static string ShowtimeReminder(BrandOptions brand, ShowtimeReminderEmailData data)
    {
        var start = BusinessTime.ToLocal(data.StartUtc);
        var startText = start.ToString(DateTimePatterns.DateHourMinute, CultureInfo.InvariantCulture);
        var checkInText = start
            .AddMinutes(-data.CheckInOpenMinutes)
            .ToString(DateTimePatterns.HourMinute, CultureInfo.InvariantCulture);

        var rows = new List<string>
        {
            DetailRow(Messages.Email_MovieLabel, Enc(data.MovieTitle)),
            DetailRow(Messages.Email_ShowtimeReminderStartLabel, Enc(startText)),
            DetailRow(Messages.Email_ShowtimeReminderRoomLabel, Enc(data.RoomName)),
            DetailRow(Messages.Email_ShowtimeReminderAddressLabel, Enc(data.CinemaAddress)),
            DetailRow(
                Messages.Email_ShowtimeReminderCheckInLabel,
                Enc(checkInText),
                isLast: true
            ),
        };

        var qrBlocks = string.Concat(data.Tickets.Select(ShowtimeReminderQrBlock));

        var comboBlock = string.Empty;
        if (data.Combos.Count > 0)
        {
            var comboRows = data.Combos.Select(
                (c, i) =>
                    DetailRow(
                        c.SupplementOrderCode is null
                            ? Enc(c.ComboName)
                            : Enc(
                                string.Format(
                                    Messages.Email_ShowtimeReminderComboSupplementLabel,
                                    c.ComboName,
                                    c.SupplementOrderCode
                                )
                            ),
                        string.Format(Messages.Email_SupplementReminderComboQuantity, c.Remaining),
                        isLast: i == data.Combos.Count - 1
                    )
            );
            comboBlock = $"""
                {Heading3(Messages.Email_ShowtimeReminderComboHeading)}
                {DetailCard([.. comboRows])}
                {MutedParagraph(Messages.Email_ShowtimeReminderComboNote)}
                """;
        }

        var notes = new List<string>
        {
            string.Format(
                Messages.Email_ShowtimeReminderCheckInNote,
                Enc(checkInText),
                data.CheckInOpenMinutes
            ),
        };
        var cardTypes = ReminderCardTypeNames(data.Tickets);
        if (cardTypes.Count > 0)
            notes.Add(
                string.Format(
                    Messages.Email_CardPresentationNote,
                    Enc(string.Join(", ", cardTypes))
                )
            );

        var body = $"""
            {Badge(Messages.Email_BadgeShowtimeReminder, Warning, SymbolAlert)}
            {Heading(Messages.Email_ShowtimeReminderHeading)}
            {Paragraph(
                string.Format(
                    Messages.Email_ShowtimeReminderBody,
                    Enc(data.CustomerName),
                    Enc(data.MovieTitle),
                    Enc(startText)
                )
            )}
            {DetailCard([.. rows])}
            {Heading3(Messages.Email_ShowtimeReminderQrHeading)}
            {qrBlocks}
            {Button(Messages.Email_ShowtimeReminderViewButton, data.TicketsLink)}
            {comboBlock}
            {MutedParagraph(string.Join("<br><br>", notes))}
            <div style="height:12px; line-height:12px; font-size:0;">&nbsp;</div>
            {MutedParagraph(Messages.Email_ShowtimeReminderOptOutNote)}
            """;

        return Layout(
            brand,
            string.Format(Messages.Email_ShowtimeReminderPreheader, data.MovieTitle, startText),
            body,
            TicketStub(data.OrderCode)
        );
    }

    public static string PointsExpiryReminder(BrandOptions brand, PointsExpiryReminderEmailData data)
    {
        var rows = data
            .Lots.Select(
                (lot, index) =>
                    DetailRow(
                        Enc(
                            string.Format(
                                Messages.Email_PointsExpiryReminderLotLabel,
                                lot.Points.ToString("N0", MoneyFormat)
                            )
                        ),
                        Enc(
                            BusinessTime
                                .ToLocal(lot.ExpiresAtUtc)
                                .ToString(DateTimePatterns.Date, CultureInfo.InvariantCulture)
                        ),
                        isLast: index == data.Lots.Count - 1
                    )
            )
            .ToArray();

        var body = $"""
            {Badge(Messages.Email_BadgePointsExpiry, Warning, SymbolAlert)}
            {Heading(Messages.Email_PointsExpiryReminderHeading)}
            {Paragraph(
                string.Format(
                    Messages.Email_PointsExpiryReminderBody,
                    Enc(data.CustomerName),
                    data.TotalPoints.ToString("N0", MoneyFormat)
                )
            )}
            {DetailCard(rows)}
            {Button(Messages.Email_PointsExpiryReminderButton, data.AccountLink)}
            <div style="height:12px; line-height:12px; font-size:0;">&nbsp;</div>
            {MutedParagraph(Messages.Email_PointsExpiryReminderOptOutNote)}
            """;

        return Layout(
            brand,
            string.Format(
                Messages.Email_PointsExpiryReminderPreheader,
                data.TotalPoints.ToString("N0", MoneyFormat)
            ),
            body
        );
    }

    private static string Heading3(string text) =>
        $"""<h2 class="tx" style="margin:8px 0 12px 0; font-family:{FontHeading}; font-weight:700; font-size:18px; line-height:1.25; color:{ColorForeground};">{Enc(text)}</h2>""";

    private static string ShowtimeReminderQrBlock(ShowtimeReminderTicketLine ticket)
    {
        var label = Enc(
            string.Format(Messages.Email_TicketLineLabel, ticket.SeatLabel, ticket.TicketTypeName)
        );
        var alt = Enc(string.Format(Messages.Email_ShowtimeReminderQrAlt, ticket.SeatLabel));

        return $"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" class="bg-sec" style="background-color:{ColorSecondary}; margin:0 0 12px 0;">
              <tr>
                <td align="center" style="padding:16px;">
                  <p class="tx" style="margin:0 0 10px 0; font-size:15px; line-height:1.4; font-weight:600; color:{ColorForeground};">{label}</p>
                  <img src="{QrCodeHelper.GenerateDataUri(ticket.QrCode)}" width="160" height="160" alt="{alt}" style="display:block; width:160px; height:160px; padding:8px; background-color:#ffffff;" />
                </td>
              </tr>
            </table>
            """;
    }

    private static List<string> ReminderCardTypeNames(
        IReadOnlyList<ShowtimeReminderTicketLine> tickets
    ) =>
        tickets
            .Where(t => t.RequiresCardPresentation)
            .Select(t => t.TicketTypeName)
            .Distinct()
            .ToList();

    public static string InvoiceAdjustmentNeeded(
        BrandOptions brand,
        string orderCode,
        string invoiceNumber,
        decimal refundAmount
    )
    {
        var body = $"""
            {Badge(Messages.Email_BadgeInvoiceAdjustment, Warning, SymbolAlert)}
            {Heading(Messages.Email_InvoiceAdjustmentHeading)}
            {Paragraph(string.Format(Messages.Email_InvoiceAdjustmentBody, Enc(orderCode)))}
            {DetailCard(
                DetailRow(Messages.Email_OrderCodeLabel, Enc(orderCode), code: true),
                DetailRow(Messages.Email_InvoiceNumberLabel, Enc(invoiceNumber), code: true),
                DetailRow(Messages.Email_RefundAmountLabel, Money(refundAmount), isLast: true)
            )}
            {MutedParagraph(Messages.Email_InvoiceAdjustmentNote)}
            """;

        return Layout(
            brand,
            string.Format(Messages.Email_InvoiceAdjustmentPreheader, orderCode),
            body
        );
    }

    public static string InvoiceRequestProcessed(
        BrandOptions brand,
        string customerName,
        string orderCode,
        InvoiceRequestStatus status,
        string? invoiceNumber,
        string? rejectReason
    )
    {
        var issued = status == InvoiceRequestStatus.Issued;
        var rows = new List<string>
        {
            DetailRow(
                Messages.Email_OrderCodeLabel,
                Enc(orderCode),
                isLast: !issued && string.IsNullOrWhiteSpace(rejectReason),
                code: true
            ),
        };
        if (issued)
            rows.Add(DetailRow(Messages.Email_InvoiceNumberLabel, Enc(invoiceNumber), isLast: true, code: true));
        else if (!string.IsNullOrWhiteSpace(rejectReason))
            rows.Add(DetailRow(Messages.Email_InvoiceRejectReasonLabel, Enc(rejectReason), isLast: true));

        var body = $"""
            {Badge(
                issued ? Messages.Email_BadgeInvoiceIssued : Messages.Email_BadgeInvoiceRejected,
                issued ? Success : Danger,
                issued ? SymbolCheck : SymbolCross
            )}
            {Heading(issued ? Messages.Email_InvoiceIssuedHeading : Messages.Email_InvoiceRejectedHeading)}
            {Paragraph(
                string.Format(
                    issued ? Messages.Email_InvoiceIssuedBody : Messages.Email_InvoiceRejectedBody,
                    Enc(customerName),
                    Enc(orderCode)
                )
            )}
            {DetailCard(rows.ToArray())}
            {MutedParagraph(issued ? Messages.Email_InvoiceIssuedNote : Messages.Email_InvoiceRejectedNote)}
            """;

        return Layout(
            brand,
            string.Format(
                issued ? Messages.Email_InvoiceIssuedPreheader : Messages.Email_InvoiceRejectedPreheader,
                orderCode
            ),
            body
        );
    }

    public static string OrderCancelled(
        BrandOptions brand,
        string customerName,
        string orderCode,
        IReadOnlyList<OrderTicketEmailLine> tickets,
        IReadOnlyList<OrderRefundEmailLine> refunds
    )
    {
        var totalRefund = refunds.Sum(r => r.Amount);
        var hasRefund = refunds.Count > 0;

        var rows = new List<string>
        {
            DetailRow(
                Messages.Email_OrderCodeLabel,
                Enc(orderCode),
                isLast: !hasRefund && tickets.Count == 0,
                code: true
            ),
        };
        rows.AddRange(TicketRows(tickets, lastIsLast: !hasRefund));
        rows.AddRange(
            refunds.Select(r =>
                DetailRow(RefundMethodLabel(r.Method), Money(r.Amount))
            )
        );
        if (hasRefund)
            rows.Add(DetailRowTotal(Messages.Email_RefundAmountLabel, totalRefund));

        var notes = new List<string>();
        if (!hasRefund)
            notes.Add(Messages.Email_OrderCancelledUnpaidNote);
        if (refunds.Any(r => r.Method.IsGatewayMethod()))
            notes.Add(Messages.Email_RefundNoteBankOrGateway);
        if (refunds.Any(r => r.Method == PaymentMethod.Cash))
            notes.Add(Messages.Email_RefundNoteCash);

        var body = $"""
            {Badge(Messages.Email_BadgeOrderCancelled, Danger, SymbolCross)}
            {Heading(Messages.Email_OrderCancelledHeading)}
            {Paragraph(
                string.Format(Messages.Email_OrderCancelledBody, Enc(customerName), Enc(orderCode))
            )}
            {DetailCard(rows.ToArray())}
            {(notes.Count > 0 ? MutedParagraph(string.Join("<br><br>", notes)) : string.Empty)}
            """;

        return Layout(
            brand,
            hasRefund
                ? string.Format(
                    Messages.Email_OrderCancelledRefundPreheader,
                    orderCode,
                    Money(totalRefund)
                )
                : string.Format(Messages.Email_OrderCancelledPreheader, orderCode),
            body
        );
    }

    private static IEnumerable<string> TicketRows(
        IReadOnlyList<OrderTicketEmailLine> tickets,
        bool lastIsLast
    ) =>
        tickets.Select(
            (t, i) =>
                DetailRow(
                    Enc(string.Format(Messages.Email_TicketLineLabel, t.SeatLabel, t.TicketTypeName)),
                    Money(t.PaidAmount),
                    isLast: lastIsLast && i == tickets.Count - 1
                )
        );

    private static List<string> CardTypeNames(IReadOnlyList<OrderTicketEmailLine> tickets) =>
        tickets
            .Where(t => t.RequiresCardPresentation)
            .Select(t => t.TicketTypeName)
            .Distinct()
            .ToList();

    private static string RefundMethodLabel(PaymentMethod method) =>
        method switch
        {
            PaymentMethod.Cash => Messages.Email_RefundMethodCash,
            PaymentMethod.GiftCard => Messages.Email_RefundMethodGiftCard,
            PaymentMethod.BankTransfer => Messages.Email_RefundMethodBankTransfer,
            PaymentMethod.Card => Messages.Email_RefundMethodCard,
            _ => string.Format(Messages.Email_RefundMethodGateway, method.ToDtoMethod()),
        };

    public static string ShowtimeCancelledNotice(
        BrandOptions brand,
        string customerName,
        string orderCode,
        IReadOnlyList<OrderRefundEmailLine> refunds
    )
    {
        var cashAmount = refunds.Where(r => r.Method == PaymentMethod.Cash).Sum(r => r.Amount);
        var hasGateway = refunds.Any(r => r.Method.IsGatewayMethod());

        var rows = new List<string>
        {
            DetailRow(Messages.Email_OrderCodeLabel, Enc(orderCode), isLast: cashAmount <= 0m, code: true),
        };
        if (cashAmount > 0m)
            rows.Add(DetailRowTotal(Messages.Email_ShowtimeCancelledCashLabel, cashAmount));

        var notes = new List<string>();
        if (refunds.Count == 0)
            notes.Add(Messages.Email_OrderCancelledUnpaidNote);
        if (hasGateway)
            notes.Add(Messages.Email_RefundNoteBankOrGateway);
        if (cashAmount > 0m)
            notes.Add(Messages.Email_ShowtimeCancelledCashNote);

        var body = $"""
            {Badge(Messages.Email_BadgeShowtimeCancelled, Danger, SymbolCross)}
            {Heading(Messages.Email_ShowtimeCancelledHeading)}
            {Paragraph(
                string.Format(
                    Messages.Email_ShowtimeCancelledNoticeBody,
                    Enc(customerName),
                    Enc(orderCode)
                )
            )}
            {DetailCard(rows.ToArray())}
            {(notes.Count > 0 ? MutedParagraph(string.Join("<br><br>", notes)) : string.Empty)}
            """;

        return Layout(
            brand,
            cashAmount > 0m
                ? string.Format(
                    Messages.Email_ShowtimeCancelledManualPreheader,
                    orderCode,
                    Money(cashAmount)
                )
                : string.Format(Messages.Email_ShowtimeCancelledNoticePreheader, orderCode),
            body
        );
    }

    public static string ShowtimeRoomChanged(
        BrandOptions brand,
        string customerName,
        string orderCode,
        string movieTitle,
        DateTime showtimeStartUtc,
        string fromRoomName,
        string toRoomName,
        IReadOnlyList<string> seatLabels,
        decimal refundDueAmount
    )
    {
        var startText = BusinessTime
            .ToLocal(showtimeStartUtc)
            .ToString(DateTimePatterns.DateHourMinute, CultureInfo.InvariantCulture);

        var rows = new List<string>
        {
            DetailRow(Messages.Email_OrderCodeLabel, Enc(orderCode), code: true),
            DetailRow(Messages.Email_MovieLabel, Enc(movieTitle)),
            DetailRow(Messages.Email_RoomChangedShowtimeLabel, Enc(startText)),
            DetailRow(Messages.Email_RoomChangedFromRoomLabel, Enc(fromRoomName)),
            DetailRow(
                Messages.Email_RoomChangedToRoomLabel,
                Enc(toRoomName),
                isLast: seatLabels.Count == 0 && refundDueAmount <= 0m
            ),
        };
        if (seatLabels.Count > 0)
            rows.Add(
                DetailRow(
                    Messages.Email_RoomChangedSeatsLabel,
                    Enc(string.Join(", ", seatLabels)),
                    isLast: refundDueAmount <= 0m
                )
            );
        if (refundDueAmount > 0m)
            rows.Add(DetailRowTotal(Messages.Email_RoomChangedRefundLabel, refundDueAmount));

        var notes = new List<string>();
        if (refundDueAmount > 0m)
            notes.Add(Messages.Email_RoomChangedRefundNote);
        notes.Add(string.Format(Messages.Email_RoomChangedCancelNote, Enc(orderCode)));

        var body = $"""
            {Badge(Messages.Email_BadgeRoomChanged, Warning, SymbolAlert)}
            {Heading(Messages.Email_RoomChangedHeading)}
            {Paragraph(
                string.Format(Messages.Email_RoomChangedBody, Enc(customerName), Enc(orderCode))
            )}
            {DetailCard([.. rows])}
            {MutedParagraph(string.Join("<br><br>", notes))}
            """;

        return Layout(
            brand,
            string.Format(Messages.Email_RoomChangedPreheader, orderCode, fromRoomName, toRoomName),
            body
        );
    }

    public static string ShowtimeCancelledAutoRefunded(
        BrandOptions brand,
        string customerName,
        string orderCode,
        decimal refundAmount
    )
    {
        var body = $"""
            {Badge(Messages.Email_BadgeShowtimeCancelled, Danger, SymbolCross)}
            {Heading(Messages.Email_ShowtimeCancelledHeading)}
            {Paragraph(
                string.Format(
                    Messages.Email_ShowtimeCancelledAutoBody,
                    Enc(customerName),
                    Enc(orderCode)
                )
            )}
            {DetailCard(
                DetailRow(Messages.Email_OrderCodeLabel, Enc(orderCode), code: true),
                DetailRowTotal(Messages.Email_RefundedAmountLabel, refundAmount)
            )}
            {MutedParagraph(
                Messages.Email_RefundNoteBankOrGateway
            )}
            """;

        return Layout(
            brand,
            string.Format(
                Messages.Email_ShowtimeCancelledAutoPreheader,
                orderCode,
                Money(refundAmount)
            ),
            body
        );
    }

    public static string ShowtimeCancelledManualRefund(
        BrandOptions brand,
        string customerName,
        string orderCode,
        decimal amountDue
    )
    {
        var body = $"""
            {Badge(Messages.Email_BadgeShowtimeCancelled, Danger, SymbolCross)}
            {Heading(Messages.Email_ShowtimeCancelledHeading)}
            {Paragraph(
                string.Format(
                    Messages.Email_ShowtimeCancelledManualBody,
                    Enc(customerName),
                    Enc(orderCode)
                )
            )}
            {DetailCard(
                DetailRow(Messages.Email_OrderCodeLabel, Enc(orderCode), code: true),
                DetailRowTotal(Messages.Email_AmountDueLabel, amountDue)
            )}
            {MutedParagraph(
                Messages.Email_ShowtimeCancelledManualNote
            )}
            """;

        return Layout(
            brand,
            string.Format(
                Messages.Email_ShowtimeCancelledManualPreheader,
                orderCode,
                Money(amountDue)
            ),
            body
        );
    }

    public static string SeatsExpiredAfterPaymentCustomer(
        BrandOptions brand,
        string customerName,
        string bookingCode,
        bool autoRefunded,
        decimal amount
    )
    {
        var rows = new[]
        {
            DetailRow(Messages.Email_BookingCodeLabel, Enc(bookingCode), code: true),
            DetailRowTotal(
                autoRefunded ? Messages.Email_RefundedAmountLabel : Messages.Email_RefundWillBeAmountLabel,
                amount
            ),
        };

        var body = autoRefunded
            ? $"""
                {Badge(Messages.Email_BadgeRefunded, Neutral, SymbolCheck)}
                {Heading(Messages.Email_TicketNotIssuedHeading)}
                {Paragraph(
                    string.Format(
                        Messages.Email_SeatsExpiredCustomerRefundedBody,
                        Enc(customerName),
                        Enc(bookingCode)
                    )
                )}
                {DetailCard([.. rows])}
                {MutedParagraph(
                    Messages.Email_RefundNoteBank
                )}
                """
            : $"""
                {Badge(Messages.Email_BadgeRefundPending, Warning, SymbolAlert)}
                {Heading(Messages.Email_TicketNotIssuedHeading)}
                {Paragraph(
                    string.Format(
                        Messages.Email_SeatsExpiredCustomerPendingBody,
                        Enc(customerName),
                        Enc(bookingCode)
                    )
                )}
                {DetailCard([.. rows])}
                {MutedParagraph(
                    Messages.Email_SeatsExpiredCustomerPendingNote
                )}
                """;

        return Layout(
            brand,
            string.Format(
                Messages.Email_SeatsExpiredCustomerPreheader,
                bookingCode,
                autoRefunded
                    ? Messages.Email_StatusRefunded
                    : Messages.Email_StatusRefundPending
            ),
            body
        );
    }

    public static string SeatsExpiredAfterPayment(
        BrandOptions brand,
        bool autoRefunded,
        int bookingId,
        int paymentId,
        decimal amount
    )
    {
        var rows = new[]
        {
            DetailRow(Messages.Email_BookingLabel, $"#{bookingId}", code: true),
            DetailRow(Messages.Email_PaymentLabel, $"#{paymentId}", code: true),
            DetailRowTotal(
                autoRefunded ? Messages.Email_RefundedAmountLabel : Messages.Email_AmountDueLabel,
                amount
            ),
        };

        var body = autoRefunded
            ? $"""
                {Badge(Messages.Email_BadgeAutoRefunded, Success, SymbolCheck)}
                {Heading(Messages.Email_SeatsExpiredAdminHeading)}
                {Paragraph(
                    Messages.Email_SeatsExpiredAdminIntro
                )}
                {Paragraph(
                    Messages.Email_SeatsExpiredAdminAutoRefundNote
                )}
                {DetailCard([.. rows])}
                """
            : $"""
                {Badge(Messages.Email_BadgeManualRequired, Danger, SymbolAlert)}
                {Heading(Messages.Email_SeatsExpiredAdminHeading)}
                {Paragraph(
                    Messages.Email_SeatsExpiredAdminIntro
                )}
                {Callout(
                    Messages.Email_SeatsExpiredAdminManualCallout,
                    Danger,
                    SymbolAlert
                )}
                {DetailCard([.. rows])}
                """;

        return Layout(
            brand,
            autoRefunded
                ? string.Format(
                    Messages.Email_SeatsExpiredAdminAutoPreheader,
                    bookingId
                )
                : string.Format(
                    Messages.Email_SeatsExpiredAdminManualPreheader,
                    bookingId
                ),
            body
        );
    }

    public static string LowStockAlert(BrandOptions brand, IReadOnlyList<LowStockEmailLine> items)
    {
        var rows = items
            .Select(
                (item, index) =>
                    DetailRow(
                        Enc($"{item.Name} ({item.Code})"),
                        Enc(
                            string.Format(
                                Messages.Email_LowStockRowValue,
                                FormatQuantity(item.QuantityOnHand),
                                item.Unit,
                                FormatQuantity(item.LowStockThreshold)
                            )
                        ),
                        isLast: index == items.Count - 1
                    )
            )
            .ToArray();

        var body = $"""
            {Badge(Messages.Email_BadgeLowStock, Warning, SymbolAlert)}
            {Heading(Messages.Email_LowStockHeading)}
            {Paragraph(Messages.Email_LowStockIntro)}
            {DetailCard(rows)}
            {MutedParagraph(Messages.Email_LowStockNote)}
            """;

        return Layout(
            brand,
            string.Format(Messages.Email_LowStockPreheader, items.Count),
            body
        );
    }

    public static string GiftCardReceived(BrandOptions brand, GiftCardEmailData data)
    {
        var message = string.IsNullOrWhiteSpace(data.Message)
            ? string.Empty
            : Callout(Enc(data.Message).Replace("\n", "<br>"), Neutral, "&ldquo;");

        var body = $"{Badge(Messages.Email_BadgeGiftCard, Success, SymbolCheck)}"
            + $"{Heading(Messages.Email_GiftCardReceivedHeading)}"
            + $"{Paragraph(string.Format(Messages.Email_GiftCardReceivedBody, Enc(data.RecipientName), Enc(data.PurchaserName), Enc(brand.DisplayName)))}"
            + message
            + DetailCard(
                DetailRow(
                    Messages.Email_GiftCardCodeLabel,
                    Enc(GiftCardCodes.Format(data.Code)),
                    code: true
                ),
                DetailRow(Messages.Email_GiftCardValueLabel, Money(data.Amount)),
                DetailRow(Messages.Email_GiftCardExpiryLabel, GiftCardDate(data.ExpiresAtUtc), isLast: true)
            )
            + MutedParagraph(Messages.Email_GiftCardUsageNote);

        return Layout(
            brand,
            string.Format(Messages.Email_GiftCardReceivedPreheader, Enc(data.PurchaserName)),
            body
        );
    }

    public static string GiftCardPurchaserConfirmation(BrandOptions brand, GiftCardEmailData data)
    {
        var body = $"{Badge(Messages.Email_BadgeGiftCardSent, Success, SymbolCheck)}"
            + $"{Heading(Messages.Email_GiftCardPurchaserHeading)}"
            + $"{Paragraph(string.Format(Messages.Email_GiftCardPurchaserBody, Enc(data.PurchaserName), Money(data.Amount), Enc(data.RecipientName)))}"
            + DetailCard(
                DetailRow(Messages.Email_GiftCardRecipientLabel, Enc(data.RecipientName)),
                DetailRow(Messages.Email_GiftCardRecipientEmailLabel, Enc(data.RecipientEmail)),
                DetailRow(Messages.Email_GiftCardValueLabel, Money(data.Amount)),
                DetailRow(Messages.Email_GiftCardExpiryLabel, GiftCardDate(data.ExpiresAtUtc), isLast: true)
            );

        return Layout(brand, Messages.Email_GiftCardPurchaserPreheader, body);
    }

    private static string GiftCardDate(DateTime utc) =>
        BusinessTime.ToLocal(utc).ToString(DateTimePatterns.Date, CultureInfo.InvariantCulture);

    public static string UnclosedShiftAlert(
        BrandOptions brand,
        IReadOnlyList<UnclosedShiftEmailLine> shifts
    )
    {
        var rows = shifts
            .Select(
                (shift, index) =>
                    DetailRow(
                        Enc(shift.EmployeeName),
                        Enc(
                            string.Format(
                                Messages.Email_UnclosedShiftRowValue,
                                BusinessTime
                                    .ToLocal(shift.OpenedAtUtc)
                                    .ToString(
                                        DateTimePatterns.DateHourMinute,
                                        CultureInfo.InvariantCulture
                                    ),
                                Money(shift.OpeningCash)
                            )
                        ),
                        isLast: index == shifts.Count - 1
                    )
            )
            .ToArray();

        var body = $"""
            {Badge(Messages.Email_BadgeUnclosedShift, Warning, SymbolAlert)}
            {Heading(Messages.Email_UnclosedShiftHeading)}
            {Paragraph(Messages.Email_UnclosedShiftIntro)}
            {DetailCard(rows)}
            {MutedParagraph(Messages.Email_UnclosedShiftNote)}
            """;

        return Layout(
            brand,
            string.Format(Messages.Email_UnclosedShiftPreheader, shifts.Count),
            body
        );
    }

    public static string DailyClosingReport(BrandOptions brand, DailyClosingEmailData report)
    {
        var date = report.BusinessDate.ToString(
            DateTimePatterns.Date,
            CultureInfo.InvariantCulture
        );
        var closedAt = BusinessTime
            .ToLocal(report.ClosedAtUtc)
            .ToString(DateTimePatterns.DateHourMinute, CultureInfo.InvariantCulture);
        var varianceTone = report.Variance == 0m ? Success : Warning;
        var varianceSymbol = report.Variance == 0m ? SymbolCheck : SymbolAlert;

        var rows = new[]
        {
            DetailRow(Messages.Email_DailyClosingRowTicket, Enc(Money(report.TicketRevenue))),
            DetailRow(Messages.Email_DailyClosingRowCombo, Enc(Money(report.ComboRevenue))),
            DetailRow(
                Messages.Email_DailyClosingRowGiftCardSold,
                Enc(Money(report.GiftCardSold))
            ),
            DetailRow(
                Messages.Email_DailyClosingRowGiftCardUsed,
                Enc(Money(report.GiftCardUsed))
            ),
            DetailRow(Messages.Email_DailyClosingRowDiscount, Enc(Money(report.TotalDiscount))),
            DetailRow(
                Messages.Email_DailyClosingRowRefund,
                Enc(Money(report.TicketRefundAmount))
            ),
            DetailRow(
                Messages.Email_DailyClosingRowCancelled,
                Enc(report.CancelledOrders.ToString(CultureInfo.InvariantCulture))
            ),
            DetailRow(
                Messages.Email_DailyClosingRowAdjustments,
                Enc(
                    string.Format(
                        Messages.Email_DailyClosingRowAdjustmentsValue,
                        report.AdjustmentCount,
                        Money(report.AdjustmentRevenue)
                    )
                )
            ),
            DetailRow(
                Messages.Email_DailyClosingRowOrders,
                Enc(
                    string.Format(
                        Messages.Email_DailyClosingRowOrdersValue,
                        report.PendingOrders,
                        report.ProblemOrders
                    )
                )
            ),
            DetailRow(Messages.Email_DailyClosingRowExpectedCash, Enc(Money(report.ExpectedCash))),
            DetailRow(Messages.Email_DailyClosingRowCountedCash, Enc(Money(report.CountedCash))),
            DetailRow(Messages.Email_DailyClosingRowVariance, Enc(Money(report.Variance))),
            DetailRowTotal(Messages.Email_DailyClosingRowTotal, report.TotalRevenue),
        };

        var note = string.IsNullOrWhiteSpace(report.Note)
            ? string.Empty
            : Callout(
                string.Format(Messages.Email_DailyClosingNoteLine, Enc(report.Note)),
                Neutral,
                SymbolAlert
            );

        var body = $"""
            {Badge(Messages.Email_BadgeDailyClosing, varianceTone, varianceSymbol)}
            {Heading(Enc(string.Format(Messages.Email_DailyClosingHeading, date)))}
            {Paragraph(Enc(string.Format(Messages.Email_DailyClosingIntro, date, report.ClosedByName, closedAt)))}
            {DetailCard(rows)}
            {note}
            {MutedParagraph(Messages.Email_DailyClosingFooter)}
            """;

        return Layout(
            brand,
            string.Format(Messages.Email_DailyClosingPreheader, date, Money(report.TotalRevenue)),
            body
        );
    }

    private static string FormatQuantity(decimal quantity) =>
        quantity.ToString("0.###", CultureInfo.InvariantCulture);
}
