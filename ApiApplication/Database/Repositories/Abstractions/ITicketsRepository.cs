using ApiApplication.Database.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ApiApplication.Database.Repositories.Abstractions
{
    public interface ITicketsRepository
    {
        Task<TicketEntity> ConfirmPaymentAsync(TicketEntity ticket, CancellationToken cancel);
        Task<TicketEntity> CreateAsync(ShowtimeEntity showtime, IEnumerable<SeatEntity> selectedSeats, CancellationToken cancel);
        Task<TicketEntity> GetAsync(Guid id, CancellationToken cancel);
        Task<IEnumerable<TicketEntity>> GetEnrichedAsync(int showtimeId, CancellationToken cancel);
        //Task<IEnumerable<int>> GetAvailableSeatsAsync(int showtimeId, CancellationToken cancel);
        //Task<TicketEntity> CreateReservationAsync(int showtimeId, IEnumerable<int> seatNumbers, TimeSpan expirationTime, CancellationToken cancel);
        Task<TicketEntity> GetReservationAsync(Guid reservationId, CancellationToken cancel);
        Task<TicketEntity> ConfirmReservationAsync(Guid reservationId, CancellationToken cancel);
    }
}