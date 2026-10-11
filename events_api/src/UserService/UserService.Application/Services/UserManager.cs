using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Exceptions;

namespace UserService.Application.Services;

public class UserManager : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<UserManager> _logger;

    public UserManager(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<UserManager> logger)
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
            ?? throw new InvalidCredentialsException("Неверный логин или пароль");

        if (!_passwordHasher.Verify(password, user.PasswordHash))
            throw new InvalidCredentialsException("Неверный логин или пароль");

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