using SS14.ChangelogTool.Models.GitHub;

namespace SS14.ChangelogTool.Clients;

/// <summary>
/// Wrapper for extracting GitHub pull request info through GraphQL API.
/// </summary>
public interface IGithubPullRequestClient
{
    /// <summary>
    /// Extracts pull requests that have merge date greater, then provided date.
    /// </summary>
    /// <param name="repo">Repo to inspect, includes both repository name and owner, as '{owner}\{repo}'.</param>
    /// <param name="pullRequestNumbers">List of pull request numbers that we should retrieve.</param>
    Task<IReadOnlyCollection<GitHubPullRequest>> GetPullRequests(string repo, IReadOnlyCollection<int> pullRequestNumbers);
}