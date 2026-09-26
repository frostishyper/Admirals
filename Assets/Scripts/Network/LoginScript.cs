using UnityEngine;
using System.Collections;

// -- SCRIPT --
// for now its just username validation done in game, but later it will be done through server validation for fully pledged multiplayer -Frostishyper
public class LoginScript : MonoBehaviour
{
    // Attached Prefab Reference In Editor
    [SerializeField] private PromptVerifiedScript UsernamePrompt;
    public static string Username; // Static variable to hold the username across scenes  -Frostishyper
    
    private void OnEnable()
    {
        // initialize prompt with a title and validation callback for this usecase -Frostishyper
        UsernamePrompt.gameObject.SetActive(true);
        UsernamePrompt.Initialize("Enter Username", ValidateUsername);
    }

    // Validation Callback for Username Input -Frostishyper
    private (bool isValid, string message) ValidateUsername(string username)
    {
        // Basic sanity checks for username length and emptiness, can be expanded later for more complex validation -Frostishyper
        if (string.IsNullOrWhiteSpace(username))
        {
            return (false, "Username cannot be empty.");
        }
        else if (username.Length < 4)
        {
            return (false, "Username must be at least 4 characters long.");
        }
        else if (username.Length > 15)
        {
            return (false, "Username cannot exceed 15 characters.");
        }
        else
        {
            Username = username;
            StartCoroutine(ProceedToMultiplayer());
            return (true, "Username is valid.");
        }
    }

    private IEnumerator ProceedToMultiplayer()
    {
        // Wait for 2 seconds to allow the user to read the success message before proceeding -Frostishyper
        yield return new WaitForSeconds(2f);
        ScreenManagerScript.Instance.ShowMultiPlayerScreen();
    }
}
