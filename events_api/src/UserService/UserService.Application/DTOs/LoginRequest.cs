using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Логин обязателен")]
    public string Login { get; set; } = null!;

    [Required(ErrorMessage = "Пароль обязателен")]
    public string Password { get; set; } = null!;
}