using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// -- SCRIPT --
// This script is responsible for handling the multiplayer choice screen, allowing players to select between local and online multiplayer modes. -Frostishyper
// currently mothballed, but will be used in the future for multiplayer selection -Frostishyper
public class MPchoiceScript : MonoBehaviour
{
    [SerializeField] private Button LocalMP_Choice;
    [SerializeField] private Button OnlineMP_Choice;
    
    void Start()
    {
        if (LocalMP_Choice != null)
        {
            LocalMP_Choice.onClick.AddListener(() => ScreenManagerScript.Instance.ShowLocalMP());
        }

        if (OnlineMP_Choice != null)
        {
            OnlineMP_Choice.onClick.AddListener(() => ScreenManagerScript.Instance.ShowOnlineMP());
        }
    }

}
