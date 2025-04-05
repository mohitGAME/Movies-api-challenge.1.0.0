using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.DTOs;
using ApiApplication.Services;
using FluentValidation;

namespace ApiApplication.Validators
{
    public class CreateShowtimeValidator : AbstractValidator<CreateShowtimeRequest>
    {
        private readonly IAuditoriumsRepository _auditoriumsRepository;
        private readonly IProvidedApiClient _providedApiClient;

        public CreateShowtimeValidator(
            IAuditoriumsRepository auditoriumsRepository,
            IProvidedApiClient providedApiClient)
        {
            _auditoriumsRepository = auditoriumsRepository;
            _providedApiClient = providedApiClient;

            // Basic input validation
            RuleFor(x => x.MovieName)
                .NotEmpty().WithMessage("Movie name is required")
                .MaximumLength(200).WithMessage("Movie name cannot exceed 200 characters");

            RuleFor(x => x.AuditoriumId)
                .GreaterThan(0).WithMessage("Auditorium ID must be a positive number");

            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("Start time is required");
                //.GreaterThan(DateTime.UtcNow).WithMessage("Start time must be in the future");

            // Business rule validations
            RuleFor(x => x.AuditoriumId)
                .MustAsync(AuditoriumExistsAsync).WithMessage("Specified auditorium does not exist")
                .WithErrorCode("INVALID_AUDITORIUM");

            RuleFor(x => x.MovieName)
                .MustAsync(MovieExistsInApiAsync).WithMessage("Movie not found in the database")
                .WithErrorCode("MOVIE_NOT_FOUND");
        }

        private async Task<bool> AuditoriumExistsAsync(int auditoriumId, CancellationToken cancellationToken)
        {
            return await _auditoriumsRepository.GetAsync(auditoriumId, cancellationToken) != null;
        }

        private async Task<bool> MovieExistsInApiAsync(string movieName, CancellationToken cancellationToken)
        {
            try
            {
                var movie = await _providedApiClient.GetMovieAsync(movieName);
                return movie != null;
            }
            catch
            {
                return false;
            }
        }

    }


}
