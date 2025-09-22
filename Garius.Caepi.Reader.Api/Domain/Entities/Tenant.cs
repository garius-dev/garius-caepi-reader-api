using Garius.Caepi.Reader.Api.Domain.Abstractions;
using Garius.Caepi.Reader.Api.Helpers.Attributes;
using System.ComponentModel.DataAnnotations;
using static Garius.Caepi.Reader.Api.Domain.Constants.DBStatus;

namespace Garius.Caepi.Reader.Api.Domain.Entities
{
    public class Tenant : BaseEntity
    {
        [Required, MaxLength(255)]
        public string TradeName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? LegalName { get; set; }

        [MaxLength(18), CNPJ]
        public string? CNPJ { get; set; }

        [MaxLength(50)]
        public string? StateRegistration { get; set; }

        [MaxLength(50)]
        public string? MunicipalRegistration { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(255)]
        public string? ImageUrl { get; set; }

        public TenantStatus Status { get; set; } = TenantStatus.Inactive;
        public virtual ICollection<UserTenant> UserMemberships { get; set; } = new List<UserTenant>();
    }
}