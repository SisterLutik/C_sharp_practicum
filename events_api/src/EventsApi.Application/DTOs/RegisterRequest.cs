using System.ComponentModel.DataAnnotations;

namespace EventsApi.Application.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "Логин обязателен")]
    [MinLength(3, ErrorMessage = "Логин должен быть не короче 3 символов")]
    [MaxLength(100)]
    public string Login { get; set; } = null!;

    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(6, ErrorMessage = "Пароль должен быть не короче 6 символов")]
    public string Password { get; set; } = null!;
}