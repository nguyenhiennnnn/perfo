using Cinema.Application.DTOs.Auth;
using Cinema.Application.DTOs.Users;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces;

public interface ICommunicationPreferenceService
{
    Task<CommunicationPreferencesDto> GetAsync(int userId, CancellationToken cancellationToken);

    Task<CommunicationPreferencesDto> UpdateAsync(
        int userId,
        UpdateCommunicationPreferencesRequest request,
        CancellationToken cancellationToken
    );

    Task<MessageResponse> UnsubscribeMarketingAsync(
        string token,
        CancellationToken cancellationToken
    );

    Task<bool> CanSendAsync(
        int userId,
        EmailCategory category,
        CancellationToken cancellationToken
    );
}
