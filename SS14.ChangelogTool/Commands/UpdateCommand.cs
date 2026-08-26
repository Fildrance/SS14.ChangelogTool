using System.CommandLine;
using SS14.ChangelogTool.Services;

namespace SS14.ChangelogTool.Commands;

public sealed class UpdateCommand : Command
{
    public UpdateCommand(ChangelogGeneratorService changelogGenerator, IChangelogFileManager changelogFileManager)
        : base("update", "Updates the changelog.yml files in resources")
    {
        var changelogDirOption = new Option<string>("--changelog-dir", "-d")
        {
            Description = "Path to the changelog directory inside repository folder",
            Required = true,
        };
        Options.Add(changelogDirOption);

        var sinceShaOption = new Option<string?>("--since-sha", "-s")
        {
            Description = "sha since which changes should be collected. "
                          + "If not provided - last commit that changed "
                          + "any of accepted Changelog files will be used instead",
            Required = false
        };
        Options.Add(sinceShaOption);

        SetAction(async parseResult =>
        {
            var changeLogDir = parseResult.GetValue(changelogDirOption)!;
            var sinceSha = parseResult.GetValue(sinceShaOption);

            return await changelogGenerator.TryGenerate(
                extraCategories => sinceSha ?? changelogFileManager.GetLastMergedSha(changeLogDir, extraCategories),
                (changelogs, revertedPrNumbers) => changelogFileManager.UpdateChangelogs(changelogs, revertedPrNumbers, changeLogDir)
            ) ? 0 : 1;
        });
    }
}
