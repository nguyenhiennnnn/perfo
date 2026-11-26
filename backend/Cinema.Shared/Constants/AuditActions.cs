namespace Cinema.Shared.Constants;

public static class AuditActions
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string Cancel = "Cancel";
    public const string ChangeRoom = "ChangeRoom";
    public const string ManualRefund = "ManualRefund";
    public const string CardNotPresented = "CardNotPresented";
    public const string OpenShift = "OpenShift";
    public const string CloseShift = "CloseShift";
    public const string AdminCloseShift = "AdminCloseShift";
    public const string ShiftVariance = "ShiftVariance";
    public const string CloseDay = "CloseDay";
    public const string CreateLedgerAdjustment = "CreateLedgerAdjustment";
    public const string IssueInvoice = "IssueInvoice";
    public const string RejectInvoice = "RejectInvoice";
    public const string ReplaceInvoiceFile = "ReplaceInvoiceFile";
    public const string FlagInvoiceAdjustment = "FlagInvoiceAdjustment";
    public const string ResolveInvoiceAdjustment = "ResolveInvoiceAdjustment";
    public const string CancelGiftCard = "CancelGiftCard";
    public const string IssueExchangeCredit = "IssueExchangeCredit";
    public const string ExchangeCashRefund = "ExchangeCashRefund";
    public const string CompleteOrderExchange = "CompleteOrderExchange";
    public const string AdjustLoyaltyPoints = "AdjustLoyaltyPoints";
    public const string AssignSupportRequest = "AssignSupportRequest";
    public const string ChangeSupportRequestStatus = "ChangeSupportRequestStatus";
    public const string IssueSupportApologyCode = "IssueSupportApologyCode";
    public const string ResendTicketEmail = "ResendTicketEmail";
    public const string ProposeExceptionalRefund = "ProposeExceptionalRefund";
    public const string ApproveExceptionalRefund = "ApproveExceptionalRefund";
    public const string RejectExceptionalRefund = "RejectExceptionalRefund";
    public const string RestrictCustomer = "RestrictCustomer";
    public const string LiftCustomerRestriction = "LiftCustomerRestriction";
    public const string ChangeRoomRentalRequestStatus = "ChangeRoomRentalRequestStatus";
    public const string CreateRoomRentalQuoteVersion = "CreateRoomRentalQuoteVersion";
    public const string SendRoomRentalQuote = "SendRoomRentalQuote";
    public const string AcceptRoomRentalQuote = "AcceptRoomRentalQuote";
    public const string DeclineRoomRentalQuote = "DeclineRoomRentalQuote";
    public const string ExpireRoomRentalQuote = "ExpireRoomRentalQuote";
}
