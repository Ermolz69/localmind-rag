using KnowledgeApp.Infrastructure.Services;

namespace KnowledgeApp.UnitTests.Infrastructure;

public sealed class LocalDiagnosticsServiceTests
{
    [Fact]
    public void SafeDirectorySize_Should_Return_Zero_When_Directory_Is_Missing()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"localmind-missing-{Guid.NewGuid():N}");

        var size = LocalDiagnosticsService.SafeDirectorySize(missingPath);

        Assert.Equal(0, size);
    }

    [Fact]
    public async Task SafeDirectorySize_Should_Return_Total_File_Size()
    {
        var directory = Directory.CreateTempSubdirectory("localmind-diagnostics-");
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "a.bin"), new byte[3]);
            var nested = Directory.CreateDirectory(Path.Combine(directory.FullName, "nested"));
            await File.WriteAllBytesAsync(Path.Combine(nested.FullName, "b.bin"), new byte[5]);

            var size = LocalDiagnosticsService.SafeDirectorySize(directory.FullName);

            Assert.Equal(8, size);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
