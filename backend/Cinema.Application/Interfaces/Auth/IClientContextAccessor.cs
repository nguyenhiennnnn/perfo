using Cinema.Application.DTOs.Common;

namespace Cinema.Application.Interfaces.Auth;

public interface IClientContextAccessor
{
    ClientContext Current { get; }
}
