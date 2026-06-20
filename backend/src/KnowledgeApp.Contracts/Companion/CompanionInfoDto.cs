namespace KnowledgeApp.Contracts.Companion;

/// <summary>Lightweight info shown by the phone companion interface.</summary>
/// <param name="ComputerName">Name of the computer running LocalMind.</param>
public sealed record CompanionInfoDto(string ComputerName);
