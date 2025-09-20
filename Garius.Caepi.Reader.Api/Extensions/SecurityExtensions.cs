using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;

namespace Garius.Caepi.Reader.Api.Extensions
{
    public static class SecurityExtensions
    {
        public static string CreateOneTimeCode(int byteLen = 32)
        {
            if (byteLen < 32) throw new ArgumentOutOfRangeException(nameof(byteLen), "Use >= 32 bytes para garantir a segurança.");

            Span<byte> bytes = stackalloc byte[byteLen];
            RandomNumberGenerator.Fill(bytes);
            return WebEncoders.Base64UrlEncode(bytes);
        }
    }
}
