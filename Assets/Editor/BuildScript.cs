using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NomadGo.Editor
{
    /// <summary>
    /// Headless build script invoked by GitHub Actions (GameCI).
    /// Call via: -buildMethod NomadGo.Editor.BuildScript.BuildAndroid
    /// </summary>
    public static class BuildScript
    {
        private const string OutputDir = "Builds/Android";
        private const string ApkName  = "NomadGo-SpatialVision.apk";

        public static void BuildAndroid()
        {
            string outputPath = Path.Combine(OutputDir, ApkName);

            // Ensure output directory exists
            Directory.CreateDirectory(OutputDir);

            // Collect scenes from EditorBuildSettings
            string[] scenes = GetEnabledScenes();

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes            = scenes,
                locationPathName  = outputPath,
                target            = BuildTarget.Android,
                options           = BuildOptions.None,
            };

            // Apply Android-specific settings
            ApplyAndroidSettings();

            Debug.Log($"[BuildScript] Building Android APK → {outputPath}");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] Build succeeded — {summary.totalSize / 1024 / 1024} MB");
            }
            else
            {
                Debug.LogError($"[BuildScript] Build FAILED: {summary.result}");
                // Exit with non-zero code so CI marks the job as failed
                EditorApplication.Exit(1);
            }
        }

        // ─────────────────────────────────────────────────────────────────

        private static void ApplyAndroidSettings()
        {
            // Scripting backend: Mono (faster build, lower memory — suitable for emulator testing)
            // Switch to IL2CPP only when building for Play Store release.
            PlayerSettings.SetScriptingBackend(
                BuildTargetGroup.Android, ScriptingImplementation.Mono2x);

            // Architecture: ARMv7 + ARM64 (Mono supports both; IL2CPP would need ARM64-only)
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

            // Min / target SDK
            PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;

            // Portrait only
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // Scripting defines
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Android,
                "UNITY_AR;NOMADGO_SPATIAL;UNITY_BARRACUDA");

            // Debug keystore — suitable for emulator testing
            // (No signing needed for local adb install)
            PlayerSettings.Android.useCustomKeystore = false;

            Debug.Log("[BuildScript] Android player settings applied.");
        }

        private static string[] GetEnabledScenes()
        {
            var scenes = new System.Collections.Generic.List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    scenes.Add(scene.path);
            }

            if (scenes.Count == 0)
                throw new InvalidOperationException(
                    "[BuildScript] No enabled scenes found in EditorBuildSettings!");

            return scenes.ToArray();
        }
    }
}
