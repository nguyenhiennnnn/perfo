using Cinema.Application.Interfaces.Abuse;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Abuse;

public class CustomerDeviceRecorder(IAppDbContext db, IClientContextAccessor clientContext)
    : ICustomerDeviceRecorder
{
    public async Task RecordAsync(int userId, CancellationToken cancellationToken)
    {
        var deviceId = clientContext.Current.DeviceId;
        if (string.IsNullOrWhiteSpace(deviceId))
            return;

        var now = DateTime.UtcNow;
        var updated = await db
            .CustomerDevices.Where(d => d.UserId == userId && d.DeviceId == deviceId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(d => d.LastSeenAt, now),
                cancellationToken
            );
        if (updated > 0)
            return;

        var entity = new CustomerDevice
        {
            UserId = userId,
            DeviceId = deviceId,
            FirstSeenAt = now,
            LastSeenAt = now,
        };
        db.CustomerDevices.Add(entity);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.CustomerDevices.Remove(entity);
        }
    }
}
