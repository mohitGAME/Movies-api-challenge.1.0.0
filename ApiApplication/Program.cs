using ApiApplication.Database;
using ApiApplication.Database.Repositories;
using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.DTOs;
using ApiApplication.Services;
using ApiApplication.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddTransient<IShowtimesRepository, ShowtimesRepository>();
builder.Services.AddTransient<ITicketsRepository, TicketsRepository>();
builder.Services.AddTransient<IAuditoriumsRepository, AuditoriumsRepository>();
builder.Services.AddTransient<IProvidedApiClient, ProvidedApiClient>();

builder.Services.AddDbContext<CinemaContext>(options =>
{
    options.UseSqlite("Data Source=cinema.db")
        .EnableSensitiveDataLogging()
        .ConfigureWarnings(b => b.Ignore(SqliteEventId.UniqueConstraintFound))
        .LogTo(Console.WriteLine, LogLevel.Information);
});


//builder.Services.AddDbContext<CinemaContext>(options =>
//{
//    options.UseInMemoryDatabase("CinemaDb")
//        .EnableSensitiveDataLogging()
//        .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning));
//});

// Add controllers
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Cinema API",
        Version = "v1",
        Description = "API for managing cinema showtimes, reservations, and ticket purchases"
    });
});

// Add HTTP client and ProvidedApiClient
builder.Services.AddHttpClient<IProvidedApiClient, ProvidedApiClient>();

// Configure Redis
//builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
//{
//    var configuration = sp.GetRequiredService<IConfiguration>();
//    var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString", "localhost:6379");
//    return ConnectionMultiplexer.Connect(redisConnectionString);
//});



// Configure GRPC
if (builder.Configuration.GetValue<bool>("UseGrpc", false))
{
    builder.Services.AddGrpc();
}

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Configuring Redis connection...");

    var configuration = sp.GetRequiredService<IConfiguration>();
    var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString", "localhost:6379");
    return ConnectionMultiplexer.Connect(redisConnectionString);
});

builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IValidator<CreateShowtimeRequest>, CreateShowtimeValidator>();
builder.Services.AddScoped<IValidator<ReserveSeatsRequest>, ReserveSeatsValidator>();



var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cinema API v1"));
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Initialize sample data
SampleData.Initialize(app);

app.Run();
