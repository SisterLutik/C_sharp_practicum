using System.ComponentModel.DataAnnotations;

namespace EventsApi.Application.DTOs;

public class UpdateEventRequest
{
    [Required(ErrorMessage = "Title обязателен")]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "StartAt обязателен")]
    public DateTime StartAt { get; set; }

    [Required(ErrorMessage = "EndAt обязателен")]
    public DateTime EndAt { get; set; }

    [Required(ErrorMessage = "TotalSeats обязателен")]
    [Range(1, int.MaxValue, ErrorMessage = "TotalSeats должен быть больше 0")]
    public int TotalSeats { get; set; }
}