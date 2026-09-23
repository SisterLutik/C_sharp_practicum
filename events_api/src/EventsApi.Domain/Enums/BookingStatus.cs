namespace EventsApi.Domain.Enums
{
    public enum BookingStatus
    {
        Pending = 0,    // Бронь создана, ожидает обработки
        Confirmed = 1,  // Бронь подтверждена
        Rejected = 2,   // Бронь отклонена
        Cancelled = 3   // Бронь отменина

    }
}