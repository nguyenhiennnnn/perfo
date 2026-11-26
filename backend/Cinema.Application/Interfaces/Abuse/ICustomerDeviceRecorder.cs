namespace Cinema.Application.Interfaces.Abuse;

public interface ICustomerDeviceRecorder
{
    Task RecordAsync(int userId, CancellationToken cancellationToken);
}
