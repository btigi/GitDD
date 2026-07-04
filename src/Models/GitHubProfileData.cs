namespace GitDD.Models;

public enum GitHubFetchStatus
{
    Success,
    NotFound,
    RateLimited,
    Error
}

public sealed class GitHubProfileData
{
    public required GitHubUser User { get; init; }
    public required IReadOnlyList<GitHubRepo> Repos { get; init; }
    public required IReadOnlyList<GitHubEvent> Events { get; init; }
}

public sealed class GitHubFetchResult
{
    public GitHubFetchStatus Status { get; init; }
    public GitHubProfileData? Data { get; init; }
    public string? ErrorMessage { get; init; }
}
