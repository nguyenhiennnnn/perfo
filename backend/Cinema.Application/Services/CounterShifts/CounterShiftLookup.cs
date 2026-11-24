using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.CounterShifts;

public static class CounterShiftLookup
{
    public static Task<int?> FindOpenCounterShiftIdByUserAsync(
        this IAppDbContext db,
        int userId,
        CancellationToken cancellationToken
    ) =>
        db.CounterShifts.AsNoTracking()
            .Where(s => s.Status == CounterShiftStatus.Open && s.Employee.UserId == userId)
            .Select(s => (int?)s.CounterShiftId)
            .FirstOrDefaultAsync(cancellationToken);

    public static async Task<int> RequireOpenCounterShiftIdByUserAsync(
        this IAppDbContext db,
        int userId,
        CancellationToken cancellationToken
    ) =>
        await db.FindOpenCounterShiftIdByUserAsync(userId, cancellationToken)
        ?? throw new BusinessRuleException(Messages.Rule_CounterShiftRequired);

    public static async Task<int> RequireOpenCounterShiftIdByEmployeeAsync(
        this IAppDbContext db,
        int employeeId,
        CancellationToken cancellationToken
    ) =>
        await db
            .CounterShifts.AsNoTracking()
            .Where(s => s.Status == CounterShiftStatus.Open && s.EmployeeId == employeeId)
            .Select(s => (int?)s.CounterShiftId)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new BusinessRuleException(Messages.Rule_CounterShiftRequired);
}
