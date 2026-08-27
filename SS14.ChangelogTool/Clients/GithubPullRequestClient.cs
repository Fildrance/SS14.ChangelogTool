using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Options;
using SS14.ChangelogTool.Models.GitHub;
using SS14.ChangelogTool.Options;

namespace SS14.ChangelogTool.Clients;

/// <inheritdoc/>
public class GithubPullRequestClient(IGraphQLClient graphQlClient, IOptions<ChangelogToolOptions> options) : IGithubPullRequestClient
{
    public const string GithubGraphQLApiBase = "https://api.github.com/graphql";

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<GitHubPullRequest>> GetPullRequests(
        string repo,
        IReadOnlyCollection<int> pullRequestNumbers
    )
    {
        if (pullRequestNumbers.Count == 0)
            return [];

        var (owner, repository) = ExtractParts(repo);

        var batchSize = options.Value.MaxPullRequestEntriesInGraphQLRequest;

        var result = new List<GitHubPullRequest>();

        var prNumberChunk = pullRequestNumbers.Distinct()
                                              .Chunk(batchSize);
        foreach (var batch in prNumberChunk) // todo: use AsyncEnumerator to avoid loading all PRs into memory at once
        {
            var pullRequestFields = string.Join(
                "\n",
                batch.Select(number => $$"""
                                         pr{{number}}: pullRequest(number: {{number}}) {
                                           merged
                                           body
                                           author {
                                             login
                                           }
                                           mergedAt
                                           baseRef {
                                             name
                                           }
                                           number
                                           url
                                         }
                                         """
                )
            );

            var query = $$"""
                          {
                            repository(owner: "{{owner}}", name: "{{repository}}") {
                              {{pullRequestFields}}
                            }
                          }
                          """;

            var request = new GraphQLRequest(query);

            var response = await graphQlClient.SendQueryAsync<GitHubPullRequestsResponse>(request);

            result.AddRange(response.Data.Repository.Values.Where(x => x is not null)!);
        }

        return result;
    }

    private static (string repo, string owner) ExtractParts(string repo)
    {
        var parts = repo.Split('/', 2);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException(
                $"Attempted to split repo name {repo} into repository name and owner parts, "
                + $"but splitting by '/' resulted in {parts.Length} parts!"
            );
        }

        return (parts[0], parts[1]);
    }
}