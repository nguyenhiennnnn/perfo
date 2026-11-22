using Cinema.Application.DTOs.Common;

namespace Cinema.Application.Interfaces;

public interface ICurrentActorAccessor
{
    ActorContext? Current { get; }
}
