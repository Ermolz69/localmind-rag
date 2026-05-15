using KnowledgeApp.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Ingestion;

public sealed class ProcessIngestionJobHandler(IAppDbContext dbContext, IIngestionJobProcessor processor)
{
    public async Task<bool> HandleAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.IngestionJobs.AnyAsync(job => job.Id == jobId, cancellationToken);
        if (!exists)
        {
            return false;
        }

        await processor.ProcessAsync(jobId, cancellationToken);
        return true;
    }
}
