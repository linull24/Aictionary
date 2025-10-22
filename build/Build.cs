using System;
using System.IO;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Publish);

    [Parameter("Configuration to build - Default is 'Release'")]
    readonly string Configuration = "Release";

    AbsolutePath SourceDirectory => RootDirectory / "Aictionary";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath WindowsArtifactDirectory => ArtifactsDirectory / "windows";
    AbsolutePath MacOSArtifactDirectory => ArtifactsDirectory / "macos";
    AbsolutePath ProjectFile => SourceDirectory / "Aictionary.csproj";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            if (Directory.Exists(ArtifactsDirectory))
                Directory.Delete(ArtifactsDirectory, true);
            Directory.CreateDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(ProjectFile));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(ProjectFile)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target PublishWindows => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetPublish(s => s
                .SetProject(ProjectFile)
                .SetConfiguration(Configuration)
                .SetRuntime("win-x64")
                .SetSelfContained(true)
                .SetPublishSingleFile(true)
                .SetPublishTrimmed(false)
                .SetOutput(WindowsArtifactDirectory / "Aictionary-win-x64"));

            Serilog.Log.Information($"Windows artifact published to: {WindowsArtifactDirectory}");
        });

    Target PublishMacOS => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            var publishOutput = MacOSArtifactDirectory / "publish";
            var appBundlePath = MacOSArtifactDirectory / "Aictionary.app";
            var contentsPath = appBundlePath / "Contents";
            var macOsPath = contentsPath / "MacOS";

            if (Directory.Exists(publishOutput))
                Directory.Delete(publishOutput, true);

            DotNetPublish(s => s
                .SetProject(ProjectFile)
                .SetConfiguration(Configuration)
                .SetRuntime("osx-arm64")
                .SetSelfContained(true)
                .SetPublishSingleFile(true)
                .SetPublishTrimmed(false)
                .SetOutput(publishOutput));

            if (Directory.Exists(appBundlePath))
                Directory.Delete(appBundlePath, true);

            Directory.CreateDirectory(macOsPath);
            CopyDirectoryContents(publishOutput, macOsPath);
            Directory.Delete(publishOutput, true);

            // Copy Info.plist and icon
            var infoSource = SourceDirectory / "Info.plist";
            var iconSource = SourceDirectory / "Assets" / "AppIcon.icns";
            var resourcesDir = contentsPath / "Resources";
            var infoTarget = contentsPath / "Info.plist";

            Directory.CreateDirectory(resourcesDir);
            if (File.Exists(infoSource))
            {
                File.Copy(infoSource, infoTarget, true);
            }
            if (File.Exists(iconSource))
            {
                File.Copy(iconSource, resourcesDir / "AppIcon.icns", true);
            }

            Serilog.Log.Information($"macOS artifact published to: {MacOSArtifactDirectory}");
        });

    Target Publish => _ => _
        .DependsOn(PublishWindows, PublishMacOS)
        .Executes(() =>
        {
            Serilog.Log.Information("All platforms published successfully!");
            Serilog.Log.Information($"Artifacts location: {ArtifactsDirectory}");
        });

    static void CopyDirectoryContents(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
        {
            var targetFile = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, targetFile, true);
        }

        foreach (var directory in Directory.GetDirectories(source))
        {
            var targetDirectory = Path.Combine(destination, Path.GetFileName(directory));
            CopyDirectoryContents(directory, targetDirectory);
        }
    }
}
