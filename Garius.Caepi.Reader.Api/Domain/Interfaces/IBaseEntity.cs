namespace Garius.Caepi.Reader.Api.Domain.Interfaces
{
    public interface IBaseEntity
    {
        Guid Id { get; set; }
        bool Enabled { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
    }
}
