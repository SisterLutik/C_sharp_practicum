using EventsApi.Domain.Entities;

namespace EventsApi.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByLoginAsync(string login);
    Task<bool> ExistsByLoginAsync(string login);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);
}