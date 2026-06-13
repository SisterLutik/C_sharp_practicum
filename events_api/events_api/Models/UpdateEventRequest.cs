namespace events_api.Models
{
    public class UpdateEventRequest
    {
        public string? Title { get; set; }        
        public string? Description { get; set; }  
        public DateTime? StartAt { get; set; }   
        public DateTime? EndAt { get; set; }
    }
}
