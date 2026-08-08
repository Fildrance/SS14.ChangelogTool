using LibGit2Sharp;
using SS14.ChangelogTool.LocalGit.Models;

namespace SS14.ChangelogTool.LocalGit;

/// <summary>
/// Default implementation for <see cref="ILocalGitRepository"/>.
/// </summary>
public class LocalGitRepository : ILocalGitRepository
{
    private IRepository? _gitRepository;

    private IRepository InternalRepository
    {
        get
        {
            _gitRepository ??= GetLocalRepository();
            return _gitRepository;
        }
    }

    public LastCommitData? GetLastCommitData(string filePath)
    {
        // Query the commit history for this specific file path
        var fileHistory = InternalRepository.Commits.QueryBy(filePath)
                                       .FirstOrDefault();

        if (fileHistory != null)
        {
            var lastCommit = fileHistory.Commit;
            DateTimeOffset lastChangeDate = lastCommit.Committer.When;
            var sha = lastCommit.Id.Sha;
            return new LastCommitData(sha, lastChangeDate);
        }

        return null;
    }

    public IReadOnlyCollection<CommitBriefInfo> GetCommitsSince(string sinceSha)
    {
        var repository = InternalRepository;
        var baseCommit = repository.Lookup<Commit>(sinceSha);
        if (baseCommit == null)
        {
            throw new InvalidOperationException(
                $"Attempted to find base commit {sinceSha} to collect all changes from git history, but no such commit was found!"
            );
        }
        var filter = new CommitFilter
        {
            IncludeReachableFrom = repository.Head.Tip, // Start from the current branch tip
            ExcludeReachableFrom = baseCommit     // Stop at the target SHA (exclusive)
        };

        ICommitLog commitsSinceSha = repository.Commits.QueryBy(filter);

        return commitsSinceSha.Select(x => new CommitBriefInfo(x.Sha, x.MessageShort))
                              .ToArray();
    }

    public void Dispose()
    {
        if(_gitRepository != null)
        {
            _gitRepository.Dispose();
        }
    }

    private IRepository GetLocalRepository()
    {
        // Searches upward from the current directory
        string repoPath = Repository.Discover(".");

        if (repoPath == null) 
            throw new InvalidOperationException("Failed to find initialized local git repository.");

        return new Repository(repoPath);

    }
}