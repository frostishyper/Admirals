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
}
