using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MysticCombat.Editor
{
    /// <summary>
    /// Automated build utility for exporting standalone game builds.
    /// Supports both interactive editor builds (via Menu or trigger file)
    /// and headless batchmode builds.
    /// </summary>
    [InitializeOnLoad]
    public static class BuildScript
    {
        private static readonly string TriggerFilePath = Path.Combine(Directory.GetCurrentDirectory(), "trigger_build.flag");

        static BuildScript()
        {
            EditorApplication.update += CheckTriggerFile;
        }

        private static void CheckTriggerFile()
        {
            if (File.Exists(TriggerFilePath))
            {
                try
                {
                    File.Delete(TriggerFilePath);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BuildScript] Could not delete trigger file: {ex.Message}");
                }

                Debug.Log("<color=#10b981>[BuildScript]</color> Build trigger detected! Starting Standalone Windows Build...");
                BuildWindows();
            }
        }

        [MenuItem("Build/Build Windows Standalone (x64)")]
        public static void BuildWindows()
        {
            string projectRoot = Directory.GetCurrentDirectory();
            string buildFolder = Path.Combine(projectRoot, "Build", "Windows");
            string statusFile = Path.Combine(projectRoot, "Build", "build_status.txt");

            if (!Directory.Exists(buildFolder))
            {
                Directory.CreateDirectory(buildFolder);
            }

            string exePath = Path.Combine(buildFolder, "Mystic_Combat_Animeverse_Incursion.exe");

            string[] scenes = new string[]
            {
                "Assets/Scenes/MainMenuScene.unity",
                "Assets/Scenes/SampleScene.unity"
            };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log($"<color=#38bdf8>[BuildScript]</color> ========================================");
            Debug.Log($"<color=#38bdf8>[BuildScript]</color> Starting Windows Standalone Build...");
            Debug.Log($"<color=#38bdf8>[BuildScript]</color> Target Executable: {exePath}");
            Debug.Log($"<color=#38bdf8>[BuildScript]</color> Scenes: {string.Join(", ", scenes)}");
            Debug.Log($"<color=#38bdf8>[BuildScript]</color> ========================================");

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                string info = $"STATUS: SUCCESS\n" +
                              $"EXE: {exePath}\n" +
                              $"SIZE_BYTES: {summary.totalSize}\n" +
                              $"TIME_SECONDS: {summary.totalTime.TotalSeconds:F1}\n" +
                              $"TIMESTAMP: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                File.WriteAllText(statusFile, info);

                Debug.Log($"<color=#10b981>[BuildScript] >>> BUILD SUCCEEDED! <<<</color>");
                Debug.Log($"<color=#10b981>[BuildScript] Output: {exePath} ({summary.totalSize / (1024 * 1024):F2} MB)</color>");

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            else
            {
                string errInfo = $"STATUS: FAILED\n" +
                                 $"ERRORS: {summary.totalErrors}\n" +
                                 $"WARNINGS: {summary.totalWarnings}\n" +
                                 $"TIMESTAMP: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                File.WriteAllText(statusFile, errInfo);

                Debug.LogError($"<color=#ef4444>[BuildScript] >>> BUILD FAILED! Errors: {summary.totalErrors} <<<</color>");

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }
    }
}
