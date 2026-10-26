using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Standalone script to run PreservationPropertyTest from command line
/// </summary>
public class RunPreservationTests
{
    public static void Main()
    {
        Console.WriteLine("Starting Preservation Property Tests...");
        
        try
        {
            // Use reflection to call the test method
            var testType = Type.GetType("PreservationPropertyTest");
            if (testType == null)
            {
                Console.Error.WriteLine("ERROR: Could not find PreservationPropertyTest type");
                Environment.Exit(1);
                return;
            }

            var runMethod = testType.GetMethod("RunAllTests", BindingFlags.Public | BindingFlags.Static);
            if (runMethod == null)
            {
                Console.Error.WriteLine("ERROR: Could not find RunAllTests method");
                Environment.Exit(1);
                return;
            }

            runMethod.Invoke(null, null);
            
            Console.WriteLine("Tests completed successfully");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
