namespace LocalMind.Sync.Contracts.Common;

public sealed record ApiErrorDto(string Code, string Message, IReadOnlyDictionary<string, string> Details);
