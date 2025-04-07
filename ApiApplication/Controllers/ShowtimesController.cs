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
    private readonly IValidator<CreateShowtimeRequest> _createShowtimeRequestValidator;
    private readonly IValidator<ReserveSeatsRequest> _reserveSeatsValidator;

    public ShowtimesController(
        IShowtimesRepository showtimesRepository,
        ITicketsRepository ticketsRepository,
        IAuditoriumsRepository auditoriumsRepository,
        ICacheService cacheService,
        IProvidedApiClient providedApiClient,
        ILogger<ShowtimesController> logger,
        IValidator<CreateShowtimeRequest> validator,
        IValidator<ReserveSeatsRequest> reserveSeatsValidator)
    {
        _showtimesRepository = showtimesRepository;
        _ticketsRepository = ticketsRepository;
        _auditoriumsRepository = auditoriumsRepository;
        _cacheService = cacheService;
        _providedApiClient = providedApiClient;
        _logger = logger;
        _createShowtimeRequestValidator = validator;
        _reserveSeatsValidator = reserveSeatsValidator;
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
    public async Task<IActionResult> CreateShowtime([FromBody] CreateShowtimeRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Validate the request
            var validationContext = new ValidationContext<CreateShowtimeRequest>(request);
            var validationResult = await _createShowtimeRequestValidator.ValidateAsync(validationContext);

            if (!validationResult.IsValid)
            {
                var validationErrors = validationResult.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.ErrorMessage).ToArray()
                    );

                return ValidationProblem(
                    new ValidationProblemDetails(validationErrors)
                    {
                        Type = "https://api.cinema.com/errors/validation",
                        Title = "Validation Error",
                        Status = StatusCodes.Status400BadRequest,
                        Detail = "One or more validation errors occurred."
                    }
                    );
            }

            if (validationContext.RootContextData["MovieData"] is not showResponse movieData)
            {
                return Problem(
                    type: "https://api.cinema.com/errors/movie-not-found",
                    title: "Movie Not Found",
                    detail: "The specified movie could not be found in the external API.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var show = new ShowtimeEntity()
            {
                AuditoriumId = request.AuditoriumId,
                Movie = new MovieEntity()
                {
                    ImdbId = movieData.Id,
                    ReleaseDate = new DateTime(int.Parse(movieData.Year), 1, 1),
                    Stars = movieData.Crew,
                    Title = movieData.Title,
                },
                SessionDate = request.StartTime,
            };

            var showtime = await _showtimesRepository.CreateShowtime(show, CancellationToken.None);

            stopwatch.Stop();
            _logger.LogInformation($"CreateShowtime took {stopwatch.ElapsedMilliseconds}ms");

            // Return 201 Created with the location header
            //return CreatedAtAction(
            //    "nameof(GetShowtime)", // Assuming you have a GetShowtime action
            //    new { id = showtime.Id },
            //    new
            //    {
            //        Id = showtime.Id,
            //        Movie = showtime.Movie.Title,
            //        StartTime = showtime.SessionDate,
            //        AuditoriumId = showtime.AuditoriumId
            //    });
            return Ok(new
            {
                showtime.Id,
                Movie = new
                {
                    showtime.Movie.Id,
                    showtime.Movie.Title,
                    showtime.Movie.ImdbId,
                    showtime.Movie.Stars,
                    showtime.Movie.ReleaseDate
                },
                StartTime = showtime.SessionDate,
                showtime.AuditoriumId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating showtime");
            return Problem(
                type: "https://api.cinema.com/errors/internal",
                title: "Internal Server Error",
                detail: "An unexpected error occurred while creating the showtime.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("reserve")]
    public async Task<IActionResult> ReserveSeats([FromBody] ReserveSeatsRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Validate the request
            var validationContext = new ValidationContext<ReserveSeatsRequest>(request);
            var validationResult = await _reserveSeatsValidator.ValidateAsync(validationContext);

            if (!validationResult.IsValid)
            {
                var validationErrors = validationResult.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.ErrorMessage).ToArray()
                    );

                return ValidationProblem(
                    new ValidationProblemDetails(validationErrors)
                    {
                        Type = "https://api.cinema.com/errors/validation",
                        Title = "Validation Error",
                        Status = StatusCodes.Status400BadRequest,
                        Detail = "One or more validation errors occurred."
                    });
            }

            // Get the showtime from validation context
            if (validationContext.RootContextData["Showtime"] is not ShowtimeEntity showtime)
            {
                return Problem(
                    type: "https://api.cinema.com/errors/showtime-not-found",
                    title: "Showtime Not Found",
                    detail: "The specified showtime could not be found in the external API.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            // Create reservation
            var reservation = await _ticketsRepository.CreateReservationAsync(
                request.ShowtimeId,
                request.Seats,
                TimeSpan.FromMinutes(10),
                CancellationToken.None);

            var reservationResponse = new ReservationResponse
            {
                Guid = reservation.Id,
                AuditoriumId = showtime.AuditoriumId, // Using stored showtime data
                Seats = [.. reservation.Seats.Select(x => new Seat { Row = x.Row, SeatNumber = x.SeatNumber })],
                Movie = showtime.Movie.Title // Using stored showtime data
            };

            stopwatch.Stop();
            _logger.LogInformation($"ReserveSeats took {stopwatch.ElapsedMilliseconds}ms");

            return CreatedAtAction(
                nameof(GetReservation),
                new { reservationId = reservation.Id },
                reservationResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving seats");
            return Problem(
                type: "https://api.cinema.com/errors/internal",
                title: "Internal Server Error",
                detail: "An unexpected error occurred while reserving the seats.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("reservations/{reservationId}")]
    public async Task<IActionResult> GetReservation(Guid reservationId)
    {
        try
        {
            var reservation = await _ticketsRepository.GetReservationAsync(reservationId, CancellationToken.None);

            if (reservation == null)
            {
                return Problem(
                    type: "https://api.cinema.com/errors/not-found",
                    title: "Reservation Not Found",
                    detail: $"Reservation with ID {reservationId} was not found.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            if (IsReservationExpired(reservation.CreatedTime))
            {
                return Problem(
                    type: "https://api.cinema.com/errors/expired",
                    title: "Reservation Expired",
                    detail: "The reservation has expired.",
                    statusCode: StatusCodes.Status410Gone);
            }

            var response = new ReservationResponse
            {
                Guid = reservation.Id,
                AuditoriumId = reservation.Showtime.AuditoriumId,
                Seats = [.. reservation.Seats.Select(x => new Seat { Row = x.Row, SeatNumber = x.SeatNumber })],
                Movie = reservation.Showtime.Movie.Title
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservation");
            return Problem(
                type: "https://api.cinema.com/errors/internal",
                title: "Internal Server Error",
                detail: "An unexpected error occurred while retrieving the reservation.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static bool IsReservationExpired(DateTime createdTime)
    {
        return DateTime.UtcNow > createdTime.AddMinutes(10);
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
