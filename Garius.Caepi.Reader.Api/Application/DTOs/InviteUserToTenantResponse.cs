namespace Garius.Caepi.Reader.Api.Application.DTOs
{
    public class InviteUserToTenantResponse
    {
        public bool Completed { get; set; } = false;
        public string Message { get; set; } = string.Empty;

        public InviteUserToTenantResponse(bool completed, string message)
        {
            this.Completed = completed;
            this.Message = message;
        }
    }
}
