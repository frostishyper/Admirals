using UnityEngine;
using UnityEngine.InputSystem;
// -- SCRIPT --
public class ScreenManagerScript : MonoBehaviour
{   
    // Static Title Screen Flag (For Now)
    private static bool TitlePassed = false;
    public static ScreenManagerScript Instance;

    // Screen(s) Game Object References
    public GameObject TitleScreen; 
    public GameObject MainMenu;
    public GameObject SinglePlayerScreen;
    public GameObject MultiPlayerScreen;
    public GameObject ManualScreen;
    public GameObject SettingsScreen;
    public GameObject CreditsScreen;

    void Awake()
    {
        Instance = this;
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

    // Show Screen Methods
    public void ShowMainMenu()
    {
        MainMenu.SetActive(true);
        SinglePlayerScreen.SetActive(false);
        MultiPlayerScreen.SetActive(false);
        ManualScreen.SetActive(false);
        SettingsScreen.SetActive(false);
        CreditsScreen.SetActive(false);
    }
    public void ShowSinglePlayer()
    {
        MainMenu.SetActive(false);
        SinglePlayerScreen.SetActive(true);
    }

    public void ShowMultiPlayer()
    {
        MainMenu.SetActive(false);
        MultiPlayerScreen.SetActive(true);
    }

    public void ShowManual()
    {
        MainMenu.SetActive(false);
        ManualScreen.SetActive(true);
    }

    public void ShowSettings()
    {
        MainMenu.SetActive(false);
        SettingsScreen.SetActive(true);
    }

    public void ShowCredits()
    {
        MainMenu.SetActive(false);
        CreditsScreen.SetActive(true);
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
