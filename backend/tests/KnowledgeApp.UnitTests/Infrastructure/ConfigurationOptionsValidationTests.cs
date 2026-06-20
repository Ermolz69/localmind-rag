using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Options.Validation;

using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Infrastructure;

public sealed class ConfigurationOptionsValidationTests
{
    [Fact]
    public void RuntimeOptionsValidator_Should_Accept_Valid_Configuration()
    {
        RuntimeOptions options = new();

        ValidateOptionsResult result =
            new RuntimeOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void RuntimeOptionsValidator_Should_Reject_Missing_Provider()
    {
        RuntimeOptions options = new()
        {
            Provider = string.Empty,
        };

        ValidateOptionsResult result =
            new RuntimeOptionsValidator().Validate(null, options);

        AssertFailure(result, "Ai:Provider is required.");
    }

    [Fact]
    public void RuntimeOptionsValidator_Should_Reject_Unsupported_Provider()
    {
        RuntimeOptions options = new()
        {
            Provider = "MissingProvider",
        };

        ValidateOptionsResult result =
            new RuntimeOptionsValidator().Validate(null, options);

        AssertFailure(result, "Ai:Provider must be one of: LlamaCpp or Stub.");
    }

    [Fact]
    public void RuntimeOptionsValidator_Should_Reject_Invalid_BaseUrl()
    {
        RuntimeOptions options = new()
        {
            BaseUrl = "not-a-url",
        };

        ValidateOptionsResult result =
            new RuntimeOptionsValidator().Validate(null, options);

        AssertFailure(
            result,
            "Ai:BaseUrl must be an absolute HTTP or HTTPS URL.");
    }

    [Fact]
    public void RuntimeOptionsValidator_Should_Reject_Empty_ChatModel()
    {
        RuntimeOptions options = new()
        {
            ChatModel = " ",
        };

        ValidateOptionsResult result =
            new RuntimeOptionsValidator().Validate(null, options);

        AssertFailure(result, "Ai:ChatModel is required.");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(2.1)]
    public void RuntimeOptionsValidator_Should_Reject_Invalid_Temperature(double temperature)
    {
        RuntimeOptions options = new()
        {
            Temperature = temperature,
        };

        ValidateOptionsResult result =
            new RuntimeOptionsValidator().Validate(null, options);

        AssertFailure(result, "Ai:Temperature must be between 0 and 2.");
    }

    [Fact]
    public void RuntimeOptionsValidator_Should_Reject_Invalid_ContextSize()
    {
        RuntimeOptions options = new()
        {
            ContextSize = 0,
        };

        ValidateOptionsResult result =
            new RuntimeOptionsValidator().Validate(null, options);

        AssertFailure(result, "Ai:ContextSize must be greater than zero.");
    }

    [Fact]
    public void RuntimeOptionsValidator_Should_Reject_Missing_RuntimePath()
    {
        RuntimeOptions options = new()
        {
            RuntimePath = string.Empty,
        };

        ValidateOptionsResult result =
            new RuntimeOptionsValidator().Validate(null, options);

        AssertFailure(
            result,
            "Ai:RuntimePath must be a valid non-empty file path.");
    }

    [Fact]
    public void RuntimeOptionsValidator_Should_Reject_Invalid_RuntimeDownloadUrl()
    {
        RuntimeOptions options = new()
        {
            RuntimeDownloadUrl = "runtime.zip",
        };

        ValidateOptionsResult result =
            new RuntimeOptionsValidator().Validate(null, options);

        AssertFailure(
            result,
            "Ai:RuntimeDownloadUrl must be an absolute HTTP or HTTPS URL.");
    }

    [Fact]
    public void EmbeddingOptionsValidator_Should_Accept_Valid_Configuration()
    {
        EmbeddingOptions options = new();

        ValidateOptionsResult result =
            new EmbeddingOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void EmbeddingOptionsValidator_Should_Reject_Unsupported_Provider()
    {
        EmbeddingOptions options = new()
        {
            EmbeddingProvider = "MissingProvider",
        };

        ValidateOptionsResult result =
            new EmbeddingOptionsValidator().Validate(null, options);

        AssertFailure(
            result,
            "Ai:EmbeddingProvider must be one of: LlamaCpp or Stub.");
    }

    [Fact]
    public void EmbeddingOptionsValidator_Should_Reject_Missing_Model()
    {
        EmbeddingOptions options = new()
        {
            EmbeddingModel = string.Empty,
        };

        ValidateOptionsResult result =
            new EmbeddingOptionsValidator().Validate(null, options);

        AssertFailure(result, "Ai:EmbeddingModel is required.");
    }

    [Fact]
    public void EmbeddingOptionsValidator_Should_Reject_Missing_ModelManifest()
    {
        EmbeddingOptions options = new()
        {
            EmbeddingModelManifest = string.Empty,
        };

        ValidateOptionsResult result =
            new EmbeddingOptionsValidator().Validate(null, options);

        AssertFailure(result, "Ai:EmbeddingModelManifest is required.");
    }

    [Fact]
    public void EmbeddingOptionsValidator_Should_Reject_Missing_ModelsPath()
    {
        EmbeddingOptions options = new()
        {
            ModelsPath = string.Empty,
        };

        ValidateOptionsResult result =
            new EmbeddingOptionsValidator().Validate(null, options);

        AssertFailure(
            result,
            "Ai:ModelsPath must be a valid non-empty directory path.");
    }

    [Fact]
    public void EmbeddingOptionsValidator_Should_Reject_Invalid_TopK()
    {
        EmbeddingOptions options = new()
        {
            TopK = 0,
        };

        ValidateOptionsResult result =
            new EmbeddingOptionsValidator().Validate(null, options);

        AssertFailure(result, "Ai:TopK must be greater than zero.");
    }

    [Fact]
    public void StorageOptionsValidator_Should_Accept_Valid_Configuration()
    {
        StorageOptions options = new();

        ValidateOptionsResult result =
            new StorageOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("DataPath", "LocalRuntime:DataPath must be a valid non-empty directory path.")]
    [InlineData("FilesPath", "LocalRuntime:FilesPath must be a valid non-empty directory path.")]
    [InlineData("PreviewsPath", "LocalRuntime:PreviewsPath must be a valid non-empty directory path.")]
    [InlineData("LogsPath", "LocalRuntime:LogsPath must be a valid non-empty directory path.")]
    public void StorageOptionsValidator_Should_Reject_Missing_Path(
        string property,
        string expectedFailure)
    {
        StorageOptions options = new();

        switch (property)
        {
            case "DataPath":
                options.DataPath = string.Empty;
                break;
            case "FilesPath":
                options.FilesPath = string.Empty;
                break;
            case "PreviewsPath":
                options.PreviewsPath = string.Empty;
                break;
            case "LogsPath":
                options.LogsPath = string.Empty;
                break;
        }

        ValidateOptionsResult result =
            new StorageOptionsValidator().Validate(null, options);

        AssertFailure(result, expectedFailure);
    }

    [Fact]
    public void DocumentPreviewOptionsValidator_Should_Accept_Valid_Configuration()
    {
        DocumentPreviewOptions options = new();

        ValidateOptionsResult result =
            new DocumentPreviewOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void DocumentPreviewOptionsValidator_Should_Reject_Invalid_Timeout()
    {
        DocumentPreviewOptions options = new()
        {
            ConversionTimeoutSeconds = 0,
        };

        ValidateOptionsResult result =
            new DocumentPreviewOptionsValidator().Validate(null, options);

        AssertFailure(
            result,
            "DocumentPreview:ConversionTimeoutSeconds must be greater than zero.");
    }

    [Fact]
    public void DatabaseOptionsValidator_Should_Accept_Valid_Configuration()
    {
        DatabaseOptions options = new();

        ValidateOptionsResult result =
            new DatabaseOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void DatabaseOptionsValidator_Should_Reject_Missing_DatabasePath()
    {
        DatabaseOptions options = new()
        {
            DatabasePath = string.Empty,
        };

        ValidateOptionsResult result =
            new DatabaseOptionsValidator().Validate(null, options);

        AssertFailure(
            result,
            "LocalRuntime:DatabasePath must be a valid non-empty file path.");
    }

    [Fact]
    public void VectorIndexOptionsValidator_Should_Accept_Valid_Configuration()
    {
        VectorIndexOptions options = new();

        ValidateOptionsResult result =
            new VectorIndexOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void VectorIndexOptionsValidator_Should_Reject_Missing_IndexPath()
    {
        VectorIndexOptions options = new()
        {
            IndexPath = string.Empty,
        };

        ValidateOptionsResult result =
            new VectorIndexOptionsValidator().Validate(null, options);

        AssertFailure(
            result,
            "LocalRuntime:IndexPath must be a valid non-empty directory path.");
    }

    private static void AssertFailure(
        ValidateOptionsResult result,
        string expectedFailure)
    {
        Assert.True(result.Failed);

        Assert.Contains(
            expectedFailure,
            result.Failures ?? []);
    }
}
