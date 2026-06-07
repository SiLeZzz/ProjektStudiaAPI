using System.ComponentModel.DataAnnotations;
using WebAPI.Domain.Entities;

namespace WebAPI.Contracts.Employees;

public class UpdateEmployeeRequest
{
    [Required(ErrorMessage = "Imię jest wymagane!")]
    public string Imie { get; set; } = string.Empty;
    [Required(ErrorMessage = "Nazwisko jest wymagane!")]
    public string Nazwisko { get; set; } = string.Empty;
    [Required(ErrorMessage = "Login jest wymagany!")]
    public string Login { get; set; } = string.Empty;
    [Required(ErrorMessage = "Hasło jest wymagane!")]
    public string Password { get; set; } = null!;
    
}