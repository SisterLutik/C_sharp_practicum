using System.ComponentModel.DataAnnotations;

namespace events_api.Models
{
    public class Event : IValidatableObject
    {
        private Event() { }

        public Event(string title, DateTime startAt, DateTime endAt, int totalSeats, string? description = null)
        {
            Id = Guid.NewGuid();
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description;
            StartAt = startAt.Kind == DateTimeKind.Utc ? startAt : DateTime.SpecifyKind(startAt, DateTimeKind.Utc);
            EndAt = endAt.Kind == DateTimeKind.Utc ? endAt : DateTime.SpecifyKind(endAt, DateTimeKind.Utc);
            TotalSeats = totalSeats;
            AvailableSeats = totalSeats;
            Bookings = new List<Booking>();
        }

        public Guid Id { get; internal set; }

        [Required(ErrorMessage = "Title обязателен")]
        public string Title { get; internal set; } = null!;

        public string? Description { get; internal set; }

        [Required(ErrorMessage = "StartAt обязателен")]
        public DateTime StartAt { get; internal set; }

        [Required(ErrorMessage = "EndAt обязателен")]
        public DateTime EndAt { get; internal set; }

        [Required(ErrorMessage = "TotalSeats обязателен")]
        public int TotalSeats { get; internal set; }

        public int AvailableSeats { get; internal set; }

        public ICollection<Booking> Bookings { get; internal set; } = new List<Booking>();

        public void UpdateDetails(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description;
            StartAt = startAt.Kind == DateTimeKind.Utc ? startAt : DateTime.SpecifyKind(startAt, DateTimeKind.Utc);
            EndAt = endAt.Kind == DateTimeKind.Utc ? endAt : DateTime.SpecifyKind(endAt, DateTimeKind.Utc);

            // Пересчитываем AvailableSeats при изменении TotalSeats
            var oldTotalSeats = TotalSeats;
            TotalSeats = totalSeats;

            if (TotalSeats != oldTotalSeats)
            {
                var seatsAdded = Math.Max(0, TotalSeats - oldTotalSeats);
                AvailableSeats = Math.Min(AvailableSeats + seatsAdded, TotalSeats);
            }
        }

        public bool TryReserveSeats(int count = 1)
        {
            if (AvailableSeats < count)
                return false;

            AvailableSeats -= count;
            return true;
        }

        public void ReleaseSeats(int count = 1)
        {
            AvailableSeats = Math.Min(AvailableSeats + count, TotalSeats);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartAt >= EndAt)
            {
                yield return new ValidationResult(
                    "Дата старта события должна быть раньше, чем дата конца",
                    new[] { nameof(StartAt), nameof(EndAt) }
                );
            }
        }
    }
}