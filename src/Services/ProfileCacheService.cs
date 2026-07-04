using System.Text.Json;
using GitDD.Models;
using Microsoft.JSInterop;

namespace GitDD.Services;

public sealed class ProfileCacheService(IJSRuntime jsRuntime)
{
    private static readonly TimeSpan SuccessTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan NotFoundTtl = TimeSpan.FromMinutes(10);
    private const string KeyPrefix = "GitDD:profile:";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<GitHubFetchResult?> TryGetAsync(string username)
    {
        var key = BuildKey(username);
        var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        CachedProfileEntry? entry;
        try
        {
            entry = JsonSerializer.Deserialize<CachedProfileEntry>(json, JsonOptions);
        }
        catch (JsonException)
        {
            await RemoveAsync(username);
            return null;
        }

        if (entry is null)
        {
            return null;
        }

        var ttl = entry.Status is GitHubFetchStatus.NotFound ? NotFoundTtl : SuccessTtl;
        if (DateTimeOffset.UtcNow - entry.CachedAt > ttl)
        {
            await RemoveAsync(username);
            return null;
        }

        return new GitHubFetchResult
        {
            Status = entry.Status,
            Data = entry.Data,
            ErrorMessage = entry.ErrorMessage
        };
    }

    public async Task SetAsync(string username, GitHubFetchResult result)
    {
        if (result.Status is not (GitHubFetchStatus.Success or GitHubFetchStatus.NotFound))
        {
            return;
        }

        var entry = new CachedProfileEntry
        {
            CachedAt = DateTimeOffset.UtcNow,
            Status = result.Status,
            Data = result.Data,
            ErrorMessage = result.ErrorMessage
        };

        var json = JsonSerializer.Serialize(entry, JsonOptions);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", BuildKey(username), json);
    }

    public Task RemoveAsync(string username) =>
        jsRuntime.InvokeVoidAsync("localStorage.removeItem", BuildKey(username)).AsTask();

    private static string BuildKey(string username) =>
        KeyPrefix + username.Trim().ToLowerInvariant();

    private sealed class CachedProfileEntry
    {
        public DateTimeOffset CachedAt { get; set; }
        public GitHubFetchStatus Status { get; set; }
        public GitHubProfileData? Data { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
