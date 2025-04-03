using Grpc.Net.Client;
using ProtoDefinitions;

namespace ApiApplication
{
    public class ApiClientGrpc
    {
        public async Task<showListResponse> GetAll()
        {
            var httpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            var httpClient = new HttpClient(httpHandler);
            httpClient.DefaultRequestHeaders.Add("X-Apikey", "68e5fbda-9ec9-4858-97b2-4a8349764c63");

            var channel =
                GrpcChannel.ForAddress("https://localhost:7443", new GrpcChannelOptions()
                {
                    HttpClient = httpClient
                });
            var client = new MoviesApi.MoviesApiClient(channel);

            var all = await client.GetAllAsync(new Empty());
            all.Data.TryUnpack<showListResponse>(out var data);
            return data;
        }
    }
}