using System.Text.Json.Serialization;

namespace SS14.ChangelogTool.Models.GitHub;

public class RepositoryInfo
{
    [JsonPropertyName("nameWithOwner")]
    public string NameWithOwner { get; set; } = string.Empty;
}

public class PullRequestInfo
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("repository")]
    public RepositoryInfo Repository { get; set; } = new();
}

public class CommitSearchNode
{
    [JsonPropertyName("nodes")]
    public List<PullRequestInfo> Nodes { get; set; } = new();
}