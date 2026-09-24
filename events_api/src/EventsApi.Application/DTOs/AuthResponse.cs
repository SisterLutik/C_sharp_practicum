using EventsApi.Domain.Enums;

namespace EventsApi.Application.DTOs;

public class AuthResponse
{
    public Guid UserId { get; set; }
    public string Login { get; set; } = null!;
    public UserRole Role { get; set; }
    public string Token { get; set; } = null!;
}