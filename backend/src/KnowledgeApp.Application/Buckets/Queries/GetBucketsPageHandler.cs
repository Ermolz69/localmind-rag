using System.Globalization;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Pagination;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Buckets;

public sealed class GetBucketsPageHandler(IBucketRepository bucketRepository)
{
    private const string CursorKind = "buckets";

    public async Task<Result<CursorPage<BucketDto>>> HandleAsync(
        GetBucketsPageQuery query,
        CancellationToken cancellationToken = default)
    {
        Result<int> limitResult = CursorPagination.ValidateLimit(query.Limit);
        if (!limitResult.IsSuccess)
        {
            return Result<CursorPage<BucketDto>>.Failure(limitResult.Error!);
        }

        int limit = limitResult.Value;
        string? normalizedQuery = string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim();
        string filterHash = CursorPagination.CreateFilterHash(new { Query = normalizedQuery });
        Result<CursorPayload?> cursorResult = CursorPagination.Decode(query.Cursor, CursorKind, filterHash);
        if (!cursorResult.IsSuccess)
        {
            return Result<CursorPage<BucketDto>>.Failure(cursorResult.Error!);
        }

        CursorPayload? cursor = cursorResult.Value;

        IReadOnlyList<Bucket> allBuckets = await bucketRepository.ListAsync(cancellationToken);

        IEnumerable<Bucket> filteredBuckets = allBuckets;
        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            filteredBuckets = filteredBuckets.Where(bucket => bucket.Name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase));
        }

        Bucket[] sortedBuckets = filteredBuckets
            .OrderByDescending(bucket => bucket.CreatedAt)
            .ThenByDescending(bucket => bucket.Id.ToString("N", CultureInfo.InvariantCulture))
            .ToArray();

        CursorPage<Bucket> bucketPage = CursorPagination.CreatePage(
            sortedBuckets,
            cursor,
            limit,
            CompareBucketToCursor,
            bucket => new CursorPayload(
                CursorKind,
                filterHash,
                null,
                bucket.CreatedAt,
                bucket.Id,
                false));
        BucketDto[] bucketDtos = bucketPage.Items.Select(BucketMapper.ToDto).ToArray();

        return Result<CursorPage<BucketDto>>.Success(
            new CursorPage<BucketDto>(bucketDtos, bucketPage.NextCursor, bucketPage.Limit, bucketPage.HasMore));
    }

    private static int CompareBucketToCursor(Bucket bucket, CursorPayload cursor)
    {
        if (bucket.Id == cursor.Id)
        {
            return 2;
        }

        if (bucket.CreatedAt < cursor.CreatedAt)
        {
            return 1;
        }

        if (bucket.CreatedAt == cursor.CreatedAt &&
            string.Compare(
                bucket.Id.ToString("N", CultureInfo.InvariantCulture),
                cursor.Id.ToString("N", CultureInfo.InvariantCulture),
                StringComparison.Ordinal) < 0)
        {
            return 1;
        }

        return 0;
    }
}
