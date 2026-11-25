using Cinema.Application.Services.Ai;

namespace Cinema.Application.Interfaces.Ai;

public interface IAiModelClient
{
    bool IsConfigured { get; }

    Task<AiMessage> GenerateAsync(AiModelRequest request, CancellationToken cancellationToken);
}
