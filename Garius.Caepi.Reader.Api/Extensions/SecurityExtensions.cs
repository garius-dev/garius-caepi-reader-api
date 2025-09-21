using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using static Garius.Caepi.Reader.Api.Configuration.AppSecretsConfiguration;

namespace Garius.Caepi.Reader.Api.Extensions
{
    public static class SecurityExtensions
    {
        public static string Encrypt(this string plainText, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(this string cipherText, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            var buffer = Convert.FromBase64String(cipherText);
            using var ms = new MemoryStream(buffer);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }

        public static string ComputeHash(this string input)
        {
            if (input is null) throw new ArgumentException(nameof(input));

            using var sha = SHA256.Create();
            var normalized = input.ToUpperInvariant().Trim();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToBase64String(bytes);
        }

        public static (string encryptedText, string textHash) EncryptAndHashText(this string text, AppKeyManagementSettings settings)
        {
            var key = settings.GetKeyBytes();
            var iv = settings.GetIVBytes();
            var encryptedEmail = text.Encrypt(key, iv);
            var emailHash = text.ComputeHash();
            return (encryptedEmail, emailHash);
        }

        public static string Base64UrlEncode(this string input)
        {
            return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(input));
        }

        public static byte[] Base64UrlDecode(this string input)
        {
            return WebEncoders.Base64UrlDecode(input);
        }

        public static string CreateOneTimeCode(int byteLen = 32)
        {
            if (byteLen < 32) throw new ArgumentOutOfRangeException(nameof(byteLen), "Use >= 32 bytes para garantir a segurança.");

            Span<byte> bytes = stackalloc byte[byteLen];
            RandomNumberGenerator.Fill(bytes);
            return WebEncoders.Base64UrlEncode(bytes);
        }
    }
}