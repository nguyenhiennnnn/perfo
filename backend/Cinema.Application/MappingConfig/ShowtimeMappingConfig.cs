using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Mapster;

namespace Cinema.Application.MappingConfig;

public class ShowtimeMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<Showtime, AdminShowtimeDto>()
            .Map(dest => dest.MovieTitle, src => src.Movie.Title)
            .Map(dest => dest.RoomName, src => src.Room.RoomName)
            .Map(dest => dest.Status, src => src.Status.ToDtoStatus());

        config
            .NewConfig<Showtime, WholeCinemaShowtimeSlotDto>()
            .Map(dest => dest.RoomName, src => src.Room.RoomName)
            .Map(
                dest => dest.ScreenType,
                src => src.Room.ScreenType != null ? src.Room.ScreenType.Name : null
            )
            .Map(dest => dest.StartTime, src => src.StartTime)
            .Map(dest => dest.EndTime, src => src.EndTime)
            .Map(dest => dest.Status, src => src.Status.ToDtoStatus());

        config
            .NewConfig<Showtime, ShowtimeCalendarItemDto>()
            .Map(dest => dest.MovieTitle, src => src.Movie.Title)
            .Map(dest => dest.MoviePosterUrl, src => src.Movie.PosterUrl)
            .Map(dest => dest.RoomName, src => src.Room.RoomName)
            .Map(
                dest => dest.ScreenType,
                src => src.Room.ScreenType != null ? src.Room.ScreenType.Name : null
            )
            .Map(dest => dest.Status, src => src.Status.ToDtoStatus());
    }
}
