using ApiApplication.Database.Entities;
using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.DTOs;
using Microsoft.EntityFrameworkCore;

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
                Seats = [.. selectedSeats]
            });

            await _context.SaveChangesAsync(cancel);

            return ticket.Entity;
        }
        public async Task<IEnumerable<Seat>> GetAvailableSeatsAsync(int showtimeId, CancellationToken cancel)
        {
            // Get showtime and auditorium in parallel
            var showtimeTask = _context.Showtimes
                .FirstOrDefaultAsync(x => x.Id == showtimeId, cancel);

            var showtimeAuditoriumIdTask = _context.Showtimes
                .Where(x => x.Id == showtimeId)
                .Select(x => x.AuditoriumId)
                .FirstOrDefaultAsync(cancel);

            await Task.WhenAll(showtimeTask, showtimeAuditoriumIdTask);

            var showtime = showtimeTask.Result;
            if (showtime == null)
                throw new ArgumentException("Showtime not found");

            var auditoriumId = showtimeAuditoriumIdTask.Result;

            // Process expired tickets
            var tenMinutesAgo = DateTime.UtcNow.AddMinutes(-10);
            var expiredTicketsQuery = _context.Tickets
                .Where(t => t.ShowtimeId == showtimeId && !t.Paid && t.CreatedTime <= tenMinutesAgo);

            var expiredTickets = await expiredTicketsQuery.ToListAsync(cancel);
            if (expiredTickets.Count != 0)
            {
                _context.Tickets.RemoveRange(expiredTickets);
                await _context.SaveChangesAsync(cancel);
            }

            var newAvailableSeats = await _context.Seats
                .Where(x => x.AuditoriumId == auditoriumId && x.TicketEntityId == null)
                .Select(s => new Seat { SeatNumber = s.SeatNumber, Row = s.Row })
                .ToListAsync(cancel);

            return newAvailableSeats;
        }

        private class SeatComparer : IEqualityComparer<Seat>
        {
            public bool Equals(Seat x, Seat y)
            {
                return x.Row == y.Row && x.SeatNumber == y.SeatNumber;
            }

            public int GetHashCode(Seat obj)
            {
                return HashCode.Combine(obj.Row, obj.SeatNumber);
            }
        }

        public async Task<TicketEntity> CreateReservationAsync(int showtimeId, IEnumerable<Seat> seatNumbers, TimeSpan expirationTime, CancellationToken cancel)
        {
            var showtime = await _context.Showtimes
                .Include(x => x.Movie)
                .FirstOrDefaultAsync(x => x.Id == showtimeId, cancel);

            var auditorium = await _context.Auditoriums
                .Include(x => x.Seats)
                .FirstOrDefaultAsync(a => a.Id == showtime.AuditoriumId, cancel);

            var ticket = new TicketEntity
            {
                ShowtimeId = showtimeId,
                Seats = auditorium.Seats
                    .Where(x => seatNumbers.Any(s => s.Row == x.Row && s.SeatNumber == x.SeatNumber))
                    .ToList(),
                Paid = false,
                Showtime = showtime
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

            //if (DateTime.UtcNow > ticket.ExpiresAt)
            //{
            //    ticket.IsExpired = true;
            //    await _context.SaveChangesAsync(cancel);
            //    return null;
            //}

            return ticket;
        }

        public async Task<TicketEntity> ConfirmReservationAsync(Guid reservationId, CancellationToken cancel)
        {
            var ticket = await GetReservationAsync(reservationId, cancel);

            if (ticket == null)
                throw new ArgumentException("Reservation not found");

            if (ticket.Paid)
                throw new ArgumentException("Ticket expired booked");

            if (IsReservationExpired(ticket.CreatedTime))
                throw new ArgumentException("Reservation has expired");

            ticket.Paid = true;
            _context.Update(ticket);
            await _context.SaveChangesAsync(cancel);

            return ticket;
        }

        public Task<TicketEntity> ConfirmPaymentAsync(TicketEntity ticket, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }


        private bool IsReservationExpired(DateTime createdTime)
        {
            var tenMinutesAgo = DateTime.UtcNow.AddMinutes(-10);
            return createdTime <= tenMinutesAgo;
        }


    }
}
