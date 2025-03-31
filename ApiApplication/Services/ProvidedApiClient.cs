using System.Net.Http.Json;
using Grpc.Core;
using Grpc.Net.Client;
using Movies;

namespace ApiApplication.Services;

public interface IProvidedApiClient
{
    Task<MovieData> GetMovieAsync(string movieId);
}

public class ProvidedApiClient : IProvidedApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProvidedApiClient> _logger;
    private readonly GrpcChannel _grpcChannel;
    private readonly bool _useGrpc;

    public ProvidedApiClient(
        HttpClient httpClient, 
        ILogger<ProvidedApiClient> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("http://localhost:7172/");

        // Configure GRPC
        _useGrpc = configuration.GetValue<bool>("UseGrpc", false);
        if (_useGrpc)
        {
            var handler = new HttpClientHandler();
            // Allow untrusted certificates for development
            handler.ServerCertificateCustomValidationCallback = 
                HttpClientHandler.DangerousAcceptAnyServerCertificate;

            _grpcChannel = GrpcChannel.ForAddress("https://localhost:7443", new GrpcChannelOptions
            {
                HttpHandler = handler
            });
        }
    }

    public async Task<MovieData> GetMovieAsync(string movieId)
    {
        try
        {
            if (_useGrpc)
            {
                return await GetMovieViaGrpc(movieId);
            }
            return await GetMovieViaHttp(movieId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching movie data");
            throw;
        }
    }

    private async Task<MovieData> GetMovieViaGrpc(string movieId)
    {
        var client = new Movies.MoviesService.MoviesServiceClient(_grpcChannel);
        var request = new MovieRequest { Id = movieId };
        
        try
        {
            var response = await client.GetMovieAsync(request);
            return new MovieData
            {
                Id = response.Id,
                Title = response.Title
                // Map other fields as needed
            };
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "GRPC call failed");
            throw;
        }
    }

    private async Task<MovieData> GetMovieViaHttp(string movieId)
    {
        var response = await _httpClient.GetAsync($"/api/movies/{movieId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MovieData>();
    }
} 