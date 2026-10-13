using UnityEngine;


// Persists The Finalized MatchConfig Across Scene Changes.
public class MatchSession :
    MonoBehaviour
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
            return
                ActiveMatch != null;
        }
    }


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(
                gameObject
            );

            return;
        }


        Instance =
            this;


        DontDestroyOnLoad(
            gameObject
        );
    }


    public void SetActiveMatch(
        MatchConfig Match
    )
    {
        ActiveMatch =
            Match;
    }


    public void ClearMatch()
    {
        ActiveMatch =
            null;
    }


    private void OnDestroy()
    {
        if (Instance ==
            this)
        {
            Instance =
                null;
        }
    }
}