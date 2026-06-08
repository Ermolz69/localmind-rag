using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.WatchedFolders;
using MediatR;

namespace KnowledgeApp.Application.Ingestion.WatchedFolders.Commands;

public sealed record RescanWatchedFoldersCommand(string? Path = null) : IRequest<Result<RescanWatchedFoldersResponse>>;
