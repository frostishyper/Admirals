using UnityEngine;

/// <summary>
/// Simple test runner for AI system unit tests.
/// Attach to a GameObject in the scene and enable 'Run On Start' to execute tests automatically.
/// </summary>
public class TestRunner : MonoBehaviour
{
    [Header("Test Configuration")]
    [Tooltip("Run tests automatically on Start")]
    public bool runOnStart = false;


    [Tooltip("Run AIDataModels validation")]
    public bool runDataModelsTests = true;

    [Tooltip("Run Preservation Property tests")]
    public bool runPreservationTests = true;

    private void Start()
    {
        if (runOnStart)
        {
            RunTests();
        }
    }

    /// <summary>
    /// Runs all enabled test suites.
    /// Can be called from Unity Inspector or external scripts.
    /// </summary>
    [ContextMenu("Run All Tests")]
    public void RunTests()
    {
        Debug.Log("=== AI Test Runner Starting ===");

        if (runDataModelsTests)
        {
            Debug.Log("\n--- Running AIDataModels Tests ---");
            AIDataModelsTest.ValidateDataModels();
        }

        {
            Debug.Log("\n--- Running DeploymentPlanner Tests ---");
        }

        if (runPreservationTests)
        {
            Debug.Log("\n--- Running Preservation Property Tests ---");
            PreservationPropertyTest.RunAllTests();
        }

        Debug.Log("\n=== AI Test Runner Completed ===");
    }
}

