using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

internal static class ExceptionalRefundReportSupport
{
    public static IQueryable<ExceptionalRefundProposal> ApprovedInWindow(
        IAppDbContext db,
        DateTime fromUtc,
        DateTime toUtc
    ) =>
        db.ExceptionalRefundProposals.AsNoTracking()
            .Where(p =>
                p.Status == ExceptionalRefundStatus.Approved
                && p.ReviewedAt >= fromUtc
                && p.ReviewedAt < toUtc
            );
}
