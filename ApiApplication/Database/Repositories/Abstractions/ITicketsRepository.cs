using ApiApplication.Database.Entities;
using ApiApplication.DTOs;

namespace ApiApplication.Database.Repositories.Abstractions
{
    public interface ITicketsRepository
    {
        Task<IEnumerable<Seat>> GetAvailableSeatsAsync(int showtimeId, CancellationToken cancel);
        Task<TicketEntity> CreateReservationAsync(int showtimeId, IEnumerable<Seat> seatNumbers, TimeSpan expirationTime, CancellationToken cancel);
        Task<TicketEntity> GetReservationAsync(Guid reservationId, CancellationToken cancel);
        Task<TicketEntity> ConfirmReservationAsync(Guid reservationId, CancellationToken cancel);
        Task<TicketEntity> ConfirmPaymentAsync(TicketEntity ticket, CancellationToken cancel);
        Task<TicketEntity> CreateAsync(ShowtimeEntity showtime, IEnumerable<SeatEntity> selectedSeats, CancellationToken cancel);
        Task<TicketEntity> GetAsync(Guid id, CancellationToken cancel);
        Task<IEnumerable<TicketEntity>> GetEnrichedAsync(int showtimeId, CancellationToken cancel);

    }
}