using System;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Simple console runner for the GridManager Dependency Test
/// This can be run outside Unity to verify the test passes
/// </summary>
class RunBugTest
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Bug Condition Exploration Test: GridManager/GameMap Dependency Removal ===");
        Console.WriteLine("EXPECTED: This test should PASS on fixed code to confirm the bug is resolved");
        Console.WriteLine("");

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

        // Test AITurnContext.cs
        allPassed &= TestFile(
            "AITurnContext.cs",
            "Assets/Scripts/Game/AI/AITurnContext.cs",
            new[]
            {
                "context.GameMap",
                "context.GridManager",
                "gameMap =",
                "gridManager ="
            },
            new[]
            {
                "GameMap",
                "GridManager"
            }
        );

        Console.WriteLine("");
        if (allPassed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[PASS] All AI subsystem files have NO GridManager/GameMap references");
            Console.WriteLine("Expected behavior is satisfied - bug is FIXED");
            Console.ResetColor();
            Environment.Exit(0);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[FAIL] AI subsystem files contain GridManager/GameMap references");
            Console.WriteLine("Bug condition confirmed - obsolete references prevent clean compilation");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    static bool TestFile(string fileName, string filePath, string[] forbiddenPatterns, string[] forbiddenTypes)
    {
        Console.WriteLine($"Testing {fileName}...");

        if (!File.Exists(filePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [ERROR] File not found: {filePath}");
            Console.ResetColor();
            return false;
        }

        string content = File.ReadAllText(filePath);
        bool passed = true;

        // Check for forbidden string patterns (field declarations, API calls)
        foreach (string pattern in forbiddenPatterns)
        {
            if (content.Contains(pattern))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [FAIL] Found forbidden pattern: '{pattern}'");
                
                // Find line number for better diagnostics
                string[] lines = content.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(pattern))
                    {
                        Console.WriteLine($"    Line {i + 1}: {lines[i].Trim()}");
                    }
                }
                Console.ResetColor();
                
                passed = false;
            }
        }

        // Check for methods with forbidden parameter types
        foreach (string type in forbiddenTypes)
        {
            // Pattern: method declaration with type as parameter
            string methodPattern = @"\b\w+\s*\([^)]*\b" + Regex.Escape(type) + @"\b[^)]*\)";
            
            var matches = Regex.Matches(content, methodPattern);
            if (matches.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [FAIL] Found {matches.Count} method(s) with {type} parameter:");
                
                foreach (Match match in matches)
                {
                    // Find line number
                    int lineNumber = content.Substring(0, match.Index).Split('\n').Length;
                    Console.WriteLine($"    Line {lineNumber}: {match.Value.Trim()}");
                }
                Console.ResetColor();
                
                passed = false;
            }
        }

        if (passed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [PASS] {fileName} has NO obsolete references");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {fileName} contains obsolete GridManager/GameMap references");
            Console.ResetColor();
        }

        return passed;
    }
}
