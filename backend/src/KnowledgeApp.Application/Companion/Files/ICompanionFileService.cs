using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Companion;
using KnowledgeApp.Contracts.Documents;

namespace KnowledgeApp.Application.Companion.Files;

/// <summary>
/// Lets the phone browse and pick files from the folders the user explicitly
/// allowed on the computer. Every path is validated to be inside an allowed root,
/// so the phone can never see the whole disk.
/// </summary>
public interface ICompanionFileService
{
    /// <summary>Returns the configured allowed roots.</summary>
    Task<CompanionRootsResponse> GetRootsAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists subfolders and supported files inside an allowed folder.</summary>
    Task<Result<CompanionBrowseResponse>> BrowseAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Adds a file from an allowed folder into LocalMind for indexing.</summary>
    Task<Result<UploadDocumentResponse>> AddFileAsync(string path, CancellationToken cancellationToken = default);
}
