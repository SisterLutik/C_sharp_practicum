using System.ComponentModel.DataAnnotations;

namespace events_api.Models

{
    public class Event : IValidatableObject
    {
        public int Id { get; set; }

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
    }
}
