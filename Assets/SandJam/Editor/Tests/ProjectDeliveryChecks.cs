using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
namespace SandJamTest.Editor
{
    public static class ProjectDeliveryChecks
    {
        [MenuItem("Sand Jam/Validation/Build standalone project")]
        public static void Run()
        {
            BurstSandChecks.Run();
            var report=BuildPipeline.BuildPlayer(new[]{"Assets/SandJam/Scenes/SandJamGame.unity"},"Build/sandjamproject.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("Standalone project build failed");
        }
    }
}
