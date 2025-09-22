using Garius.Caepi.Reader.Api.Domain.Entities;

namespace Garius.Caepi.Reader.Api.Application.Interfaces
{
    public interface ITenantService
    {
        Guid GetTenantId();
        
    }
}
