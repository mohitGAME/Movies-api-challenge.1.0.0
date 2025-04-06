using System.Diagnostics;
using ApiApplication.Database.Entities;
using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.DTOs;
using ApiApplication.DTOs.Common;
using ApiApplication.Services;
using FluentValidation;
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
    private readonly IValidator<CreateShowtimeRequest> _validator;

    public ShowtimesController(
        IShowtimesRepository showtimesRepository,
        ITicketsRepository ticketsRepository,
        IAuditoriumsRepository auditoriumsRepository,
        ICacheService cacheService,
        IProvidedApiClient providedApiClient,
        ILogger<ShowtimesController> logger,
        IValidator<CreateShowtimeRequest> validator)
    {
        _showtimesRepository = showtimesRepository;
        _ticketsRepository = ticketsRepository;
        _auditoriumsRepository = auditoriumsRepository;
        _cacheService = cacheService;
        _providedApiClient = providedApiClient;
        _logger = logger;
        _validator = validator;
    }

    //[HttpGet]
    //public async Task<showListResponse> GetTest()
    //{

    //    var grpc = new ApiClientGrpc();
    //    var dd = await grpc.GetAll();


    //    var dds = await _providedApiClient.GetMovieAsync(dd.Shows.FirstOrDefault().FullTitle);

    //    Results.Problem("Error", statusCode: 500, title: "Error", type: "https://example.com/error");
    //    return dd;
    //}


    [HttpPost]
    public async Task<ActionResult<ApiResponse<string>>> CreateShowtime([FromBody] CreateShowtimeRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var validationContext = new ValidationContext<CreateShowtimeRequest>(request);
            var validationResult = await _validator.ValidateAsync(validationContext);

            if (validationResult.IsValid)
            {
                var movieData = validationContext.RootContextData["MovieData"] as showResponse;
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
            }
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

    [HttpPost("reserve")]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> ReserveSeats([FromBody] ReserveSeatsRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {

            // Create reservation
            var reservation = await _ticketsRepository.CreateReservationAsync(
                request.ShowtimeId,
                request.Seats,
                TimeSpan.FromMinutes(10), CancellationToken.None);


            var reservationResponse = new ReservationResponse()
            {
                Guid = reservation.Id,
                AuditoriumId = reservation.Showtime.AuditoriumId,
                Seats = [.. reservation.Seats.Select(x => new Seat { Row = x.Row, SeatNumber = x.SeatNumber })],
                Movie = reservation.Showtime.Movie.Title,
            };

            stopwatch.Stop();
            _logger.LogInformation($"ReserveSeats took {stopwatch.ElapsedMilliseconds}ms");
            return Ok(ApiResponse<ReservationResponse>.SuccessResponse(reservationResponse, "Seats reserved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving seats");
            return StatusCode(500, "An error occurred while reserving seats");
        }
    }

    [HttpPost("reservations/{reservationId}/confirm")]
    public async Task<ActionResult<ApiResponse<BuySeatsResponseDto>>> ConfirmReservation(Guid reservationId)
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
