using System.Text.Json.Nodes;
using Cinema.Application.Caching;
using Cinema.Application.DTOs.Inventory;
using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Pricing;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Audit;

public static class AuditSnapshots
{
    public static JsonObject Settings(CinemaSettings settings, bool defaultPasswordChanged)
    {
        var snapshot = AuditJson.ToObject(CinemaSettingsSnapshot.From(settings));
        if (defaultPasswordChanged)
            snapshot["defaultEmployeePasswordChanged"] = true;
        return snapshot;
    }

    public static MembershipTierSnapshot Tier(MembershipTier tier) =>
        MembershipTierSnapshot.From(tier);

    public static JsonObject TicketTypeState(TicketType t) =>
        AuditJson.ToObject(
            new
            {
                t.Name,
                t.PriceMultiplier,
                t.RequiresCardVerification,
                allowedAgeRatingIds = t.AllowedAgeRatingIds.Order(),
                allowedFromTime = PricingMapper.FormatTime(t.AllowedFromTime),
                allowedToTime = PricingMapper.FormatTime(t.AllowedToTime),
                allowedDaysOfWeek = t.AllowedDaysOfWeek.Order(),
                t.IsActive,
                t.IsDeleted,
            }
        );

    public static JsonObject PricingRuleState(PricingRule r) =>
        AuditJson.ToObject(
            new
            {
                r.Name,
                r.RuleType,
                condition = PricingMapper.ToDto(
                    PricingRuleCondition.TryParse(r.ConditionJson) ?? new PricingRuleCondition()
                ),
                r.AdjustmentMode,
                r.AdjustmentValue,
                r.Priority,
                r.ValidFrom,
                r.ValidTo,
                roomIds = r.RoomIds.Order(),
                screenTypeIds = r.ScreenTypeIds.Order(),
                movieIds = r.MovieIds.Order(),
                r.IsActive,
                r.IsDeleted,
            }
        );

    public static JsonObject PromotionState(Promotion p) =>
        AuditJson.ToObject(
            new
            {
                p.Code,
                p.Name,
                p.Description,
                p.Type,
                p.DiscountType,
                p.DiscountValue,
                p.MaxDiscountAmount,
                p.Scope,
                p.MinOrderAmount,
                p.StartsAt,
                p.EndsAt,
                p.Channel,
                movieIds = p.MovieIds.Order(),
                showtimeIds = p.ShowtimeIds.Order(),
                screenTypeIds = p.ScreenTypeIds.Order(),
                daysOfWeek = p.DaysOfWeek.Order(),
                membershipTierIds = p.MembershipTierIds.Order(),
                p.NewCustomersOnly,
                p.TotalUsageLimit,
                p.PerCustomerLimit,
                p.BudgetAmount,
                p.Priority,
                p.IsExclusive,
                p.AssignedUserId,
                p.IsActive,
                p.IsDeleted,
            }
        );

    public static JsonObject SpecialDayState(SpecialDay d) =>
        AuditJson.ToObject(
            new
            {
                d.Name,
                d.StartDate,
                d.EndDate,
                d.IsActive,
                d.IsDeleted,
            }
        );

    public static JsonObject InventoryItemState(InventoryItem i) =>
        AuditJson.ToObject(
            new
            {
                i.Code,
                i.Name,
                i.Unit,
                i.LowStockThreshold,
                i.IsActive,
                i.IsDeleted,
            }
        );

    public static JsonObject ComboRecipeState(int comboId, IEnumerable<ComboRecipeLine> lines) =>
        AuditJson.ToObject(
            new
            {
                comboId,
                lines = lines
                    .OrderBy(l => l.InventoryItemId)
                    .Select(l => new { l.InventoryItemId, l.Quantity }),
            }
        );

    public static JsonObject StockReceiptState(
        StockReceipt r,
        IEnumerable<StockReceiptCostChange> costChanges
    ) =>
        AuditJson.ToObject(
            new
            {
                r.SupplierName,
                r.ReceivedDate,
                r.Note,
                lines = r.Lines.OrderBy(l => l.StockReceiptLineId)
                    .Select(l => new
                    {
                        l.InventoryItemId,
                        l.Quantity,
                        l.UnitCost,
                    }),
                costChanges = costChanges.Select(c => new
                {
                    c.InventoryItemId,
                    c.QuantityBefore,
                    c.CostBefore,
                    c.CostAfter,
                }),
            }
        );

    public static JsonObject StocktakeSheetState(
        StocktakeSheet s,
        IEnumerable<StockQuantityChange> quantityChanges
    ) =>
        AuditJson.ToObject(
            new
            {
                s.StocktakeDate,
                s.Note,
                lines = s.Lines.OrderBy(l => l.StocktakeSheetLineId)
                    .Select(l => new
                    {
                        l.InventoryItemId,
                        l.BookQuantity,
                        l.CountedQuantity,
                        l.Difference,
                    }),
                quantityChanges = quantityChanges.Select(c => new
                {
                    c.InventoryItemId,
                    c.QuantityBefore,
                    c.QuantityAfter,
                }),
            }
        );

    public static JsonObject WasteSheetState(
        WasteSheet s,
        IEnumerable<StockQuantityChange> quantityChanges
    ) =>
        AuditJson.ToObject(
            new
            {
                s.WasteDate,
                s.Reason,
                s.Note,
                lines = s.Lines.OrderBy(l => l.WasteSheetLineId)
                    .Select(l => new
                    {
                        l.InventoryItemId,
                        l.Quantity,
                        l.UnitCost,
                    }),
                quantityChanges = quantityChanges.Select(c => new
                {
                    c.InventoryItemId,
                    c.QuantityBefore,
                    c.QuantityAfter,
                }),
            }
        );

    public static JsonObject OrderBeforeCancel(OrderStatus status) =>
        AuditJson.ToObject(new { status });

    public static JsonObject OrderAfterCancel(
        CancelActor actor,
        CancelReason reason,
        decimal refundAmount,
        decimal manualRefundAmount,
        int voidedRefundDueCount = 0,
        decimal voidedRefundDueAmount = 0m
    )
    {
        var snapshot = AuditJson.ToObject(
            new
            {
                status = OrderStatus.Cancelled,
                reason,
                cancelledBy = actor.CancelledBy,
                actorKind = actor.Kind,
                actorUserId = actor.UserId,
                refundAmount,
                manualRefundAmount,
            }
        );
        if (voidedRefundDueCount > 0)
        {
            snapshot["voidedRefundDueCount"] = voidedRefundDueCount;
            snapshot["voidedRefundDueAmount"] = voidedRefundDueAmount;
        }

        return snapshot;
    }

    public static JsonObject ShowtimeBeforeCancel() =>
        AuditJson.ToObject(new { status = ShowtimeStatus.Scheduled });

    public static JsonObject ShowtimeAfterCancel(
        int ordersCancelled,
        int autoRefundOrders,
        int manualRefundOrders,
        decimal manualRefundAmount,
        CancelReason reason = CancelReason.ShowtimeCancelled
    ) =>
        AuditJson.ToObject(
            new
            {
                status = ShowtimeStatus.Cancelled,
                reason,
                ordersCancelled,
                autoRefundOrders,
                manualRefundOrders,
                manualRefundAmount,
            }
        );

    public static JsonObject ShowtimeRoomBeforeChange(int roomId) =>
        AuditJson.ToObject(new { roomId });

    public static JsonObject ShowtimeRoomAfterChange(
        int roomId,
        int seatsMoved,
        int seatsNeedingReview,
        int refundDueCount,
        decimal refundDueTotal,
        string? note
    ) =>
        AuditJson.ToObject(
            new
            {
                roomId,
                seatsMoved,
                seatsNeedingReview,
                refundDueCount,
                refundDueTotal,
                note,
            }
        );

    public static JsonObject RoomClosureState(RoomClosure c) =>
        AuditJson.ToObject(
            new
            {
                c.RoomId,
                c.StartTime,
                c.EndTime,
                c.Reason,
            }
        );

    public static JsonObject SeatBlockState(SeatBlock b) =>
        AuditJson.ToObject(
            new
            {
                b.ShowtimeId,
                b.ShowtimeSeatId,
                b.Type,
                b.Reason,
                b.ExpiresAt,
            }
        );

    public static JsonObject CashRefundBeforeConfirm() =>
        AuditJson.ToObject(new { status = PaymentStatus.Pending, needsManualRefund = true });

    public static JsonObject CashRefundAfterConfirm(PaymentTransaction refund, int confirmedByUserId) =>
        AuditJson.ToObject(
            new
            {
                status = PaymentStatus.Success,
                needsManualRefund = false,
                orderId = refund.OrderId,
                amount = refund.Amount,
                confirmedByUserId,
            }
        );

    public static JsonObject RoomChangeRefundBeforeConfirm(RoomChangeRefundDue due) =>
        AuditJson.ToObject(
            new
            {
                status = RoomChangeRefundStatus.Pending,
                orderId = due.OrderId,
                orderLineId = due.OrderLineId,
                amountDue = due.AmountDue,
            }
        );

    public static JsonObject RoomChangeRefundAfterConfirm(
        RoomChangeRefundDue due,
        int confirmedByUserId,
        IReadOnlyList<RoomChangeRefundTransactionDto> refunds
    ) =>
        AuditJson.ToObject(
            new
            {
                status = RoomChangeRefundStatus.Refunded,
                orderId = due.OrderId,
                orderLineId = due.OrderLineId,
                amountRefunded = due.AmountDue,
                confirmedByUserId,
                refunds = refunds.Select(r => new
                {
                    r.RefundTransactionId,
                    r.Method,
                    r.Amount,
                }),
            }
        );
}
