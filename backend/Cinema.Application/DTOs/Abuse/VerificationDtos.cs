namespace Cinema.Application.DTOs.Abuse;

public record CustomerVerificationStatusDto(
    bool Required,
    string? Reason,
    DateTime? CodeSentAt,
    DateTime? CodeExpiresAt,
    int? AttemptsLeft
);

public record ConfirmVerificationRequest(string Code);
