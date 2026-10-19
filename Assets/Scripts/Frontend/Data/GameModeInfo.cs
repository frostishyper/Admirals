using UnityEngine;


// ScriptableObject Asset - Frontend Selectable GameMode Information
[CreateAssetMenu(
    fileName = "GameMode_New",
    menuName = "Admirals/GameMode Info"
)]
public class GameModeInfo :
    ScriptableObject
{
    [SerializeField]
    private string _GameModeName;

    [SerializeField]
    private string _GameModeID;

    [SerializeField]
    private Definition_GameMode _GameModeReference;


    public string GameModeName
    {
        get
        {
            return _GameModeName;
        }
    }


    public string GameModeID
    {
        get
        {
            return _GameModeID;
        }
    }


    public Definition_GameMode GameModeReference
    {
        get
        {
            return _GameModeReference;
        }
    }
}