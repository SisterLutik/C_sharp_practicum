using System.ComponentModel.DataAnnotations;

namespace events_api.Models
{
    public class Booking : IValidatableObject
    {
        private Booking() { }

        public Booking(Guid eventId)
        {
            Id = Guid.NewGuid();
            EventId = eventId;
            Status = BookingStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; internal set; }

        [Required(ErrorMessage = "EventId обязателен")]
        public Guid EventId { get; internal set; }

        [Required(ErrorMessage = "Status обязателен")]
        public BookingStatus Status { get; internal set; }

        [Required(ErrorMessage = "CreatedAt обязателен")]
        public DateTime CreatedAt { get; internal set; }

        public DateTime? ProcessedAt { get; internal set; }

        // ✅ Навигационное свойство: бронь → событие
        public Event Event { get; internal set; } = null!;

        public void Confirm()
        {
            Status = BookingStatus.Confirmed;
            ProcessedAt = DateTime.UtcNow;
        }

        public void Reject()
        {
            Status = BookingStatus.Rejected;
            ProcessedAt = DateTime.UtcNow;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EventId == Guid.Empty)
                yield return new ValidationResult("EventId обязателен", new[] { nameof(EventId) });
        }
    }
}