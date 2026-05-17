using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class WebGLBuilder
{
    public static void Build()
    {
        // Gather all scenes enabled in the project settings
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
            .Select(s => s.path)
            .ToArray();

        // If no scenes are configured (e.g. fresh project skeleton with empty Scenes/ folder),
        // create and include a minimal default scene so the WebGL build can still complete.
        if (scenes.Length == 0)
        {
            scenes = new[] { EnsureDefaultScene() };
        }

        // Force platform switch and disable compression for native GitHub Pages hosting compatibility
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        // Execute compilation
        BuildPipeline.BuildPlayer(scenes, "build/WebGL", BuildTarget.WebGL, BuildOptions.None);
    }

    private static string EnsureDefaultScene()
    {
        const string scenePath = "Assets/HorrorCoopGame/Scenes/Default.unity";
        if (!File.Exists(scenePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();
        }

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        return scenePath;
    }
}
