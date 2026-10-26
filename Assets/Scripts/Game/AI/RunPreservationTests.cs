using UnityEngine;

/// <summary>
/// Test runner for Preservation Property Tests.
/// Executes preservation tests that validate snapshot-based AI behavior before bug fixes.
/// </summary>
public class RunPreservationTests : MonoBehaviour
{
    /// <summary>
    /// Runs preservation property tests from the Unity Editor menu.
    /// </summary>
    [UnityEditor.MenuItem("Tests/AI/Run All Preservation Tests")]
    public static void RunTests()
    {
        Debug.Log("<b>=== Running Preservation Property Tests ===</b>");
        Debug.Log("Testing snapshot-based AI behavior BEFORE fix...");
        Debug.Log("");

        PreservationPropertyTest.RunAllTests();
    }

    /// <summary>
    /// Unity Start method - can be attached to a GameObject to run tests on scene load.
    /// </summary>
    void Start()
    {
        if (Application.isPlaying)
        {
            RunTests();
        }
    }
}
