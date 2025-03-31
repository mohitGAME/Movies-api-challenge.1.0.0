using ApiApplication.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using ApiApplication.Database.Repositories.Abstractions;

namespace ApiApplication.Database.Repositories
{
    public class TicketsRepository : ITicketsRepository
    {
        private readonly CinemaContext _context;

        public TicketsRepository(CinemaContext context)
        {
            _context = context;
        }

        public Task<TicketEntity> GetAsync(Guid id, CancellationToken cancel)
        {
            return _context.Tickets.FirstOrDefaultAsync(x => x.Id == id, cancel);
        }

        public async Task<IEnumerable<TicketEntity>> GetEnrichedAsync(int showtimeId, CancellationToken cancel)
        {
            return await _context.Tickets
                .Include(x => x.Showtime)
                .Include(x => x.Seats)
                .Where(x => x.ShowtimeId == showtimeId)
                .ToListAsync(cancel);
        }

        public async Task<TicketEntity> CreateAsync(ShowtimeEntity showtime, IEnumerable<SeatEntity> selectedSeats, CancellationToken cancel)
        {
            var ticket = _context.Tickets.Add(new TicketEntity
            {
                Showtime = showtime,
                Seats = new List<SeatEntity>(selectedSeats)
            });

            await _context.SaveChangesAsync(cancel);

            return ticket.Entity;
        }

        public async Task<TicketEntity> ConfirmPaymentAsync(TicketEntity ticket, CancellationToken cancel)
        {
            ticket.Paid = true;
            _context.Update(ticket);
            await _context.SaveChangesAsync(cancel);
            return ticket;
        }

        public async Task<IEnumerable<int>> GetAvailableSeatsAsync(int showtimeId, CancellationToken cancel)
        {
            var showtime = await _context.Showtimes
                .Include(x => x.Auditorium)
                .FirstOrDefaultAsync(x => x.Id == showtimeId, cancel);

            if (showtime == null)
                return Enumerable.Empty<int>();

            var totalSeats = showtime.Auditorium.Rows * showtime.Auditorium.SeatsPerRow;
            var reservedSeats = await _context.Tickets
                .Where(x => x.ShowtimeId == showtimeId && !x.IsExpired)
                .SelectMany(x => x.Seats)
                .Select(x => x.SeatNumber)
                .ToListAsync(cancel);

            return Enumerable.Range(1, totalSeats).Except(reservedSeats);
        }

        public async Task<TicketEntity> CreateReservationAsync(int showtimeId, IEnumerable<int> seatNumbers, TimeSpan expirationTime, CancellationToken cancel)
        {
            var showtime = await _context.Showtimes
                .Include(x => x.Auditorium)
                .FirstOrDefaultAsync(x => x.Id == showtimeId, cancel);

            if (showtime == null)
                throw new ArgumentException("Showtime not found");

            var seats = seatNumbers.Select(seatNumber => new SeatEntity
            {
                SeatNumber = seatNumber
            }).ToList();

            var ticket = new TicketEntity
            {
                ShowtimeId = showtimeId,
                Seats = seats,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expirationTime),
                Paid = false
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync(cancel);

            return ticket;
        }

        public async Task<TicketEntity> GetReservationAsync(Guid reservationId, CancellationToken cancel)
        {
            var ticket = await _context.Tickets
                .Include(x => x.Showtime)
                .Include(x => x.Seats)
                .FirstOrDefaultAsync(x => x.Id == reservationId, cancel);

            if (ticket == null)
                return null;

            if (DateTime.UtcNow > ticket.ExpiresAt)
            {
                ticket.IsExpired = true;
                await _context.SaveChangesAsync(cancel);
                return null;
            }

            return ticket;
        }

        public async Task<TicketEntity> ConfirmReservationAsync(Guid reservationId, CancellationToken cancel)
        {
            var ticket = await GetReservationAsync(reservationId, cancel);
            if (ticket == null)
                throw new ArgumentException("Reservation not found or expired");

            ticket.Paid = true;
            _context.Update(ticket);
            await _context.SaveChangesAsync(cancel);

            return ticket;
        }
    }
}
