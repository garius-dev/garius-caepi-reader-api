namespace Garius.Caepi.Reader.Api.Application.DTOs
{
    public class RefreshTokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }

        public RefreshTokenResponse(string token, DateTime expiresAt)
        {
            Token = token;
            ExpiresAt = expiresAt;
        }

        public RefreshTokenResponse()
        {
            
        }
    }
}
