using System.ComponentModel.DataAnnotations;

namespace events_api.Models
{
    public class CreateEventRequest
    {
        [Required(ErrorMessage = "Title обязателен")]
        public required string Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "StartAt обязателен")]
        public required DateTime StartAt { get; set; }

        [Required(ErrorMessage = "EndAt обязателен")]
        public required DateTime EndAt { get; set; }
    }
}