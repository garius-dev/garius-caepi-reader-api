using System.ComponentModel.DataAnnotations;

namespace Garius.Caepi.Reader.Api.Application.DTOs
{
    public class RegisterTenantRequest
    {
        [Required]
        [StringLength(100)]
        public string TenantName { get; set; } = default!;
    }
}
