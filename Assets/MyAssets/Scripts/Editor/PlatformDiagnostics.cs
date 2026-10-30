using UnityEngine;
using UnityEditor;

namespace PlatformCoinLab.Editor
{
    public static class PlatformDiagnostics
    {
        [MenuItem("Tools/Platform Lab/Print Build Target")]
        public static void PrintBuildTarget()
        {
            Debug.Log($"Build Target={EditorUserBuildSettings.activeBuildTarget}");
        }
    }
}