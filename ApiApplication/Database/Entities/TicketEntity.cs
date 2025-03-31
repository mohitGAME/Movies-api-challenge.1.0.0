using System;
using System.Collections.Generic;

namespace ApiApplication.Database.Entities
{
    public class TicketEntity
    {
        public TicketEntity()
        {
            CreatedAt = DateTime.UtcNow;
            Paid = false;
            IsExpired = false;
        }

        public Guid Id { get; set; }
        public int ShowtimeId { get; set; }
        public ICollection<SeatEntity> Seats { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Paid { get; set; }
        public bool IsExpired { get; set; }
        public ShowtimeEntity Showtime { get; set; }
    }
}
