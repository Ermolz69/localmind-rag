using System.Text.Json;
using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KnowledgeApp.Infrastructure.Persistence.Interceptors;

public sealed class SyncOutboxSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        CaptureOutboxEvents(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        CaptureOutboxEvents(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void CaptureOutboxEvents(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var entries = context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            if (entry.Entity is SyncOutboxItem)
            {
                continue;
            }

            if (IsSyncableEntity(entry.Entity))
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.LocalVersion++;
                }

                var operation = (entry.Entity, entry.State) switch
                {
                    (Document, EntityState.Added) => SyncOperation.CreateDocument,
                    (Document, EntityState.Modified) => SyncOperation.UpdateDocument,
                    (Document, EntityState.Deleted) => SyncOperation.DeleteDocument,
                    (Note, EntityState.Added) => SyncOperation.CreateNote,
                    (Note, EntityState.Modified) => SyncOperation.UpdateNote,
                    (Note, EntityState.Deleted) => SyncOperation.DeleteNote,
                    (Bucket, EntityState.Added) => SyncOperation.CreateBucket,
                    (Bucket, EntityState.Modified) => SyncOperation.UpdateBucket,
                    (Bucket, EntityState.Deleted) => SyncOperation.DeleteBucket,
                    _ => throw new NotSupportedException()
                };

                var payloadJson = JsonSerializer.Serialize((object)entry.Entity);

                var outboxItem = new SyncOutboxItem
                {
                    EntityType = entry.Entity.GetType().Name,
                    EntityId = entry.Entity.Id,
                    Operation = operation,
                    PayloadJson = payloadJson,
                    Status = SyncStatus.PendingUpload
                };

                context.Set<SyncOutboxItem>().Add(outboxItem);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LocalVersion++;
            }
        }
    }

    private static bool IsSyncableEntity(Entity entity)
    {
        return entity is Bucket or Document or Note;
    }
}
