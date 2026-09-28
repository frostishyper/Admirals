using UnityEngine;

// Attach to each GameObject that is a screen in the UI.
public class UIScreen : MonoBehaviour
{
    // The ID of this screen, used to identify it in the ScreenNavigator.
    [SerializeField] private ScreenID screenID;

    // Public read-only bridges so the Controller can see them
    public ScreenID ID {get {return screenID; }}
    public ScreenNavigator Navigator { get; private set; }
    
    public void Initialize(ScreenNavigator ScreenNavigatorInstance)
    {
        Navigator = ScreenNavigatorInstance;
    }

    // On & Off Switches
    public void ShowScreen()
    {
       gameObject.SetActive(true); 
    }

    public void HideScreen()
    {
        gameObject.SetActive(false);
    }
}
