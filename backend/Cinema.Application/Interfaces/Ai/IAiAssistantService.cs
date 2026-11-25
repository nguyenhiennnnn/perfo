using Cinema.Application.DTOs.Ai;

namespace Cinema.Application.Interfaces.Ai;

public interface IAiAssistantService
{
    Task<AiChatResponse> ChatAsync(
        int? userId,
        AiChatRequest request,
        CancellationToken cancellationToken
    );
}
