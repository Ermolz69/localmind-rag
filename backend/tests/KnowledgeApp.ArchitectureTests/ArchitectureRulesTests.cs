using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Persistence;

namespace KnowledgeApp.ArchitectureTests;

public sealed class ArchitectureRulesTests
{
    [Fact]
    public void Domain_Should_Not_Reference_Outer_Layers()
    {
        var forbidden = new[]
        {
            "KnowledgeApp.Application",
            "KnowledgeApp.Infrastructure",
            "KnowledgeApp.LocalApi",
            "KnowledgeApp.SyncApi",
            "Microsoft.EntityFrameworkCore",
        };

        var references = typeof(Document).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();

        Assert.DoesNotContain(references, reference => forbidden.Contains(reference));
    }

    [Fact]
    public void Application_Should_Not_Reference_Infrastructure_Or_Api_Projects()
    {
        var forbidden = new[]
        {
            "KnowledgeApp.Infrastructure",
            "KnowledgeApp.LocalApi",
            "KnowledgeApp.SyncApi",
            "KnowledgeApp.Worker",
        };

        var references = typeof(IAppDbContext).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();

        Assert.DoesNotContain(references, reference => forbidden.Contains(reference));
    }

    [Fact]
    public void Infrastructure_Should_Implement_Application_Ports()
    {
        Assert.True(typeof(IAppDbContext).IsAssignableFrom(typeof(AppDbContext)));
    }

    [Fact]
    public void Api_Project_Files_Should_Not_Declare_Direct_Domain_ProjectReference()
    {
        var root = FindRepositoryRoot();
        var localApiProject = File.ReadAllText(Path.Combine(root, "backend/src/KnowledgeApp.LocalApi/KnowledgeApp.LocalApi.csproj"));
        var syncApiProject = File.ReadAllText(Path.Combine(root, "backend/src/KnowledgeApp.SyncApi/KnowledgeApp.SyncApi.csproj"));

        Assert.DoesNotContain("KnowledgeApp.Domain.csproj", localApiProject);
        Assert.DoesNotContain("KnowledgeApp.Domain.csproj", syncApiProject);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "pnpm-workspace.yaml")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
