using System.ComponentModel.DataAnnotations;

namespace Garius.Caepi.Reader.Api.Application.DTOs
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;
    }
}
