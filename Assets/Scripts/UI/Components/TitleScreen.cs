using UnityEngine;
using UnityEngine.InputSystem;

// Space To Continue Behaviour For Title Screen
[RequireComponent(typeof(UIScreen))]
public class TitleScreen : MonoBehaviour
{   
    private UIScreen Screen;
    
    private void Awake()
    {
        Screen = GetComponent<UIScreen>();
    }

    
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Screen.Navigator.Swap(ScreenID.Screen_MainMenu);
        }
    }
}
