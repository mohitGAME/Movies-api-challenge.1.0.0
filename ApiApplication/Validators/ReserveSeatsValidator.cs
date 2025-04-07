using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.DTOs;
using FluentValidation.Results;
using FluentValidation;

public class ReserveSeatsValidator : AbstractValidator<ReserveSeatsRequest>
{
    private readonly ITicketsRepository _ticketsRepository;
    private readonly IShowtimesRepository _showtimesRepository;

    public ReserveSeatsValidator(
        ITicketsRepository ticketsRepository,
        IShowtimesRepository showtimesRepository)
    {
        _ticketsRepository = ticketsRepository;
        _showtimesRepository = showtimesRepository;

        ClassLevelCascadeMode = CascadeMode.Stop;

        // Basic input validation
        RuleFor(x => x.ShowtimeId)
            .NotNull()
            .NotEmpty()
            .WithMessage("ShowtimeId is required")
            .GreaterThan(0)
            .WithMessage("Invalid showtime");

        RuleFor(x => x.Seats)
            .NotEmpty()
            .WithMessage("Seats are required")
            .Must(AreSeatsContiguous)
            .WithMessage("Seats must be contiguous");

        // Business rule validation
        RuleFor(x => x)
            .Cascade(CascadeMode.Stop)
            .CustomAsync(ValidateShowtimeAsync)
            .MustAsync((request, cancellationToken) => AreSeatsAvailable(request.Seats, request.ShowtimeId, cancellationToken))
            .WithMessage("One or more seats are not available");
    }

    private async Task ValidateShowtimeAsync(ReserveSeatsRequest request, ValidationContext<ReserveSeatsRequest> context, CancellationToken cancellationToken)
    {
        var showtime = await _showtimesRepository.GetWithMoviesByIdAsync(request.ShowtimeId, cancellationToken);

        if (showtime == null)
        {
            context.AddFailure(new ValidationFailure(
                nameof(ReserveSeatsRequest.ShowtimeId),
                "Showtime not found")
            {
                ErrorCode = "SHOWTIME_NOT_FOUND"
            });
            return;
        }

        // Check if showtime is in the past
        if (showtime.SessionDate < DateTime.UtcNow)
        {
            context.AddFailure(new ValidationFailure(
                nameof(ReserveSeatsRequest.ShowtimeId),
                "Cannot reserve seats for past showtimes")
            {
                ErrorCode = "SHOWTIME_IN_PAST"
            });
            return;
        }

        // Store showtime in context for later use
        context.RootContextData["Showtime"] = showtime;
    }

    private bool AreSeatsContiguous(IEnumerable<Seat> seats)
    {
        var sortedSeats = seats.OrderBy(x => x.Row).ThenBy(x => x.SeatNumber).ToList();
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

    private async Task<bool> AreSeatsAvailable(IEnumerable<Seat> seats, int showtimeId, CancellationToken cancellationToken)
    {
        var availableSeats = await _ticketsRepository.GetAvailableSeatsAsync(showtimeId, cancellationToken);
        return seats.All(seat => availableSeats.Any(a => a.Row == seat.Row && a.SeatNumber == seat.SeatNumber));
    }
}
