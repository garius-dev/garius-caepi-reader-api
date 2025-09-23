namespace Garius.Caepi.Reader.Api.Application.DTOs
{
    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public RefreshTokenResponse RefreshToken { get; set; }

        public TokenResponse(string accessToken, RefreshTokenResponse refreshToken)
        {
            this.AccessToken = accessToken;
            this.RefreshToken = refreshToken;
        }

        public TokenResponse()
        {
            
        }
    }
}
