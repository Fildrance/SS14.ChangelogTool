using SS14.ChangelogTool.Models.GitHub;

namespace SS14.ChangelogTool.Services;

/// <summary>
/// Service for interacting with github api.
/// </summary>
public interface IGitHubPullRequestService
{
    /// <summary>
    /// Gets list of pull-requests that were merged since provided commit hash.
    /// Repo is set by app settings.
    /// </summary>
    Task<IReadOnlyCollection<GitHubPullRequest>> GetDiff(string sinceSha);
}
