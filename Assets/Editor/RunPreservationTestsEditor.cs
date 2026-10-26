using UnityEngine;
using UnityEditor;

/// <summary>
/// Unity Editor script to run PreservationPropertyTest
/// Execute via: Unity.exe -quit -batchmode -executeMethod RunPreservationTestsEditor.RunTests -logFile -
/// </summary>
public class RunPreservationTestsEditor
{
    [MenuItem("Tests/Run Preservation Tests (Exit)")]
    public static void RunTests()
    {
        Debug.Log("=== Running Preservation Property Tests (Batch Mode) ===");
        
        PreservationPropertyTest.RunAllTests();
        
        Debug.Log("=== Tests Complete - Exiting ===");
        
        #if UNITY_EDITOR
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
        #endif
    }
}
