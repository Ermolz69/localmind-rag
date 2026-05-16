using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class NoteLink : Entity
{
    public Guid SourceNoteId { get; set; }
    public Guid TargetNoteId { get; set; }
}
