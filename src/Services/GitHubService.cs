using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GitDD.Models;

namespace GitDD.Services;

public sealed class GitHubService(HttpClient httpClient, ProfileCacheService cache)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<GitHubFetchResult> FetchProfileAsync(
        string username,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.Trim();

        if (!forceRefresh)
        {
            var cached = await cache.TryGetAsync(normalizedUsername);
            if (cached is not null)
            {
                return cached;
            }
        }
        else
        {
            await cache.RemoveAsync(normalizedUsername);
        }

        var result = await FetchFromApiAsync(normalizedUsername, cancellationToken);

        if (result.Status is GitHubFetchStatus.Success or GitHubFetchStatus.NotFound)
        {
            await cache.SetAsync(normalizedUsername, result);
        }

        return result;
    }

    private async Task<GitHubFetchResult> FetchFromApiAsync(string username, CancellationToken cancellationToken)
    {
        var encodedUsername = Uri.EscapeDataString(username);

        using var userResponse = await httpClient.GetAsync($"users/{encodedUsername}", cancellationToken);
        if (userResponse.StatusCode is HttpStatusCode.NotFound)
        {
            return new GitHubFetchResult { Status = GitHubFetchStatus.NotFound };
        }

        if (userResponse.StatusCode is HttpStatusCode.Forbidden)
        {
            return new GitHubFetchResult { Status = GitHubFetchStatus.RateLimited };
        }

        if (!userResponse.IsSuccessStatusCode)
        {
            return new GitHubFetchResult
            {
                Status = GitHubFetchStatus.Error,
                ErrorMessage = $"GitHub returned {(int)userResponse.StatusCode}."
            };
        }

        var user = await userResponse.Content.ReadFromJsonAsync<GitHubUser>(JsonOptions, cancellationToken);
        if (user is null)
        {
            return new GitHubFetchResult
            {
                Status = GitHubFetchStatus.Error,
                ErrorMessage = "Unable to read GitHub profile."
            };
        }

        var reposTask = FetchReposAsync(encodedUsername, cancellationToken);
        var eventsTask = FetchEventsAsync(encodedUsername, cancellationToken);
        await Task.WhenAll(reposTask, eventsTask);

        return new GitHubFetchResult
        {
            Status = GitHubFetchStatus.Success,
            Data = new GitHubProfileData
            {
                User = user,
                Repos = await reposTask,
                Events = await eventsTask
            }
        };
    }

    private async Task<IReadOnlyList<GitHubRepo>> FetchReposAsync(string encodedUsername, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"users/{encodedUsername}/repos?per_page=100&sort=pushed",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Forbidden)
        {
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<GitHubRepo>>(JsonOptions, cancellationToken) ?? [];
    }

    private async Task<IReadOnlyList<GitHubEvent>> FetchEventsAsync(string encodedUsername, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"users/{encodedUsername}/events/public?per_page=100",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Forbidden)
        {
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<GitHubEvent>>(JsonOptions, cancellationToken) ?? [];
    }
}
