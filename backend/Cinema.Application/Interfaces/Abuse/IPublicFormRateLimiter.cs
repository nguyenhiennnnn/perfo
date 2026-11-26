namespace Cinema.Application.Interfaces.Abuse;

public interface IPublicFormRateLimiter
{
    Task EnsureAllowedAsync(string form, CancellationToken cancellationToken);
}
