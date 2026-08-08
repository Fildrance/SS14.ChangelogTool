using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SS14.ChangelogTool.Clients;
using SS14.ChangelogTool.Models.GitHub;
using SS14.ChangelogTool.Options;
using System.Text.RegularExpressions;
using SS14.ChangelogTool.LocalGit;

namespace SS14.ChangelogTool.Services;

/// <inheritdoc/>
public partial class GitHubPullRequestService(
    IGithubPullRequestClient ghPullRequestClient,
    ILocalGitRepository repository,
    IOptions<ChangelogToolOptions> options,
    ILogger<GitHubPullRequestService> logger
) : IGitHubPullRequestService
{
    private readonly ChangelogToolOptions _options = options.Value;

    [GeneratedRegex(@"\(#(\d+)\)\s*$", RegexOptions.None)]
    private static partial Regex PullRequestNumberRegex();

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<GitHubPullRequest>> GetDiff(string sinceSha)
    {
        var repo = _options.Repo;
        
        HashSet<int> pullRequestNumbers = new();
        
        var commitsSinceSha = repository.GetCommitsSince(sinceSha);
        foreach (var commit in commitsSinceSha)
        {
            var match = PullRequestNumberRegex().Match(commit.MessageShort);
            if (!match.Success)
            {
                logger.LogWarning(
                    "Commit {CommitSha} does not have a pull request number in its message: {CommitMessage}",
                    commit.Sha,
                    commit.MessageShort
                );
                continue;
            }

            var number = match.Groups[1].Value;
            if (!int.TryParse(number, out var prNumber))
            {
                logger.LogWarning(
                    "Commit {CommitSha} have problematic pattern in its message "
                    + "- it have PR number but it is not a valid number. Commit message: {CommitMessage}",
                    commit.Sha,
                    commit.MessageShort
                ); 
                continue;
            }

            pullRequestNumbers.Add(prNumber);
        }

        logger.LogInformation("Collected {count} pull request numbers since {sha}", pullRequestNumbers.Count, sinceSha);

        var pullRequests = await ghPullRequestClient.GetPullRequests(repo, pullRequestNumbers);
        pullRequests = pullRequests.OrderBy(item => item.MergedAt)
                                   .ToList();

        return pullRequests;
    }
}
