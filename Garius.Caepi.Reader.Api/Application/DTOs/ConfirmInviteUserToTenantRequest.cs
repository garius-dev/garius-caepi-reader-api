using System.ComponentModel.DataAnnotations;

namespace Garius.Caepi.Reader.Api.Application.DTOs
{
    public class ConfirmInviteUserToTenantRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = default!;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "As senhas não coincidem.")]
        public string ConfirmPassword { get; set; } = default!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;
    }
}
