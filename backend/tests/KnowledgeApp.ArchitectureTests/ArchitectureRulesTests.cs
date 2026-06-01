using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Application.Chats;
using KnowledgeApp.Application.Notes;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services;
using System.Text.RegularExpressions;

namespace KnowledgeApp.ArchitectureTests;

public sealed class ArchitectureRulesTests
{
    [Fact]
    public void Domain_Should_Not_Reference_Outer_Layers()
    {
        string[]? forbidden = new[]
        {
            "KnowledgeApp.Application",
            "KnowledgeApp.Infrastructure",
            "KnowledgeApp.LocalApi",
            "KnowledgeApp.SyncApi",
            "Microsoft.EntityFrameworkCore",
        };

        string?[]? references = typeof(Document).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();

        Assert.DoesNotContain(references, reference => forbidden.Contains(reference));
    }

    [Fact]
    public void Application_Should_Not_Reference_Infrastructure_Or_Api_Projects()
    {
        string[]? forbidden = new[]
        {
            "KnowledgeApp.Infrastructure",
            "KnowledgeApp.LocalApi",
            "KnowledgeApp.SyncApi",
            "KnowledgeApp.Worker",
        };

        string?[]? references = typeof(IAppDbContext).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();

        Assert.DoesNotContain(references, reference => forbidden.Contains(reference));
    }

    [Fact]
    public void Contracts_Should_Not_Reference_Domain()
    {
        string?[]? references = typeof(BucketDto).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();

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
        string? root = FindRepositoryRoot();
        string? localApiProject = File.ReadAllText(Path.Combine(root, "backend/src/KnowledgeApp.LocalApi/KnowledgeApp.LocalApi.csproj"));
        string? syncApiProject = File.ReadAllText(Path.Combine(root, "backend/src/KnowledgeApp.SyncApi/KnowledgeApp.SyncApi.csproj"));

        Assert.DoesNotContain("KnowledgeApp.Domain.csproj", localApiProject);
        Assert.DoesNotContain("KnowledgeApp.Domain.csproj", syncApiProject);
    }

    [Fact]
    public void LocalApi_Endpoint_Modules_Should_Not_Use_AppDbContext_Directly()
    {
        string? root = FindRepositoryRoot();
        string[]? endpointFiles = Directory.GetFiles(
            Path.Combine(root, "backend/src/KnowledgeApp.LocalApi/Endpoints"),
            "*.cs",
            SearchOption.AllDirectories);

        foreach (string endpointFile in endpointFiles)
        {
            string? source = File.ReadAllText(endpointFile);

            Assert.DoesNotContain("AppDbContext", source);
            Assert.DoesNotContain("KnowledgeApp.Infrastructure.Persistence", source);
        }
    }

    [Fact]
    public void LocalApi_Endpoint_Modules_Should_Not_Use_Domain_Entities_Directly()
    {
        string? root = FindRepositoryRoot();
        string[]? endpointFiles = Directory.GetFiles(
            Path.Combine(root, "backend/src/KnowledgeApp.LocalApi/Endpoints"),
            "*.cs",
            SearchOption.AllDirectories);

        foreach (string endpointFile in endpointFiles)
        {
            string? source = File.ReadAllText(endpointFile);

            Assert.DoesNotContain("KnowledgeApp.Domain.Entities", source);
        }
    }

    [Fact]
    public void LocalApi_Endpoint_Modules_Should_Not_Bypass_ApiResults_For_Normal_Responses()
    {
        string root = FindRepositoryRoot();
        string[] endpointFiles = Directory.GetFiles(
            Path.Combine(root, "backend/src/KnowledgeApp.LocalApi/Endpoints"),
            "*Endpoints.cs",
            SearchOption.AllDirectories);
        Regex directResultsCall = new(@"(?<!Api)(?<!Typed)Results\.(Ok|Created|Accepted|Problem|BadRequest|NotFound|Conflict|NoContent)\(");

        foreach (string endpointFile in endpointFiles)
        {
            if (endpointFile.EndsWith("HealthEndpoints.cs", StringComparison.Ordinal))
            {
                continue;
            }

            string source = File.ReadAllText(endpointFile);
            Assert.False(
                directResultsCall.IsMatch(source),
                $"{Path.GetFileName(endpointFile)} should return through ApiResults for non-exempt endpoints.");
        }
    }

    [Fact]
    public void LocalApi_Endpoint_Modules_Should_Advertise_ApiResponse_Metadata()
    {
        string root = FindRepositoryRoot();
        string[] endpointFiles = Directory.GetFiles(
            Path.Combine(root, "backend/src/KnowledgeApp.LocalApi/Endpoints"),
            "*Endpoints.cs",
            SearchOption.AllDirectories);

        foreach (string endpointFile in endpointFiles)
        {
            if (endpointFile.EndsWith("HealthEndpoints.cs", StringComparison.Ordinal))
            {
                continue;
            }

            string source = File.ReadAllText(endpointFile);
            Assert.Contains("Produces<ApiResponse", source);
        }
    }

    [Fact]
    public void Bucket_Note_Chat_Query_And_Create_Handlers_Should_Not_Return_Domain_Entities()
    {
        Type[]? handlerTypes = new[]
        {
            typeof(CreateBucketHandler),
            typeof(GetBucketsHandler),
            typeof(CreateNoteHandler),
            typeof(GetNotesHandler),
            typeof(CreateChatHandler),
            typeof(GetChatsHandler),
        };
        Type[]? forbiddenReturnTypes = new[]
        {
            typeof(Bucket),
            typeof(Note),
            typeof(Conversation),
        };

        foreach (Type handlerType in handlerTypes)
        {
            IEnumerable<System.Reflection.MethodInfo>? handleMethods = handlerType.GetMethods()
                .Where(method => method.Name == "HandleAsync");

            foreach (System.Reflection.MethodInfo method in handleMethods)
            {
                foreach (Type forbiddenType in forbiddenReturnTypes)
                {
                    Assert.False(
                        TypeUses(method.ReturnType, forbiddenType),
                        $"{handlerType.Name}.{method.Name} returns {forbiddenType.Name} through {method.ReturnType}.");
                }
            }
        }
    }

    [Fact]
    public void Ai_Runtime_Manager_Should_Implement_Runtime_Provider_Contract()
    {
        Assert.True(typeof(IAiRuntimeProvider).IsAssignableFrom(typeof(AiRuntimeManager)));
    }

    [Fact]
    public void Ingestion_Lifecycle_Code_Should_Use_Job_Repository_Port()
    {
        string root = FindRepositoryRoot();
        string[] sourceFiles =
        [
            "backend/src/KnowledgeApp.Application/Ingestion/Commands/ProcessIngestionJobHandler.cs",
            "backend/src/KnowledgeApp.Application/Ingestion/Commands/RetryIngestionJobHandler.cs",
            "backend/src/KnowledgeApp.Application/Ingestion/Commands/CancelIngestionJobHandler.cs",
            "backend/src/KnowledgeApp.Application/Ingestion/Queries/ListIngestionJobsHandler.cs",
            "backend/src/KnowledgeApp.Application/Ingestion/Queries/GetIngestionJobHandler.cs",
            "backend/src/KnowledgeApp.Infrastructure/Services/Ingestion/IngestionJobProcessor.cs",
            "backend/src/KnowledgeApp.Infrastructure/Services/Ingestion/QueuedIngestionJobDispatcher.cs",
        ];

        foreach (string sourceFile in sourceFiles)
        {
            string source = File.ReadAllText(Path.Combine(root, sourceFile));
            Assert.Contains("IIngestionJobRepository", source);
        }
    }

    [Fact]
    public void Runtime_Endpoint_Should_Use_Provider_Registry_Not_Concrete_Runtime_Manager()
    {
        string root = FindRepositoryRoot();
        string runtimeEndpoint = File.ReadAllText(Path.Combine(root, "backend/src/KnowledgeApp.LocalApi/Endpoints/Runtime/RuntimeEndpoints.cs"));

        Assert.Contains("IAiRuntimeProviderRegistry", runtimeEndpoint);
        Assert.DoesNotContain("AiRuntimeManager", runtimeEndpoint);
    }

    [Fact]
    public void Frontend_Should_Not_Call_Ai_Runtime_Providers_Directly()
    {
        string root = FindRepositoryRoot();
        string[] frontendFiles = Directory.GetFiles(
            Path.Combine(root, "apps/desktop/src"),
            "*.*",
            SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".ts", StringComparison.Ordinal) || file.EndsWith(".tsx", StringComparison.Ordinal))
            .ToArray();

        foreach (string frontendFile in frontendFiles)
        {
            if (frontendFile.EndsWith("generated.ts", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string source = File.ReadAllText(frontendFile);

            Assert.DoesNotContain("ollama", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("llama.cpp", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("11435", source, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Architecture_Decisions_Should_Be_Documented_In_Decisions_Folder()
    {
        string root = FindRepositoryRoot();
        string decisionsRoot = Path.Combine(root, "docs/architecture/decisions");
        string[] expected =
        [
            "template.md",
            "0001-modular-monolith.md",
            "0002-localapi-as-api-boundary.md",
            "0003-apiresponse-envelope.md",
            "0004-result-for-expected-failures.md",
            "0005-frontend-does-not-call-ai-runtime.md",
            "0006-local-vector-index-and-offline-mode.md",
            "0007-ai-runtime-provider-abstraction.md",
            "0008-ingestion-job-lifecycle.md",
            "0009-localapi-local-security.md",
        ];

        foreach (string fileName in expected)
        {
            string path = Path.Combine(decisionsRoot, fileName);
            Assert.True(File.Exists(path), $"{fileName} is missing.");

            string source = File.ReadAllText(path);
            Assert.Contains("## Status", source);
            Assert.Contains("## Context", source);
            Assert.Contains("## Decision", source);
            Assert.Contains("## Consequences", source);
        }

        string toc = File.ReadAllText(Path.Combine(root, "docs/toc.yml"));
        Assert.Contains("architecture/decisions/0001-modular-monolith.md", toc);
        Assert.Contains("architecture/decisions/0009-localapi-local-security.md", toc);
        Assert.DoesNotContain("adr/", toc);
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
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
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
