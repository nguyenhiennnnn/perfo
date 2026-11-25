using Cinema.Application.DTOs.Waitlist;

namespace Cinema.Application.Interfaces.Waitlist;

public interface IWaitlistService
{
    Task<WaitlistEntryDto> RegisterAsync(
        int userId,
        int showtimeId,
        RegisterWaitlistRequest request,
        CancellationToken cancellationToken
    );

    Task<WaitlistEntryDto> WithdrawAsync(
        int userId,
        int showtimeId,
        CancellationToken cancellationToken
    );

    Task<WaitlistEntryDto> GetMineAsync(
        int userId,
        int showtimeId,
        CancellationToken cancellationToken
    );
}
