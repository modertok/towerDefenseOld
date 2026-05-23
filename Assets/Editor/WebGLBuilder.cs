using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// WebGL build для GitHub Pages.
/// Виводить у /docs (звідки GitHub Pages може хостити).
/// Викликається з командного рядка:
///   Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod WebGLBuilder.BuildForPages -logFile -
/// </summary>
public static class WebGLBuilder
{
    [MenuItem("TowerDefense/3 - Build WebGL (to /docs)")]
    public static void BuildForPages()
    {
        string outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "docs");

        // Видаляємо попередній білд (щоб не змішувати файли різних версій)
        if (Directory.Exists(outDir))
        {
            try { Directory.Delete(outDir, true); } catch { }
        }
        Directory.CreateDirectory(outDir);

        // ── Сцени ─────────────────────────────────────────────────────────
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity",     true),
        };

        // ── WebGL-специфічні налаштування ─────────────────────────────────
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.WebGL.compressionFormat   = WebGLCompressionFormat.Disabled; // GitHub Pages не вміє Brotli auto
        PlayerSettings.WebGL.exceptionSupport    = WebGLExceptionSupport.None;
        PlayerSettings.WebGL.dataCaching         = true;
        PlayerSettings.WebGL.memorySize          = 256;
        PlayerSettings.runInBackground           = true;
        PlayerSettings.companyName               = "modertok";
        PlayerSettings.productName               = "TowerDefense";
        PlayerSettings.SplashScreen.show         = false;

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes           = new[] {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Game.unity"
            },
            locationPathName = outDir,
            target           = BuildTarget.WebGL,
            options          = BuildOptions.None,
        });

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[WebGLBuilder] OK -> {outDir} (size: {report.summary.totalSize / 1024 / 1024} MB)");
        }
        else
        {
            Debug.LogError($"[WebGLBuilder] FAILED: {report.summary.result}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
