namespace ApiApplication.DTOs;

public class CreateShowtimeRequest
{
    public string MovieName { get; set; }
    public int AuditoriumId { get; set; }
    public DateTime StartTime { get; set; }
}
