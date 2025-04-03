using System.Diagnostics;
using ApiApplication.Database.Entities;
using ApiApplication.Database.Repositories.Abstractions;
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

            await _cacheService.SetAsync("test", "rocket");


            //var ss = await _cacheService.GetAsync<string>("test");

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
                    Stars = movieData.ImDbRating,
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

    //[HttpPost("{showtimeId}/reserve")]
    //public async Task<IActionResult> ReserveSeats(int showtimeId, [FromBody] ReserveSeatsRequest request)
    //{
    //    var stopwatch = Stopwatch.StartNew();
    //    try
    //    {
    //        // Validate showtime exists
    //        var showtime = await _showtimesRepository.GetShowtimeAsync(showtimeId, CancellationToken.None);
    //        if (showtime == null)
    //            return NotFound("Showtime not found");

    //        // Validate seats are contiguous
    //        if (!AreSeatsContiguous(request.SeatNumbers))
    //            return BadRequest("Seats must be contiguous");

    //        // Check if seats are available
    //        var availableSeats = await _ticketsRepository.GetAvailableSeatsAsync(showtimeId, CancellationToken.None);
    //        if (!request.SeatNumbers.All(seat => availableSeats.Contains(seat)))
    //            return BadRequest("One or more seats are not available");

    //        // Create reservation
    //        var reservation = await _ticketsRepository.CreateReservationAsync(
    //            showtimeId,
    //            request.SeatNumbers,
    //            TimeSpan.FromMinutes(10),CancellationToken.None);

    //        stopwatch.Stop();
    //        _logger.LogInformation($"ReserveSeats took {stopwatch.ElapsedMilliseconds}ms");
    //        return Ok(reservation);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error reserving seats");
    //        return StatusCode(500, "An error occurred while reserving seats");
    //    }
    //}

    //[HttpPost("reservations/{reservationId}/confirm")]
    //public async Task<IActionResult> ConfirmReservation(Guid reservationId)
    //{
    //    var stopwatch = Stopwatch.StartNew();
    //    try
    //    {
    //        var reservation = await _ticketsRepository.GetReservationAsync(reservationId, CancellationToken.None);
    //        if (reservation == null)
    //            return NotFound("Reservation not found");

    //        if (reservation.IsExpired)
    //            return BadRequest("Reservation has expired");

    //        var ticket = await _ticketsRepository.ConfirmReservationAsync(reservationId, CancellationToken.None);

    //        stopwatch.Stop();
    //        _logger.LogInformation($"ConfirmReservation took {stopwatch.ElapsedMilliseconds}ms");
    //        return Ok(ticket);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error confirming reservation");
    //        return StatusCode(500, "An error occurred while confirming the reservation");
    //    }
    //}

    private bool AreSeatsContiguous(IEnumerable<int> seatNumbers)
    {
        var sortedSeats = seatNumbers.OrderBy(x => x).ToList();
        for (int i = 1; i < sortedSeats.Count; i++)
        {
            if (sortedSeats[i] != sortedSeats[i - 1] + 1)
                return false;
        }
        return true;
    }
}

public class CreateShowtimeRequest
{
    public string MovieName { get; set; }

    public int AuditoriumId { get; set; }
    public DateTime StartTime { get; set; }
}

public class ReserveSeatsRequest
{
    public List<int> SeatNumbers { get; set; }
}

public class MovieData
{
    public string Id { get; set; }
    public string Title { get; set; }
    // Add other required fields
}