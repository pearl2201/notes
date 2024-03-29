using NodaTime;

namespace NotesApi.Entities;
public abstract class BaseAuditableEntity : BaseEntity
{
    public Instant CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public Instant? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }
}
