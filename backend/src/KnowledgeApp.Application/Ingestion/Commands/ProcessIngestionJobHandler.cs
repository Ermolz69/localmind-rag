using KnowledgeApp.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Ingestion;

public sealed class ProcessIngestionJobHandler(IAppDbContext dbContext, IIngestionJobProcessor processor)
{
    public async Task<ProcessIngestionJobResult> HandleAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.IngestionJob? job = await dbContext.IngestionJobs
            .FirstOrDefaultAsync(item => item.Id == jobId, cancellationToken);
        if (job is null)
        {
            return new ProcessIngestionJobResult(false, null, null);
        }

        await processor.ProcessAsync(jobId, cancellationToken);
        return new ProcessIngestionJobResult(true, jobId, job.Status.ToString());
    }
}
