using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Application.Chats;
using KnowledgeApp.Application.Notes;
using KnowledgeApp.Contracts.Buckets;
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
    public void Contracts_Should_Not_Reference_Domain()
    {
        var references = typeof(BucketDto).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();

        Assert.DoesNotContain("KnowledgeApp.Domain", references);
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

    [Fact]
    public void LocalApi_Endpoint_Modules_Should_Not_Use_AppDbContext_Directly()
    {
        var root = FindRepositoryRoot();
        var endpointFiles = Directory.GetFiles(
            Path.Combine(root, "backend/src/KnowledgeApp.LocalApi/Endpoints"),
            "*.cs",
            SearchOption.AllDirectories);

        foreach (var endpointFile in endpointFiles)
        {
            var source = File.ReadAllText(endpointFile);

            Assert.DoesNotContain("AppDbContext", source);
            Assert.DoesNotContain("KnowledgeApp.Infrastructure.Persistence", source);
        }
    }

    [Fact]
    public void LocalApi_Endpoint_Modules_Should_Not_Use_Domain_Entities_Directly()
    {
        var root = FindRepositoryRoot();
        var endpointFiles = Directory.GetFiles(
            Path.Combine(root, "backend/src/KnowledgeApp.LocalApi/Endpoints"),
            "*.cs",
            SearchOption.AllDirectories);

        foreach (var endpointFile in endpointFiles)
        {
            var source = File.ReadAllText(endpointFile);

            Assert.DoesNotContain("KnowledgeApp.Domain.Entities", source);
        }
    }

    [Fact]
    public void Bucket_Note_Chat_Query_And_Create_Handlers_Should_Not_Return_Domain_Entities()
    {
        var handlerTypes = new[]
        {
            typeof(CreateBucketHandler),
            typeof(GetBucketsHandler),
            typeof(CreateNoteHandler),
            typeof(GetNotesHandler),
            typeof(CreateChatHandler),
            typeof(GetChatsHandler),
        };
        var forbiddenReturnTypes = new[]
        {
            typeof(Bucket),
            typeof(Note),
            typeof(Conversation),
        };

        foreach (var handlerType in handlerTypes)
        {
            var handleMethods = handlerType.GetMethods()
                .Where(method => method.Name == "HandleAsync");

            foreach (var method in handleMethods)
            {
                foreach (var forbiddenType in forbiddenReturnTypes)
                {
                    Assert.False(
                        TypeUses(method.ReturnType, forbiddenType),
                        $"{handlerType.Name}.{method.Name} returns {forbiddenType.Name} through {method.ReturnType}.");
                }
            }
        }
    }

    private static bool TypeUses(Type candidate, Type forbidden)
    {
        if (candidate == forbidden)
        {
            return true;
        }

        if (candidate.IsArray)
        {
            return TypeUses(candidate.GetElementType()!, forbidden);
        }

        return candidate.IsGenericType && candidate.GetGenericArguments().Any(type => TypeUses(type, forbidden));
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
