using Garius.Caepi.Reader.Api.Domain.Interfaces;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace Garius.Caepi.Reader.Api.Domain.Abstractions
{
    public class BaseEntity : IBaseEntity
    {
        [Key]
        public Guid Id { get; set; }
        public bool Enabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
