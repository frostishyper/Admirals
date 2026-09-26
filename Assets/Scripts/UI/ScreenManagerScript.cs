using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
// -- SCRIPT --
public class ScreenManagerScript : MonoBehaviour
{   
    // Static Title Screen Flag (For Now) -Frostishyper
    private static bool TitlePassed = false;
    public static ScreenManagerScript Instance; // Singleton Instance Reference, allows for easy access to the instance from anywhere in the project -Frostishyper

    // Screen(s) Game Object References -Frostishyper
    public GameObject TitleScreen; 
    public GameObject MainMenu;
    public GameObject SinglePlayerScreen;
    public GameObject MultiPlayerScreen;
    public GameObject ManualScreen;
    public GameObject SettingsScreen;
    public GameObject CreditsScreen;
    public GameObject LoginScreen;
    
    void Awake()
    {
        Instance = this; // Set Singleton Instance Reference to the existing instance -Frostishyper
    }
    void Start()
    {   
        // Set Default States
        MainMenu.SetActive(false);
        SinglePlayerScreen.SetActive(false);
        MultiPlayerScreen.SetActive(false);
        ManualScreen.SetActive(false);
        SettingsScreen.SetActive(false);
        CreditsScreen.SetActive(false);

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

    // SHOW SCREEN METHODS -Frostishyper

    // Default to Main Menu, called when the game starts or when returning from other screens -Frostishyper
    public void ShowMainMenu()
    {
        MainMenu.SetActive(true);
        SinglePlayerScreen.SetActive(false);
        MultiPlayerScreen.SetActive(false);
        ManualScreen.SetActive(false);
        SettingsScreen.SetActive(false);
        CreditsScreen.SetActive(false);
    }

    // When Single Player is clicked (Main Menu) -Frostishyper
    public void ShowSinglePlayer()
    {
        MainMenu.SetActive(false);
        SinglePlayerScreen.SetActive(true);
    }

    //  When Multi Player is clicked (Main Menu) -Frostishyper
    public void ShowLogin()
    {
        MainMenu.SetActive(false);
        LoginScreen.SetActive(true);
    }

    // When Login is succesful or already logged in (Login) -Frostishyper
    public void ShowMultiPlayerScreen() {
        if (LoginScript.Username != null)
        {
            LoginScreen.SetActive(false);
            MultiPlayerScreen.SetActive(true);
        }
        else
        {
            MainMenu.SetActive(false);
            LoginScreen.SetActive(true);
        }
    }

    public void ShowLocalMP()
    {
        
    }

    public void ShowOnlineMP()
    {
        
    }

    // When Manual is clicked (Main Menu) -Frostishyper
    public void ShowManual()
    {
        MainMenu.SetActive(false);
        ManualScreen.SetActive(true);
    }


    // When Settings is clicked (Bottom Nav) -Frostishyper
    public void ShowSettings()
    {
        MainMenu.SetActive(false);
        SettingsScreen.SetActive(true);
    }

    // When Credits is clicked (Bottom Nav) -Frostishyper
    public void ShowCredits()
    {
        MainMenu.SetActive(false);
        CreditsScreen.SetActive(true);
    }

    // When Back is clicked (Bottom Nav) -Frostishyper
    public void ShowGithub()
    {
        Application.OpenURL("https://github.com/frostishyper/Admirals");
    }

    // When Quit is clicked (Anywhere) -Frostishyper
    public void ShowQuit()
    {
        Application.Quit();
    }
}
