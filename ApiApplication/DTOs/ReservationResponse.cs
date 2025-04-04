namespace ApiApplication.DTOs;

public class ReservationResponse
{
    public Guid Guid { get; set; }

    public List<Seat> Seats { get; set; }

    public int AuditoriumId { get; set; }

    public string Movie { get; set; }

}