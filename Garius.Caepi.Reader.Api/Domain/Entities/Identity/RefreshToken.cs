using Garius.Caepi.Reader.Api.Domain.Abstractions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Garius.Caepi.Reader.Api.Domain.Entities.Identity
{
    public class RefreshToken : BaseEntity
    {
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        public virtual ApplicationUser User { get; set; } = default!;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public DateTime? RevokedAt { get; set; }
        public bool IsActive => RevokedAt == null && !IsExpired;
    }
}