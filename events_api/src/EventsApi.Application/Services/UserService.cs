using EventsApi.Application.DTOs;
using EventsApi.Application.Interfaces;
using EventsApi.Domain.Entities;
using EventsApi.Domain.Enums;
using EventsApi.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace EventsApi.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(string login, string password)
    {
        if (await _userRepository.ExistsByLoginAsync(login))
            throw new ValidationException($"Пользователь с логином '{login}' уже существует");

        var passwordHash = _passwordHasher.Hash(password);
        var user = new User(login, passwordHash, UserRole.User);

        await _userRepository.AddAsync(user);

        _logger.LogInformation($"Зарегистрирован пользователь {user.Login} ({user.Id})");

        var token = _tokenService.GenerateToken(user.Id, user.Login, user.Role);

        return new AuthResponse
        {
            UserId = user.Id,
            Login = user.Login,
            Role = user.Role,
            Token = token
        };
    }

    public async Task<AuthResponse> LoginAsync(string login, string password)
    {
        var user = await _userRepository.GetByLoginAsync(login)
            ?? throw new ValidationException("Неверный логин или пароль");

        if (!_passwordHasher.Verify(password, user.PasswordHash))
            throw new ValidationException("Неверный логин или пароль");

        var token = _tokenService.GenerateToken(user.Id, user.Login, user.Role);

        _logger.LogInformation($"Пользователь {login} вошёл в систему");

        return new AuthResponse
        {
            UserId = user.Id,
            Login = user.Login,
            Role = user.Role,
            Token = token
        };
    }
}