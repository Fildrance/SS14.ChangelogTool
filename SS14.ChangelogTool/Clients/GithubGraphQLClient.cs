using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Options;
using SS14.ChangelogTool.Models.GitHub;
using SS14.ChangelogTool.Options;

namespace SS14.ChangelogTool.Clients;

/// <inheritdoc/>
public class GithubGraphQLClient(IGraphQLClient graphQlClient, IOptions<ChangelogToolOptions> options) : IGithubGraphQLClient
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
        foreach (var batch in prNumberChunk)
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

    public async Task<IReadOnlyCollection<(string Sha, string RepoWithOwner)>> GetOwnedBy(IReadOnlyCollection<string> shaListToDiscover)
    {
        if (shaListToDiscover.Count == 0)
            return [];

        var chunkSize = options.Value.MaxCommitEntriesInGraphQLRequest;
        var chunks = shaListToDiscover.Distinct()
                                      .Chunk(chunkSize);

        var result = new List<(string Sha, string RepoWithOwner)>();

        foreach (var chunk in chunks) 
        {
            var queryParts = new List<string>();
            for (int i = 0; i < chunk.Length; i++)
            {
                queryParts.Add(
                    $$"""
                      commit_{{i}}: search(q: "{{chunk[i]}} is:pr is:merged", type: ISSUE, first: 1) {
                          nodes {
                              ... on PullRequest {
                                  url
                                  repository {
                                      nameWithOwner
                                  }
                              }
                          }
                      }
                      """
                    );
            }
            var query = "query {" + string.Join("", queryParts) + "}";

            var request = new GraphQLRequest(query);

            var response = await graphQlClient.SendQueryAsync<GitHubBatchResponse>(request);
            var data = response.Data.Commits.Values.SelectMany(x => x.Nodes);
            // graphQL guarantees ordering of results
            // https://github.com/graphql/graphql-spec/blob/main/spec/Section%207%20--%20Response.md#serialized-map-ordering
            var index = 0;
            foreach (var pullRequestInfo in data)
            {
                result.Add((chunk[index], pullRequestInfo.Repository.NameWithOwner));
                index++;
            }
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