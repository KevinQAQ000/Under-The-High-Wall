using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 把当前工程导出成 WebGL，输出到 Builds/WebGL，用本地静态服务器打开 index.html 就能玩。
public static class DemoWebGLBuild
{
    private const string OutputDir = "Builds/WebGL";

    [MenuItem("高墙之下/导出 WebGL")]
    public static void Run()
    {
        // 本地文件服务器不会带 Brotli 响应头，关掉压缩让 index.html 能直接跑。
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.runInBackground = true;

        Directory.CreateDirectory(OutputDir);

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = OutputDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"WebGL 导出完成：{Path.GetFullPath(OutputDir)}（{report.summary.totalSize / 1024 / 1024} MB）");
        else
            Debug.LogError($"WebGL 导出失败：{report.summary.result}");
    }
}
