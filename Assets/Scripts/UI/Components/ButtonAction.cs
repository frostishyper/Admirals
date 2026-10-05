using UnityEngine;
using UnityEngine.UI;

// Attach To Any Button That Navigates Screens
[RequireComponent(typeof(Button))]
public class ButtonAction : MonoBehaviour
{
    public enum ActionType
    {
        None,
        Push,
        Pop,
        Swap,
        OpenUrl,
        Quit
    }
    
    // Used For Buttons Wihtou Yet A Purpose
    [SerializeField] private ActionType Action = ActionType.None;

    // Used For Push Or Swap
    [SerializeField] private ScreenID Target;

    // Used For Opening URLs
    [SerializeField] private string Url;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        switch (Action)
        {
            case ActionType.Push:
                {
                    ScreenNavigator navigator = GetNavigator();
                    if (navigator != null) navigator.Push(Target);
                    break;
                }
            case ActionType.Pop:
                {
                    ScreenNavigator navigator = GetNavigator();
                    if (navigator != null) navigator.Pop();
                    break;
                }
            case ActionType.Swap:
                {
                    ScreenNavigator navigator = GetNavigator();
                    if (navigator != null) navigator.Swap(Target);
                    break;
                }
            case ActionType.OpenUrl:
                if (!string.IsNullOrEmpty(Url))
                {
                    Application.OpenURL(Url);
                }
                break;
            case ActionType.Quit:
                Application.Quit();
                break;
            case ActionType.None:
            default:
                // Intentionally does nothing. Lets a button exist in the scene without doing anything
                break;
        }
    }

    // To Get ScreenNavigator And Return It
    private ScreenNavigator GetNavigator()
    {
        UIScreen ParentScreen = GetComponentInParent<UIScreen>();
        ScreenNavigator Navigator = ParentScreen.Navigator;
        return Navigator;
    }
}
