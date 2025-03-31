using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiApplication.Database.Entities
{
    public class ShowtimeEntity
    {
        public ShowtimeEntity()
        {
            Tickets = new List<TicketEntity>();
        }

        public int Id { get; set; }
        public string MovieId { get; set; }
        public string MovieTitle { get; set; }
        public DateTime StartTime { get; set; }
        public int AuditoriumId { get; set; }
        public AuditoriumEntity Auditorium { get; set; }
        public ICollection<TicketEntity> Tickets { get; set; }
    }
}
