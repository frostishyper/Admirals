using UnityEngine;

// Attach To Session_Match GameObject
// Carries The Finalized Match Configuration Between Scenes
public class MatchSession : MonoBehaviour
{
    public static MatchSession Instance
    {
        get;
        private set;
    }


    public MatchConfig ActiveMatch
    {
        get;
        private set;
    }


    public bool HasActiveMatch
    {
        get
        {
            return ActiveMatch != null;
        }
    }


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }


        Instance = this;


        DontDestroyOnLoad(
            gameObject
        );
    }


    public void SetActiveMatch(
        MatchConfig Match
    )
    {
        if (Match == null)
        {
            Debug.LogError(
                "MatchSession cannot receive a null MatchConfig."
            );

            return;
        }


        ActiveMatch =
            Match;


        Debug.Log(
            "MatchSession received MatchConfig."
        );
    }


    public void ClearMatch()
    {
        ActiveMatch =
            null;
    }
}