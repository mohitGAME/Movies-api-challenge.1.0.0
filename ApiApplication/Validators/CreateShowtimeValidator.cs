using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.DTOs;
using ApiApplication.Services;
using FluentValidation;
using FluentValidation.Results;

namespace ApiApplication.Validators
{
    public class CreateShowtimeValidator : AbstractValidator<CreateShowtimeRequest>
    {
        private readonly IAuditoriumsRepository _auditoriumsRepository;
        private readonly IProvidedApiClient _providedApiClient;
        private readonly ICacheService _cacheService;


        public CreateShowtimeValidator(
            IAuditoriumsRepository auditoriumsRepository,
            IProvidedApiClient providedApiClient,
            ICacheService cacheService)
        {
            _auditoriumsRepository = auditoriumsRepository;
            _providedApiClient = providedApiClient;
            _cacheService = cacheService;

            ClassLevelCascadeMode = CascadeMode.Stop;
            // Basic input validation
            RuleFor(x => x.MovieName)
           .NotEmpty().WithMessage("Movie name is required");

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
                .CustomAsync(MovieExistsInApiAsync);
        }


        private async Task<bool> AuditoriumExistsAsync(int auditoriumId, CancellationToken cancellationToken)
        {
            return await _auditoriumsRepository.GetAsync(auditoriumId, cancellationToken) != null;
        }

        private async Task<bool> MovieExistsInApiAsync(string movieName, ValidationContext<CreateShowtimeRequest> context, CancellationToken cancellationToken)
        {
            try
            {
                ////Get movie data from provided API with caching
                var movieData = await _cacheService.GetOrSetAsync(
                   $"movie_{movieName}",
                   async () => await _providedApiClient.GetMovieAsync(movieName),
                   TimeSpan.FromHours(1));

                if (movieData == null)
                {
                    context.AddFailure(new ValidationFailure(
                        "MovieName",
                        "Movie not found in the database",
                        movieName)
                    {
                        ErrorCode = "MOVIE_NOT_FOUND"
                    });
                    return false;
                }
                // Store for later (e.g., for use in handler)
                context.RootContextData["MovieData"] = movieData;
                return true;
            }
            catch
            {
                context.AddFailure(new FluentValidation.Results.ValidationFailure(
                    "MovieName",
                    "Movie not found in the database",
                    movieName)
                {
                    ErrorCode = "MOVIE_NOT_FOUND"
                });
                return false;
            }
        }
    }


}
