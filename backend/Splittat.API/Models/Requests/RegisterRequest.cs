using System.ComponentModel.DataAnnotations;

namespace Splittat.API.Models.Requests;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string LastName { get; set; } = string.Empty;
}
