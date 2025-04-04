using System.Text.Json;
using ApiApplication.Controllers;
using ApiApplication.Database.Entities;
using Google.Protobuf;
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

            if (_useGrpc)
                return await GetMovieViaGrpc(movieName);

            return await GetMovieViaHttp(movieName);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching movie data");
            throw;
        }
    }

    private async Task<showResponse> GetMovieViaGrpc(string movieName)
    {
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


    private async Task<showResponse> GetMovieViaHttp(string movieName)
    {
        var response = await _httpClient.GetAsync($"/v1/movies/?search={movieName}");
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();

        // Determine if the response is an array or an object
        string wrappedJson;
        var jsonRoot = JsonDocument.Parse(responseContent).RootElement;

        if (jsonRoot.ValueKind == JsonValueKind.Array)
        {
            // API returned a raw array, wrap it
            wrappedJson = $"{{ \"shows\": {responseContent} }}";
        }
        else
        {
            // Already an object
            wrappedJson = responseContent;
        }

        // Parse using Google.Protobuf.JsonParser
        var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));
        var showListResponse = parser.Parse<showListResponse>(wrappedJson);

        if (showListResponse == null || showListResponse.Shows.Count == 0)
        {
            throw new Exception("No shows found in response.");
        }

        return showListResponse.Shows.First();
    }
}