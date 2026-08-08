using SS14.ChangelogTool.LocalGit.Models;

namespace SS14.ChangelogTool.LocalGit;

/// <summary>
/// Provider of local git repository from LibGit2Sharp.
/// </summary>
public interface ILocalGitRepository : IDisposable
{
    /// <summary>
    /// Gets last commit info for file on provided path. Returns null if file is not tracked by git or was not found.
    /// </summary>
    LastCommitData? GetLastCommitData(string filePath);

    /// <summary>
    /// Returns list of commits that were made since provided commit hash.
    /// </summary>
    IReadOnlyCollection<CommitBriefInfo> GetCommitsSince(string sinceSha);
}