namespace ApiApplication.DTOs;

public class ReserveSeatsRequest
{
    public List<Seat> Seats { get; set; }
    public int ShowtimeId { get; set; }
}
