using KnowledgeApp.Contracts.Documents;

namespace KnowledgeApp.Application.Abstractions;

public interface IFileStorageService
{
    Task<StoredFileDto> SaveAsync(
        Stream content,
        Guid documentId,
        string fileName,
        CancellationToken cancellationToken = default);
}
