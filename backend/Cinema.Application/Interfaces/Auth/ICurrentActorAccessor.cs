using Cinema.Application.DTOs.Common;

namespace Cinema.Application.Interfaces.Auth;

public interface ICurrentActorAccessor
{
    ActorContext? Current { get; }
}
