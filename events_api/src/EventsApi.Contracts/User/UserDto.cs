namespace EventsApi.Contracts.Users;

public class UserDto
{
    public Guid Id { get; set; }
    public string Login { get; set; } = null!;
    public string Role { get; set; } = null!;
}