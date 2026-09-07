using System.CommandLine;
using SS14.ChangelogTool.Services;

namespace SS14.ChangelogTool.Commands;

public sealed class UpdateCommand : Command
{
    public UpdateCommand(ChangelogGeneratorService changelogGenerator, IChangelogFileManager changelogFileManager)
        : base("update", "Updates yml changelog files in resources")
    {
        var changelogDirOption = new Option<string>("--changelog-dir", "-d")
        {
            Description = "Path to the changelog directory inside repository folder",
            Required = true,
        };

        Options.Add(changelogDirOption);

        var allowCreateOption = new Option<bool>("--allow-create", "-ac")
        {
            Description = "Marker, if tool should create changelog file that it expects but failed to find, or just throw exception and stop",
            Required = false
        };

        Options.Add(allowCreateOption);

        SetAction(async parseResult =>
        {
            var changeLogDir = parseResult.GetValue(changelogDirOption)!;
            var allowCreate = parseResult.GetValue(allowCreateOption)!;
            return await changelogGenerator.TryGenerate(
                extraCategories => changelogFileManager.GetLastMergedSha(changeLogDir, extraCategories),
                (changelogs, revertedPrNumbers) => changelogFileManager.UpdateChangelogs(changelogs, revertedPrNumbers, changeLogDir, allowCreate)
            ) ? 0 : 1;
        });
    }
}
