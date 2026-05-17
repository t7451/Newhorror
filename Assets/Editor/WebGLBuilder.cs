using UnityEditor;
using System.Linq;

public static class WebGLBuilder
{
    public static void Build()
    {
        // Gather all scenes enabled in the project settings
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        // Force platform switch and disable compression for native GitHub Pages hosting compatibility
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        // Execute compilation
        BuildPipeline.BuildPlayer(scenes, "build/WebGL", BuildTarget.WebGL, BuildOptions.None);
    }
}
