using System.ComponentModel.DataAnnotations;
using UserService.Domain.Enums;

namespace UserService.Application.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "Логин обязателен")]
    [MinLength(3)]
    [MaxLength(100)]
    public string Login { get; set; } = null!;

    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(6)]
    public string Password { get; set; } = null!;
}