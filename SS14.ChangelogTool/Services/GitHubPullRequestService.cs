using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SS14.ChangelogTool.Clients;
using SS14.ChangelogTool.Models.GitHub;
using SS14.ChangelogTool.Options;
using System.Text.RegularExpressions;
using SS14.ChangelogTool.LocalGit;
using SS14.ChangelogTool.LocalGit.Models;

namespace SS14.ChangelogTool.Services;

/// <inheritdoc/>
public partial class GitHubPullRequestService(
    IGithubGraphQLClient ghGraphQlClient,
    ILocalGitRepository repository,
    IOptions<ChangelogToolOptions> options,
    ILogger<GitHubPullRequestService> logger
) : IGitHubPullRequestService
{
    private readonly ChangelogToolOptions _options = options.Value;

    /// <summary>
    /// Matches the trailing PR reference that GitHub appends to squash merged commit messages, e.g. "... (#12345)".
    /// </summary>
    [GeneratedRegex(@"\(#(\d+)\)\s*$", RegexOptions.None)]
    private static partial Regex PullRequestNumberRegex();

    /// <summary>
    /// Detects whether a commit is a revert commit.
    /// </summary>
    [GeneratedRegex(@"\brevert\b", RegexOptions.IgnoreCase)]
    private static partial Regex RevertKeywordRegex();

    /// <summary>
    /// Matches every integer in a revert commit message, e.g. "Revert: 44644 - 40090 - 37716 - 42439 - 41004 (#44924)"
    /// yields 44644, 40090, 37716, 42439, 41004 and 44924. The caller should exclude the commit's own trailing PR number.
    /// </summary>
    [GeneratedRegex(@"\d+", RegexOptions.None)]
    private static partial Regex AnyNumberRegex();

    /// <inheritdoc/>
    public async Task<GitHubDiff> GetDiff(string sinceSha)
    {
        var repo = _options.Repo;

        var commitsSinceSha = repository.GetCommitsSince(sinceSha);

        var filtered = await FilterCommits(commitsSinceSha, repo);

        HashSet<int> pullRequestNumbers = new();
        HashSet<int> revertedPullRequestNumbers = new();

        foreach (var commit in filtered)
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

            if (RevertKeywordRegex().IsMatch(commit.MessageShort))
            {
                var revertedNumbers = AnyNumberRegex().Matches(commit.MessageShort)
                                                      .Select(x => x.Value)
                                                      .Select(int.Parse);

                foreach (var revertedNumber in revertedNumbers)
                {
                    if(revertedNumber == prNumber)
                        continue;

                    // if we added PR in this changelog update - no need
                    // to actually remove anything but that pr from current updates list
                    if(!pullRequestNumbers.Remove(revertedNumber))
                        revertedPullRequestNumbers.Add(revertedNumber);
                }
            }
        }

        logger.LogInformation(
            "Collected {count} pull request numbers and {revertCount} reverted pull request numbers since {sha}",
            pullRequestNumbers.Count,
            revertedPullRequestNumbers.Count,
            sinceSha
        );

        var pullRequests = await ghGraphQlClient.GetPullRequests(repo, pullRequestNumbers);
        pullRequests = pullRequests.OrderBy(item => item.MergedAt)
                                   .ToList();

        return new GitHubDiff(pullRequests, revertedPullRequestNumbers);
    }

    private async Task<IEnumerable<CommitBriefInfo>> FilterCommits(IReadOnlyCollection<CommitBriefInfo> commitsSinceSha, string repo)
    {
        if (!_options.IsProcessOnlyFromCurrentRepoEnabled)
            return commitsSinceSha;

        var shaListToDiscover = commitsSinceSha.Select(x => x.Sha)
                                               .ToArray();
        var withOwners = await ghGraphQlClient.GetOwnedBy(shaListToDiscover);

        var onlyFromCurrentRepo = withOwners.Where(x => x.RepoWithOwner == repo)
                                            .Select(x => x.Sha)
                                            .ToHashSet();

        return commitsSinceSha.Where(x => onlyFromCurrentRepo.Contains(x.Sha));
    }
}