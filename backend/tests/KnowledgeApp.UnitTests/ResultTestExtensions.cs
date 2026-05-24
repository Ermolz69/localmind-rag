using KnowledgeApp.Application.Common.Results;

namespace KnowledgeApp.UnitTests;

internal static class ResultTestExtensions
{
    public static T AssertSuccess<T>(this Result<T> result)
    {
        Assert.True(result.IsSuccess, result.Error?.Code);
        return result.Value!;
    }

    public static void AssertSuccess(this Result result)
    {
        Assert.True(result.IsSuccess, result.Error?.Code);
    }

    public static ApplicationError AssertFailure<T>(this Result<T> result, ErrorType? expectedType = null)
    {
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        if (expectedType.HasValue)
        {
            Assert.Equal(expectedType.Value, result.Error.Type);
        }

        return result.Error;
    }

    public static ApplicationError AssertFailure(this Result result, ErrorType? expectedType = null)
    {
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        if (expectedType.HasValue)
        {
            Assert.Equal(expectedType.Value, result.Error.Type);
        }

        return result.Error;
    }
}
