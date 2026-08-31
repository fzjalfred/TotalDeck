using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TotalDeck.EditorTools
{
    /// <summary>
    /// Windows x64 build entry point. Run from the menu
    /// (Tools/TotalDeck/Build Windows x64) or from the command line:
    ///   Tuanjie.exe -batchmode -quit -projectPath ^<proj^>
    ///     -executeMethod TotalDeck.EditorTools.BuildGame.BuildWindows
    /// </summary>
    public static class BuildGame
    {
        const string MainScene = "Assets/Scenes/TotalDeck.unity";

        static string OutputDir =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "Windows");

        [MenuItem("Tools/TotalDeck/Build Windows x64", false, 20)]
        public static void BuildWindows()
        {
            // Make sure the main scene is enabled in Build Settings
            var sceneList = EditorBuildSettings.scenes.ToList();
            if (!sceneList.Any(s => s.path == MainScene && s.enabled))
            {
                sceneList.Insert(0, new EditorBuildSettingsScene(MainScene, true));
                EditorBuildSettings.scenes = sceneList.ToArray();
                Debug.Log($"[BuildGame] added scene to build settings: {MainScene}");
            }

            string outputDir = OutputDir;
            // Clean stale output so no dead files ship with the new build
            if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
            Directory.CreateDirectory(outputDir);

            string exePath = Path.Combine(outputDir, "TotalDeck.exe");
            Debug.Log($"[BuildGame] building {PlayerSettings.productName} -> {exePath}");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = sceneList.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            var sum = report.summary;
            if (sum.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildGame] build FAILED: {sum.result}, errors={sum.totalErrors}, warnings={sum.totalWarnings}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[BuildGame] build Succeeded: {exePath} (size={sum.totalSize} bytes, warnings={sum.totalWarnings})");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
