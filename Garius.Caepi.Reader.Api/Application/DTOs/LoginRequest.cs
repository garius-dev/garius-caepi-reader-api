using System.ComponentModel.DataAnnotations;

namespace Garius.Caepi.Reader.Api.Application.DTOs
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = default!;
    }
}
