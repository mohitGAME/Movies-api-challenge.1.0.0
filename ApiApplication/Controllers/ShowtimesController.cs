using System.Diagnostics;
using ApiApplication.Database.Entities;
using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.DTOs;
using ApiApplication.Services;
using Microsoft.AspNetCore.Mvc;
using ProtoDefinitions;

namespace ApiApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShowtimesController : ControllerBase
{
    private readonly IShowtimesRepository _showtimesRepository;
    private readonly ITicketsRepository _ticketsRepository;
    private readonly IAuditoriumsRepository _auditoriumsRepository;
    private readonly ICacheService _cacheService;
    private readonly IProvidedApiClient _providedApiClient;
    private readonly ILogger<ShowtimesController> _logger;

    public ShowtimesController(
        IShowtimesRepository showtimesRepository,
        ITicketsRepository ticketsRepository,
        IAuditoriumsRepository auditoriumsRepository,
        ICacheService cacheService,
        IProvidedApiClient providedApiClient,
        ILogger<ShowtimesController> logger)
    {
        _showtimesRepository = showtimesRepository;
        _ticketsRepository = ticketsRepository;
        _auditoriumsRepository = auditoriumsRepository;
        _cacheService = cacheService;
        _providedApiClient = providedApiClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<showListResponse> GetTest()
    {

        var grpc = new ApiClientGrpc();
        var dd = await grpc.GetAll();


        var dds = await _providedApiClient.GetMovieAsync(dd.Shows.FirstOrDefault().FullTitle);
        return dd;
    }


    [HttpPost]
    public async Task<IActionResult> CreateShowtime([FromBody] CreateShowtimeRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            //Get movie data from provided API with caching
            var movieData = await _cacheService.GetOrSetAsync(
            $"movie_{request.MovieName}",
            async () =>
            {
                var movie = await _providedApiClient.GetMovieAsync(request.MovieName);
                if (movie == null)
                {
                    throw new Exception("Movie data is null");
                }
                return movie;
            },
            TimeSpan.FromHours(1));
            var show = new ShowtimeEntity()
            {
                AuditoriumId = request.AuditoriumId,
                Movie = new MovieEntity()
                {
                    ImdbId = movieData.Id,
                    // Obv: Movie api does not have release date, add  
                    ReleaseDate = new DateTime(int.Parse(movieData.Year), 1, 1),
                    Stars = movieData.Crew,
                    Title = movieData.Title,

                },
                SessionDate = request.StartTime,
            };
            var showtime = await _showtimesRepository.CreateShowtime(show, CancellationToken.None);


            stopwatch.Stop();
            _logger.LogInformation($"CreateShowtime took {stopwatch.ElapsedMilliseconds}ms");
            return Ok("showtime");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating showtime");
            return StatusCode(500, "An error occurred while creating the showtime");
        }
    }

    [HttpPost("{showtimeId}/reserve")]
    public async Task<IActionResult> ReserveSeats(int showtimeId, [FromBody] ReserveSeatsRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Validate seats are contiguous
            if (!AreSeatsContiguous(request.Seats))
                return BadRequest("Seats must be contiguous");

            // Check if seats are available // handled below : if passed seat does not exist
            var availableSeats = await _ticketsRepository.GetAvailableSeatsAsync(showtimeId, CancellationToken.None);
            if (!request.Seats.All(seat => availableSeats.Any(a => a.Row == seat.Row && a.SeatNumber == seat.SeatNumber)))
                return BadRequest("One or more seats are not available");

            // Create reservation
            var reservation = await _ticketsRepository.CreateReservationAsync(
                showtimeId,
                request.Seats,
                TimeSpan.FromMinutes(10), CancellationToken.None);


            var reservationResponse = new ReservationResponse()
            {
                Guid = reservation.Id,
                AuditoriumId = reservation.Showtime.AuditoriumId,
                Seats = reservation.Seats.Select(x => new Seat { Row = x.Row, SeatNumber = x.SeatNumber }).ToList(),
                Movie = reservation.Showtime.Movie.Title,
            };

            stopwatch.Stop();
            _logger.LogInformation($"ReserveSeats took {stopwatch.ElapsedMilliseconds}ms");
            return Ok(reservationResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving seats");
            return StatusCode(500, "An error occurred while reserving seats");
        }
    }

    [HttpPost("reservations/{reservationId}/confirm")]
    public async Task<IActionResult> ConfirmReservation(Guid reservationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var ticket = await _ticketsRepository.ConfirmReservationAsync(reservationId, CancellationToken.None);
            var buySeatsResponse = new BuySeatsResponseDto()
            {
                Message = "Booking Confirmed",
                PurchasedSeats = [.. ticket.Seats.Select(s => new Seat { SeatNumber = s.SeatNumber, Row = s.Row })],
                ReservationId = ticket.Id,
                PurchaseTime = DateTime.UtcNow,
            };
            stopwatch.Stop();
            _logger.LogInformation($"ConfirmReservation took {stopwatch.ElapsedMilliseconds}ms");
            return Ok(buySeatsResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming reservation");
            return StatusCode(500, "An error occurred while confirming the reservation");
        }
    }
    private bool AreSeatsContiguous(IEnumerable<Seat> seatNumbers)
    {
        var sortedSeats = seatNumbers.OrderBy(x => x.Row).ThenBy(x => x.SeatNumber).ToList();
        for (int i = 1; i < sortedSeats.Count; i++)
        {
            if (sortedSeats[i].Row == sortedSeats[i - 1].Row)
            {
                if (sortedSeats[i].SeatNumber != sortedSeats[i - 1].SeatNumber + 1)
                    return false;
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    public class BuySeatsResponseDto
    {
        public Guid ReservationId { get; set; }
        public string Message { get; set; }
        public DateTime PurchaseTime { get; set; }
        public List<Seat> PurchasedSeats { get; set; }
    }

    //private int GetLastSeatNumberInRow(int row)
    //{
    //    // This method should return the last seat number in the given row.
    //    // You need to implement this method based on your auditorium's seating arrangement.
    //    // For example, if each row has 10 seats, you can return 10.

    //    return 10; // Example value, replace with actual logic.
    //}
}
