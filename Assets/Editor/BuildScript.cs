// BuildScript.cs
// Called by BUILD.bat via Unity's -executeMethod flag.
// Configures ALL project settings programmatically and builds the APK.
// No manual Project Settings editing required.

using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

public static class BuildScript
{
    public static void BuildAndroid()
    {
        Debug.Log("=== DichopticTetris: Starting Android Build ===");

        // ── 1. Player Settings ────────────────────────────────────────────
        PlayerSettings.companyName             = "VisionTherapy";
        PlayerSettings.productName             = "Dichoptic Tetris";
        PlayerSettings.bundleVersion           = "1.0.0";
        PlayerSettings.applicationIdentifier   = "com.visiontherapy.dichoptictetris";

        // Android-specific
        PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel31;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;

        // IL2CPP + ARM64 for Snapdragon 8 Gen 3 performance
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // Linear color space for OLED accuracy
        PlayerSettings.colorSpace = ColorSpace.Linear;

        // ── 2. Quality Settings ───────────────────────────────────────────
        QualitySettings.antiAliasing  = 4;   // 4x MSAA
        QualitySettings.vSyncCount    = 0;   // Disable — we control via Application.targetFrameRate

        // Physics fixed step = 1/120 to match display
        Time.fixedDeltaTime = 1f / 120f;

        // ── 3. Graphics API — Vulkan first, GLES3 fallback ───────────────
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
        {
            UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
            UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3,
        });

        // ── 4. Layers ─────────────────────────────────────────────────────
        // Layer 8 = LeftOnly | 9 = RightOnly | 10 = FusionLock
        SetLayer(8,  "LeftOnly");
        SetLayer(9,  "RightOnly");
        SetLayer(10, "FusionLock");

        // ── 5. Resolve output path ────────────────────────────────────────
        string outputDir = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", "BUILD_OUTPUT"));
        Directory.CreateDirectory(outputDir);
        string apkPath = Path.Combine(outputDir, "DichopticTetris.apk");

        // ── 6. Find scenes ────────────────────────────────────────────────
        // Use all enabled scenes from Build Settings, or default to MainScene
        var editorScenes = EditorBuildSettings.scenes;
        string[] scenePaths;
        if (editorScenes.Length > 0)
        {
            var enabledScenes = System.Array.FindAll(editorScenes, s => s.enabled);
            scenePaths = System.Array.ConvertAll(enabledScenes, s => s.path);
        }
        else
        {
            // Fallback: find any scene in Assets
            var guids = AssetDatabase.FindAssets("t:Scene");
            scenePaths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                scenePaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
        }

        if (scenePaths.Length == 0)
        {
            Debug.LogError("[Build] No scenes found! Add a scene to Build Settings.");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[Build] Building {scenePaths.Length} scene(s). Output: {apkPath}");

        // ── 7. Build ──────────────────────────────────────────────────────
        var options = new BuildPlayerOptions
        {
            scenes           = scenePaths,
            locationPathName = apkPath,
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
        };

        BuildReport  report  = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"=== BUILD SUCCEEDED === Size: {summary.totalSize / 1048576f:F1} MB | Path: {apkPath}");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"=== BUILD FAILED === Errors: {summary.totalErrors}");
            EditorApplication.Exit(1);
        }
    }

    // Sets a layer name via SerializedObject on TagManager
    private static void SetLayer(int index, string name)
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        if (layers == null || !layers.isArray) return;
        var element = layers.GetArrayElementAtIndex(index);
        if (element.stringValue == name) return;
        element.stringValue = name;
        tagManager.ApplyModifiedProperties();
        Debug.Log($"[Build] Layer {index} = \"{name}\"");
    }
}
