using System.ComponentModel.DataAnnotations;

namespace events_api.Models

{
    public class Event : IValidatableObject
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Title обязателен")]
        public required string Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "StartAt обязателен")]
        public DateTime StartAt { get; set; }

        [Required(ErrorMessage = "EndAt обязателен")]
        public DateTime EndAt { get; set; }

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

        [Required(ErrorMessage = "TotalSeats обязателен")]
        public int TotalSeats { get; set; }

        public int AvailableSeats { get; set; }

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
    }
}
