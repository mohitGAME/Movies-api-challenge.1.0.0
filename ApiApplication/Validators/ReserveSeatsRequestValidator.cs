using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.DTOs;
using FluentValidation;
using ProtoDefinitions;

namespace ApiApplication.Validators
{
    public class ReserveSeatsRequestValidator : AbstractValidator<ReserveSeatsRequest>
    {
        private readonly ITicketsRepository _ticketsRepository;

        public ReserveSeatsRequestValidator(ITicketsRepository ticketsRepository)
        {
            _ticketsRepository = ticketsRepository;

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
                .WithMessage("Seats must be contiguous")
                .MustAsync((request, seats, cancellationToken) => AreSeatsAvailable(seats, request.ShowtimeId, cancellationToken))
                .WithMessage("One or more seats are not available");
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

}
