using System.Text.Json.Serialization;

namespace GitDD.Models;

public sealed class GitHubEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("payload")]
    public GitHubEventPayload? Payload { get; set; }
}

public sealed class GitHubEventPayload
{
    [JsonPropertyName("commits")]
    public List<GitHubPushCommit>? Commits { get; set; }
}

public sealed class GitHubPushCommit
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
