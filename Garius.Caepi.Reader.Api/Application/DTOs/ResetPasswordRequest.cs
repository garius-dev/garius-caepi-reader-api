﻿using System.ComponentModel.DataAnnotations;

namespace Garius.Caepi.Reader.Api.Application.DTOs
{
    public class ResetPasswordRequest
    {
        [Required]
        public Guid UserId { get; set; } = default!;

        [Required]
        public string Token { get; set; } = default!;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } = default!;

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "As senhas não coincidem.")]
        public string ConfirmPassword { get; set; } = default!;
    }
}
