using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// -- SCRIPT --
// Navigation Invoker Script for Handling Button Clicks for the BottomNav prefab & variants -Frostishyper
public class NavInvokerScript : MonoBehaviour
{
    // Obtain game object references for the buttons in the BottomNav prefab & variants -Frostishyper
    [SerializeField] private Button Exit_BTN;
    [SerializeField] private Button Back_BTN;
    [SerializeField] private Button Github_BTN;
    [SerializeField] private Button Credits_BTN;
    [SerializeField] private Button Settings_BTN;
    void Start()
    {
        // if statements makes sure that the script doesn't throw errors if the buttons are not present in the prefab variant -Frostishyper
        if (Exit_BTN != null)
        {
            Exit_BTN.onClick.AddListener(() => ScreenManagerScript.Instance.ShowQuit());
        }
        if (Back_BTN != null)
        {
            Back_BTN.onClick.AddListener(() => ScreenManagerScript.Instance.ShowMainMenu());
        }
        if (Github_BTN != null)
        {
            Github_BTN.onClick.AddListener(() => ScreenManagerScript.Instance.ShowGithub());
        }
        if (Credits_BTN != null)
        {
            Credits_BTN.onClick.AddListener(() => ScreenManagerScript.Instance.ShowCredits());
        }
        if (Settings_BTN != null)
        {
            Settings_BTN.onClick.AddListener(() => ScreenManagerScript.Instance.ShowSettings());
        }
    }

}
