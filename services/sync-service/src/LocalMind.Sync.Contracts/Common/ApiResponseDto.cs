namespace LocalMind.Sync.Contracts.Common;

public sealed record ApiResponseDto<T>(bool Success, T? Data, ApiErrorDto? Error, ApiMetadataDto Metadata)
{
    public static ApiResponseDto<T> Ok(T data, string requestId)
    {
        return new ApiResponseDto<T>(true, data, null, ApiMetadataDto.Now(requestId));
    }

    public static ApiResponseDto<T> Fail(ApiErrorDto error, string requestId)
    {
        return new ApiResponseDto<T>(false, default, error, ApiMetadataDto.Now(requestId));
    }
}
