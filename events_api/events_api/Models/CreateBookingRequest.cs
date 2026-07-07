using System;
using System.ComponentModel.DataAnnotations;

namespace events_api.Models
{
    public class CreateBookingRequest
    {
        [Required(ErrorMessage = "EventId обязателен")]
        public int EventId { get; set; }
    }
}