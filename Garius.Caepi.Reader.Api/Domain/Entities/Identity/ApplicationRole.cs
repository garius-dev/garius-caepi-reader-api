﻿using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Garius.Caepi.Reader.Api.Domain.Entities.Identity
{
    public class ApplicationRole : IdentityRole<Guid>
    {
        public ApplicationRole() : base()
        {
        }

        public ApplicationRole(string roleName) : base(roleName)
        {
        }

        public ApplicationRole(string roleName, string? description, int level) : base(roleName)
        {
            Description = description;
            Level = level;
        }

        [MaxLength(250, ErrorMessage = "A 'Descrição' deve ter no máximo 250 caracteres.")]
        public string? Description { get; set; }

        public int Level { get; set; }

        public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
        public virtual ICollection<ApplicationRoleClaim> Claims { get; set; } = new List<ApplicationRoleClaim>();
    }
}
