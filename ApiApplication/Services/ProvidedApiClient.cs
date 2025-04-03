using ApiApplication.Controllers;
using ApiApplication.Database.Entities;
using Grpc.Core;
using Grpc.Net.Client;
using ProtoDefinitions;
using static ProtoDefinitions.MoviesApi;

namespace ApiApplication.Services;

public interface IProvidedApiClient
{
    Task<showResponse> GetMovieAsync(string movieId);
}

public class ProvidedApiClient : IProvidedApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProvidedApiClient> _logger;
    private readonly GrpcChannel _grpcChannel;
    private readonly bool _useGrpc;
    private readonly MoviesApiClient _moviesApiClient;


    public ProvidedApiClient(
        HttpClient httpClient,
        ILogger<ProvidedApiClient> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _useGrpc = configuration.GetValue<bool>("UseGrpc", false);
        var apiKey = configuration["ApiSettings:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("API Key is not configured.");
        }

        if (_useGrpc)
        {
            // gRPC Client Configuration
            var grpcAddress = configuration["GrpcSettings:BaseUrl"] ?? "https://localhost:7443";
            var handler = new HttpClientHandler();

            // Allow untrusted certificates only in development
            var isDevelopment = configuration.GetValue<bool>("Environment:IsDevelopment", false);
            if (isDevelopment)
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            var grpcHttpClient = new HttpClient(handler);
            grpcHttpClient.DefaultRequestHeaders.Add("X-Apikey", apiKey); 

            _grpcChannel = GrpcChannel.ForAddress(grpcAddress, new GrpcChannelOptions
            {
                HttpClient = grpcHttpClient
            });

            _moviesApiClient = new MoviesApi.MoviesApiClient(_grpcChannel);

            _logger.LogInformation("gRPC client configured for {GrpcAddress}", grpcAddress);
        }
        else
        {
            // REST API Client Configuration
            var apiBaseUrl = configuration["ApiSettings:BaseUrl"];
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                throw new InvalidOperationException("API Base URL is not configured.");
            }

            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.BaseAddress = new Uri(apiBaseUrl);
            _httpClient.DefaultRequestHeaders.Add("X-Apikey", apiKey);

            _logger.LogInformation("REST API client configured for {ApiBaseUrl}", apiBaseUrl);
        }
    }

    public async Task<showResponse> GetMovieAsync(string movieName)
    {
        try
        {


            return await GetMovieViaGrpc(movieName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching movie data");
            throw;
        }
    }

    private async Task<showResponse> GetMovieViaGrpc(string movieName)
    {
        //var request = new MovieRequest { Id = movieId };

        try
        {
            var response = await _moviesApiClient.SearchAsync(new SearchRequest() { Text = movieName });
            response.Data.TryUnpack<showListResponse>(out var data);
            return data.Shows.FirstOrDefault();
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "GRPC call failed");
            throw;
        }
    }

    //private async Task<MovieEntity> GetMovieViaHttp(string movieId)
    //{
    //    var response = await _httpClient.GetAsync($"/api/movies/{movieId}");
    //    response.EnsureSuccessStatusCode();
    //    return await response.Content.ReadFromJsonAsync<MovieEntity>();
    //}
}