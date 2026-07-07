using System.ComponentModel.DataAnnotations;

namespace events_api.Models
{
    public class Booking
    {
        [Required(ErrorMessage = "Id обязателен")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "EventId обязателен")]
        public int EventId { get; set; }

        [Required(ErrorMessage = "Status обязателен")]
        public BookingStatus Status { get; set; }

        [Required(ErrorMessage = "CreatedAt обязателен")]
        public DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }
    }
}
