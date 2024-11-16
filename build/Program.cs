using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Common.Tools.DotNet.Test;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;

// プログラムのエントリーポイント
public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

// ビルド設定を保持するコンテキストクラス
public class BuildContext : FrostingContext
{
    public string[] BuildEnvironments { get; }
    public string BuildConfiguration { get; }
    public string OutputPath { get; }

    public BuildContext(ICakeContext context) : base(context)
    {
        // 利用可能な環境の定義
        BuildEnvironments = new[] { "Development", "Staging", "Production" };
        // コマンドライン引数からビルド設定を取得
        BuildConfiguration = context.Argument("configuration", "Release");
        OutputPath = context.Argument("output", "publish");
    }
}

// ビルドディレクトリのクリーンアップタスク
[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        // ビルド成果物のディレクトリをクリーンアップ
        context.CleanDirectory($"src/BatchApplication/bin/{context.BuildConfiguration}");
        context.CleanDirectory($"src/BatchApplication.Tests/bin/{context.BuildConfiguration}");
        context.CleanDirectory(context.OutputPath);
    }
}

// NuGetパッケージの復元タスク
[TaskName("Restore")]
[IsDependentOn(typeof(CleanTask))]
public sealed class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetRestore("BatchApplication.sln");
    }
}

// ソリューションのビルドタスク
[TaskName("Build")]
[IsDependentOn(typeof(RestoreTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetBuild("BatchApplication.sln", new DotNetBuildSettings {
            Configuration = context.BuildConfiguration,
            NoRestore = true
        });
    }
}

// テスト実行タスク
[TaskName("Test")]
[IsDependentOn(typeof(BuildTask))]
public sealed class TestTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetTest("BatchApplication.sln", new DotNetTestSettings {
            Configuration = context.BuildConfiguration,
            NoRestore = true,
            NoBuild = true
        });
    }
}

// 全環境向けの発行タスク
[TaskName("Publish-All")]
[IsDependentOn(typeof(TestTask))]
public sealed class PublishAllTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        foreach (var env in context.BuildEnvironments)
        {
            var outputPath = $"{context.OutputPath}/{env}";
            // アプリケーションの発行
            context.DotNetPublish("src/BatchApplication/BatchApplication.csproj", new DotNetPublishSettings {
                Configuration = context.BuildConfiguration,
                OutputDirectory = outputPath,
                NoRestore = true,
                Runtime = "win-x64",
                SelfContained = true,
                PublishSingleFile = true,
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = env
                }
            });

            // 環境固有の設定ファイルをコピー
            context.CopyFile(
                $"src/BatchApplication/appsettings.{env}.json",
                $"{outputPath}/appsettings.json"
            );

            // 実行ファイルの名前を環境ごとに変更
            var exePath = $"{outputPath}/BatchApplication.exe";
            var newExePath = $"{outputPath}/BatchApplication-{env}.exe";
            if (context.FileExists(exePath))
            {
                context.MoveFile(exePath, newExePath);
            }

            context.Log.Information($"Published {env} build to: {outputPath}");
        }
    }
}

// 特定環境向けの発行タスク
[TaskName("Publish")]
[IsDependentOn(typeof(TestTask))]
public sealed class PublishTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        // 環境名をコマンドライン引数から取得
        var env = context.Argument("environment", "Development");
        var outputPath = $"{context.OutputPath}/{env}";

        // アプリケーションの発行
        context.DotNetPublish("src/BatchApplication/BatchApplication.csproj", new DotNetPublishSettings {
            Configuration = context.BuildConfiguration,
            OutputDirectory = outputPath,
            NoRestore = false,
            Runtime = "win-x64",
            SelfContained = true,
            PublishSingleFile = true,
            EnvironmentVariables = new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = env
            }
        });

        // 環境固有の設定ファイルをコピー
        context.CopyFile(
            $"src/BatchApplication/appsettings.{env}.json",
            $"{outputPath}/appsettings.json"
        );

        // 実行ファイルの名前を環境ごとに変更
        var exePath = $"{outputPath}/BatchApplication.exe";
        var newExePath = $"{outputPath}/BatchApplication-{env}.exe";
        if (context.FileExists(exePath))
        {
            context.MoveFile(exePath, newExePath);
        }

        context.Log.Information($"Published {env} build to: {outputPath}");
    }
}

// デフォルトタスク（全環境向け発行を実行）
[TaskName("Default")]
[IsDependentOn(typeof(PublishAllTask))]
public sealed class DefaultTask : FrostingTask
{
}