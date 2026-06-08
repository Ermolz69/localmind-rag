using KnowledgeApp.Contracts.WatchedFolders;
using MediatR;

namespace KnowledgeApp.Application.Ingestion.WatchedFolders.Queries;

public sealed record GetWatchedFolderStatusQuery : IRequest<WatchedFolderStatusResponse>;
