using System.Text.Json.Serialization;

namespace SS14.ChangelogTool.Models.GitHub;

public class RepositoryCommitSearchResponse
{
    public Dictionary<string, CommitObjectResult?> Repository { get; set; } = [];
}

public class CommitObjectResult
{
    [JsonPropertyName("associatedPullRequests")]
    public AssociatedPullRequestsConnection? AssociatedPullRequests { get; set; }
}

public class AssociatedPullRequestsConnection
{
    [JsonPropertyName("nodes")]
    public List<AssociatedPullRequestInfo> Nodes { get; set; } = [];
}

public class AssociatedPullRequestInfo
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("mergeCommit")]
    public MergeCommitInfo? MergeCommit { get; set; }
}

public class MergeCommitInfo
{
    [JsonPropertyName("oid")]
    public string? Oid { get; set; }
}