﻿using ApiApplication.Controllers;
using ApiApplication.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ApiApplication.Database.Repositories.Abstractions
{
    public interface IShowtimesRepository
    {
        //Task<ShowtimeEntity> CreateShowtimeAsync(string movieId, int auditoriumId, DateTime startTime, MovieData movieData, CancellationToken cancel);
        //Task<IEnumerable<ShowtimeEntity>> GetAllAsync(Expression<Func<ShowtimeEntity, bool>> filter, CancellationToken cancel);
        //Task<ShowtimeEntity> GetWithMoviesByIdAsync(int id, CancellationToken cancel);
        //Task<ShowtimeEntity> GetWithTicketsByIdAsync(int id, CancellationToken cancel);
        //Task<ShowtimeEntity> GetShowtimeAsync(int id, CancellationToken cancel);

        //+


        Task<ShowtimeEntity> CreateShowtime(ShowtimeEntity showtimeEntity, CancellationToken cancel);
        Task<IEnumerable<ShowtimeEntity>> GetAllAsync(Expression<Func<ShowtimeEntity, bool>> filter, CancellationToken cancel);
        Task<ShowtimeEntity> GetWithMoviesByIdAsync(int id, CancellationToken cancel);
        Task<ShowtimeEntity> GetWithTicketsByIdAsync(int id, CancellationToken cancel);

    }
}