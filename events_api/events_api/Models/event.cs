using System.ComponentModel.DataAnnotations;

namespace events_api.Models

{
    public class Event
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public required string Title { get; set; }

        public string? Description { get; set; }  

        [Required]
        public required DateTime StartAt { get; set; }

        [Required]
        public required DateTime EndAt { get; set; }
    }
}
