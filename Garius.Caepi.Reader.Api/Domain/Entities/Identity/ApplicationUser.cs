using Garius.Caepi.Reader.Api.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Garius.Caepi.Reader.Api.Domain.Entities.Identity
{
    public class ApplicationUser : IdentityUser<Guid>, IBaseEntity
    {
        [Required(ErrorMessage = "O 'Nome' é obrigatório.")]
        [MaxLength(100, ErrorMessage = "O 'Nome' deve ter no máximo 100 caracteres.")]
        public string FirstName { get; set; } = default!;

        [Required(ErrorMessage = "O 'Sobrenome' é obrigatório.")]
        [MaxLength(100, ErrorMessage = "O 'Sobrenome' deve ter no máximo 100 caracteres.")]
        public string LastName { get; set; } = default!;

        [MaxLength(201, ErrorMessage = "O 'Nome Completo' deve ter no máximo 201 caracteres.")]
        public string FullName { get; set; } = default!;

        public string NormalizedFullName { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool Enabled { get; set; } = true;

        public string EmailEncrypted { get; set; } = string.Empty;

        public string EmailHash { get; set; } = string.Empty;

        public string? CpfEncrypted { get; set; }

        public string? CpfHash { get; set; }

        public virtual ICollection<UserTenant> TenantMemberships { get; set; } = new List<UserTenant>();
        public virtual ICollection<ApplicationUserClaim> Claims { get; set; } = new List<ApplicationUserClaim>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
    }
}