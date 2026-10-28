using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Bug Condition Exploration Test for GridManager/GameMap Dependency Removal
/// 
/// **CRITICAL**: This test MUST FAIL on unfixed code - failure confirms the bug exists
/// **DO NOT attempt to fix the test or the code when it fails**
/// 
/// This test encodes the expected behavior - it will validate the fix when it passes after implementation.
/// 
/// Property 1: Bug Condition - Compilation Failure Due to Obsolete GridManager/GameMap References
/// 
/// Validates that AI subsystem files compile successfully and contain NO GridManager/GameMap references:
/// - FleetCommander.cs should not have gridManager/gameMap field declarations or API calls
/// - MovementPlanner.cs should not have methods with GridManager parameters
/// - SearchPlanner.cs should not have context.GameMap API calls
/// 
/// Reference: RandomRoboAIController.cs has ZERO GridManager/GameMap references and works perfectly
/// with ParticipantGameSnapshot.
/// 
/// **Validates Requirements: 2.1, 2.2, 2.3, 2.4, 2.5**
/// </summary>
public class GridManagerDependencyTest : MonoBehaviour
{
    /// <summary>
    /// Runs the bug condition exploration test.
    /// This should be called from TestRunner or manually from Unity Editor.
    /// </summary>
    public static void RunTest()
    {
        Debug.Log("=== Bug Condition Exploration Test: GridManager/GameMap Dependency Removal ===");
        Debug.Log("EXPECTED: This test should FAIL on unfixed code to confirm the bug exists");
        Debug.Log("");

        bool allPassed = true;

        // Test FleetCommander.cs
        allPassed &= TestFile(
            "FleetCommander.cs",
            "Assets/Scripts/Game/AI/FleetCommander.cs",
            new[]
            {
                "gridManager",
                "gameMap",
                "FindObjectOfType<GridManager>",
                "FindObjectOfType<GameMap>",
                "GetComponent<GameMap>",
                "gridManager.width",
                "gridManager.height"
            },
            new[]
            {
                "GridManager"
            }
        );

        // Test MovementPlanner.cs
        allPassed &= TestFile(
            "MovementPlanner.cs",
            "Assets/Scripts/Game/AI/MovementPlanner.cs",
            new[]
            {
                "GridManager gridManager",
                "GameMap gameMap",
                "context.GridManager",
                "context.GameMap"
            },
            new[]
            {
                "GridManager",
                "GameMap"
            }
        );

        // Test SearchPlanner.cs
        allPassed &= TestFile(
            "SearchPlanner.cs",
            "Assets/Scripts/Game/AI/SearchPlanner.cs",
            new[]
            {
                "context.GameMap",
                "context.GridManager",
                "GameMap.TryGetUnitCoordinates"
            },
            new[]
            {
                "GameMap"
            }
        );

        Debug.Log("");
        if (allPassed)
        {
            Debug.Log("<color=green>[PASS]</color> All AI subsystem files have NO GridManager/GameMap references");
            Debug.Log("Expected behavior is satisfied - bug is FIXED");
        }
        else
        {
            Debug.LogError("[FAIL] AI subsystem files contain GridManager/GameMap references");
            Debug.LogError("Bug condition confirmed - obsolete references prevent clean compilation");
            Debug.LogError("");
            Debug.LogError("EXPECTED OUTCOME: Test FAILS on unfixed code (this is correct)");
            Debug.LogError("This proves the bug exists and these references must be removed");
        }

        Debug.Log("=== Test Complete ===");
    }

    /// <summary>
    /// Tests a single file for obsolete GridManager/GameMap references.
    /// </summary>
    /// <param name="fileName">Display name of the file</param>
    /// <param name="filePath">Relative path from project root</param>
    /// <param name="forbiddenPatterns">String patterns that should NOT appear in the file</param>
    /// <param name="forbiddenTypes">Type names that should NOT appear as method parameters</param>
    /// <returns>True if file passes (no obsolete references), false if fails</returns>
    private static bool TestFile(string fileName, string filePath, string[] forbiddenPatterns, string[] forbiddenTypes)
    {
        Debug.Log($"Testing {fileName}...");

        // Construct full path
        string fullPath = Path.Combine(Application.dataPath, "..", filePath);
        
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"  [ERROR] File not found: {fullPath}");
            return false;
        }

        string content = File.ReadAllText(fullPath);
        bool passed = true;

        // Check for forbidden string patterns (field declarations, API calls)
        foreach (string pattern in forbiddenPatterns)
        {
            if (content.Contains(pattern))
            {
                Debug.LogError($"  [FAIL] Found forbidden pattern: '{pattern}'");
                
                // Find line number for better diagnostics
                string[] lines = content.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(pattern))
                    {
                        Debug.LogError($"    Line {i + 1}: {lines[i].Trim()}");
                    }
                }
                
                passed = false;
            }
        }

        // Check for methods with forbidden parameter types
        foreach (string type in forbiddenTypes)
        {
            // Pattern: method declaration with type as parameter
            // Matches patterns like "Method(GridManager param)" or "Method(RuntimeUnit unit, GridManager grid)"
            string methodPattern = @"\b\w+\s*\([^)]*\b" + Regex.Escape(type) + @"\b[^)]*\)";
            
            var matches = Regex.Matches(content, methodPattern);
            if (matches.Count > 0)
            {
                Debug.LogError($"  [FAIL] Found {matches.Count} method(s) with {type} parameter:");
                
                foreach (Match match in matches)
                {
                    // Find line number
                    int lineNumber = content.Substring(0, match.Index).Split('\n').Length;
                    Debug.LogError($"    Line {lineNumber}: {match.Value.Trim()}");
                }
                
                passed = false;
            }
        }

        if (passed)
        {
            Debug.Log($"  <color=green>[PASS]</color> {fileName} has NO obsolete references");
        }
        else
        {
            Debug.LogError($"  [FAIL] {fileName} contains obsolete GridManager/GameMap references");
        }

        return passed;
    }

    /// <summary>
    /// Unity Editor menu item to run the test manually.
    /// </summary>
    [UnityEditor.MenuItem("Tests/AI/Run GridManager Dependency Test")]
    public static void RunTestFromMenu()
    {
        RunTest();
    }
}
