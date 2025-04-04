using System.Linq.Expressions;
using ApiApplication.Database.Entities;
using ApiApplication.Database.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ApiApplication.Database.Repositories
{
    //public class ShowtimesRepository : IShowtimesRepository
    //{
    //    private readonly CinemaContext _context;

    //    public ShowtimesRepository(CinemaContext context)
    //    {
    //        _context = context;
    //    }

    //    public async Task<ShowtimeEntity> CreateShowtimeAsync(string movieId, int auditoriumId, DateTime startTime, MovieData movieData, CancellationToken cancel)
    //    {
    //        var auditorium = await _context.Auditoriums.FindAsync(auditoriumId);
    //        if (auditorium == null)
    //            throw new ArgumentException("Auditorium not found");

    //        var showtime = new ShowtimeEntity
    //        {
    //            MovieId = movieId,
    //            AuditoriumId = auditoriumId,
    //            StartTime = startTime,
    //            MovieTitle = movieData.Title,
    //            // Add other movie data fields as needed
    //        };

    //        _context.Showtimes.Add(showtime);
    //        await _context.SaveChangesAsync(cancel);

    //        return showtime;
    //    }

    //    public async Task<IEnumerable<ShowtimeEntity>> GetAllAsync(Expression<Func<ShowtimeEntity, bool>> filter, CancellationToken cancel)
    //    {
    //        return await _context.Showtimes
    //            .Include(x => x.Auditorium)
    //            .Where(filter)
    //            .ToListAsync(cancel);
    //    }

    //    public async Task<ShowtimeEntity> GetWithMoviesByIdAsync(int id, CancellationToken cancel)
    //    {
    //        return await _context.Showtimes
    //            .Include(x => x.Auditorium)
    //            .FirstOrDefaultAsync(x => x.Id == id, cancel);
    //    }

    //    public async Task<ShowtimeEntity> GetWithTicketsByIdAsync(int id, CancellationToken cancel)
    //    {
    //        return await _context.Showtimes
    //            .Include(x => x.Auditorium)
    //            .Include(x => x.Tickets)
    //                .ThenInclude(x => x.Seats)
    //            .FirstOrDefaultAsync(x => x.Id == id, cancel);
    //    }

    //    public async Task<ShowtimeEntity> GetShowtimeAsync(int id, CancellationToken cancel)
    //    {
    //        return await _context.Showtimes
    //            .Include(x => x.Auditorium)
    //            .FirstOrDefaultAsync(x => x.Id == id, cancel);
    //    }
    //}








    //----


    public class ShowtimesRepository : IShowtimesRepository
    {
        private readonly CinemaContext _context;

        public ShowtimesRepository(CinemaContext context)
        {
            _context = context;
        }

        public async Task<ShowtimeEntity> GetWithMoviesByIdAsync(int id, CancellationToken cancel)
        {
            return await _context.Showtimes
                .Include(x => x.Movie)
                .FirstOrDefaultAsync(x => x.Id == id, cancel);
        }

        public async Task<ShowtimeEntity> GetWithTicketsByIdAsync(int id, CancellationToken cancel)
        {
            return await _context.Showtimes
                .Include(x => x.Tickets)
                .FirstOrDefaultAsync(x => x.Id == id, cancel);
        }

        public async Task<IEnumerable<ShowtimeEntity>> GetAllAsync(Expression<Func<ShowtimeEntity, bool>> filter, CancellationToken cancel)
        {
            if (filter == null)
            {
                return await _context.Showtimes
                .Include(x => x.Movie)
                .ToListAsync(cancel);
            }
            return await _context.Showtimes
                .Include(x => x.Movie)
                .Where(filter)
                .ToListAsync(cancel);
        }

        public async Task<ShowtimeEntity> CreateShowtime(ShowtimeEntity showtimeEntity, CancellationToken cancel)
        {
            var movie = await _context.Movies
                .FirstOrDefaultAsync(x => x.ImdbId == showtimeEntity.Movie.ImdbId, cancel);

            if (movie == null)
            {
                // Add the new movie to the context
                await _context.Movies.AddAsync(showtimeEntity.Movie, cancel);
            }
            else
            {
                // Use the existing movie's Id
                showtimeEntity.Movie = movie;
            }

            var showtime = await _context.Showtimes.AddAsync(showtimeEntity, cancel);
            await _context.SaveChangesAsync(cancel);
            return showtime.Entity;
        }

    }
}


