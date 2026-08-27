# SS14 Changelog Tool

Small CLI tool to generate changelogs for SS14 repositories.

See [Configuration & HOW TO USE](README.Usage.md) for configuraiton instructions and usage instructions.

### Architecture notes
- Commands are registered in DI as `UpdateCommand`, `DumpDiffCommand`, and `SendWebhookCommand`.
- Pull requests are collected from **local git history** (via LibGit2Sharp): `update` finds the newest commit that touched the changelog files, `dump-diff` uses the provided `--sha`, and all commits reachable from the current branch tip are walked. PR numbers are extracted from commit messages (`(#NNN)` suffix) and PR details are fetched from GitHub in batches through the GraphQL API.


### Testing
- Local debug launch can be done using provided `launchSettings.json` - just fill required parts of configuration and run the project, profiles are named according to subcommands they invoke
- Unit tests live in `SS14.ChangelogTool.Tests`. Use XUnit, NSubstitute. Run tests with:

```powershell
dotnet test
```

### Building

```powershell
dotnet build
```

### CI/CD workflows

This repository has two GitHub Actions workflows:

- **Build and Test** (`.github/workflows/build.yml`) — runs on every pull request to `master`.  
  Restores dependencies, builds in Release mode, and runs all tests.

- **Build and Publish** (`.github/workflows/publish.yml`) — runs when a tag matching `v*` is pushed.  
  Builds, tests, packs the project into a NuGet package, and pushes it to `https://api.nuget.org/v3/index.json`  
  using the `NUGET_API_KEY` secret.

  The project is configured as a **.NET tool** with the command name `ss14-changelog`.  
  After publishing, anyone can install and run it.

### HOW TO USE

Tool installation:
```powershell
dotnet tool install --global SpaceWizards.ChangelogTool
ss14-changelog --help
```

To update to the latest version:
```powershell
dotnet tool update --global SpaceWizards.ChangelogTool
```

Using tool:

> The tool must be run inside a local clone/worktree of the SS14 repository with the branch you want to generate a
> changelog for checked out. Changelog entries are generated from commits reachable from the current branch tip.

```powershell
ss14-changelog update -d Resources\Changelog
# commit updated YMLs, build, run publish tests
```

- Creating a diff for a stable publish (`<last-publish-ref>` must exist in the local clone):

```powershell
ss14-changelog dump-diff -s <last-publish-ref> -c diff.md
ss14-changelog send-webhook -c diff.md
```
