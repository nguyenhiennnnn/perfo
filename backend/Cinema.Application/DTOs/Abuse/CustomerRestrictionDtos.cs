using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.Abuse;

public record CustomerRestrictionDto(
    int CustomerRestrictionId,
    int UserId,
    string CustomerName,
    string CustomerEmail,
    string Reason,
    string Status,
    int ExpiredOrderCount,
    DateTime StartedAt,
    DateTime ExpiresAt,
    DateTime? LiftedAt,
    int? LiftedByUserId,
    string? LiftedByName
);

public record CustomerRestrictionListFilter(CustomerRestrictionStatus? Status, string? Search);

public record ActiveCustomerRestriction(
    int CustomerRestrictionId,
    CustomerRestrictionReason Reason,
    int ExpiredOrderCount,
    DateTime ExpiresAt
);
