using System.Buffers;
using System.Diagnostics;

namespace KnowledgeApp.Infrastructure.Extensions;

public static class StreamExtensions
{
    public static async Task CopyToWithProgressAsync(
        this Stream source,
        Stream destination,
        long? totalBytes,
        Action<long, long?, double> onProgress,
        CancellationToken cancellationToken = default)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(81920);
        long totalRead = 0;
        long lastReportedBytes = 0;
        Stopwatch startedAt = Stopwatch.StartNew();
        Stopwatch lastReportAt = Stopwatch.StartNew();

        try
        {
            while (true)
            {
                int read = await source.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    break;
                }

                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                totalRead += read;

                bool shouldReport =
                    lastReportAt.ElapsedMilliseconds >= 250 ||
                    totalRead - lastReportedBytes >= 1024 * 1024 ||
                    totalBytes == totalRead;

                if (shouldReport)
                {
                    double speed = totalRead / Math.Max(1, startedAt.Elapsed.TotalSeconds);
                    onProgress(totalRead, totalBytes, speed);
                    lastReportedBytes = totalRead;
                    lastReportAt.Restart();
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
