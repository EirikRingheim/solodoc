using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Procedures;

public class ProcedureReadConfirmation : BaseEntity
{
    public Guid ProcedureTemplateId { get; set; }
    public Guid PersonId { get; set; }
    public DateTimeOffset ReadAt { get; set; }
}
