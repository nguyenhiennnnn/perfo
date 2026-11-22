namespace Cinema.Application.Interfaces;

public record AdminBookingListFilter(
    string? Status,
    string? Channel,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? Search
);

public interface IBookingExportService : IExportSource<AdminBookingListFilter>;
