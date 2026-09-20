using UnityEngine;
using UnityEngine.InputSystem;
// -- SCRIPT --
public class ScreenManagerScript : MonoBehaviour
{   
    // Static Title Screen Flag (For Now)
    private static bool TitlePassed = false;

    // Screen(s) Game Object References
    public GameObject TitleScreen; 
    public GameObject MainMenu;

    // Navigation References


    void Start()
    {   
        // Set Default States
        MainMenu.SetActive(false);

        if (TitlePassed == false)
        {
            TitleScreen.SetActive(true);
        }
        else
        {
            TitleScreen.SetActive(false);
            MainMenu.SetActive(true);
            TitlePassed = true;
        }
    }

    void Update()
    {
        if (TitleScreen.activeInHierarchy && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TitlePassed = true;
            TitleScreen.SetActive(false);
            MainMenu.SetActive(true);
        }
    }

    // Show Screen Methods
    public void ShowSinglePlayer()
    {
        
    }

    public void ShowMultiPlayer()
    {
        
    }

    public void ShowManual()
    {
        
    }

    public void ShowSettings()
    {
        
    }

    public void ShowCredits()
    {
        
    }

    public void ShowGithub()
    {
        Application.OpenURL("https://github.com/frostishyper/Admirals");
    }
    public void ShowQuit()
    {
        Application.Quit();
    }
}
