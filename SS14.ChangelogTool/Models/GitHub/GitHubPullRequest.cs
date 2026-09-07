namespace SS14.ChangelogTool.Models.GitHub;

public sealed record GitHubPullRequest(
    bool Merged,
    string? Body,
    GitHubUser? Author,
    DateTimeOffset? MergedAt,
    GitHubPullRequestBase? Base,
    int Number,
    string Url
);

public sealed class GitHubPullRequestsResponse
{
    public Dictionary<string, GitHubPullRequest?> Repository { get; set; } = [];
}