using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ApiApplication.Database.Entities;

namespace ApiApplication.Database.Entities
{
    //public class ShowtimeEntity
    //{
    //    public ShowtimeEntity()
    //    {
    //        Tickets = new List<TicketEntity>();
    //    }

    //    public int Id { get; set; }
    //    public string MovieId { get; set; }
    //    public string MovieTitle { get; set; }
    //    public DateTime StartTime { get; set; }
    //    public int AuditoriumId { get; set; }
    //    public AuditoriumEntity Auditorium { get; set; }
    //    public ICollection<TicketEntity> Tickets { get; set; }
    //}
    public class ShowtimeEntity
    {
        public int Id { get; set; }
        public MovieEntity Movie { get; set; }
        public DateTime SessionDate { get; set; }
        public int AuditoriumId { get; set; }
        public ICollection<TicketEntity> Tickets { get; set; }
    }
}


