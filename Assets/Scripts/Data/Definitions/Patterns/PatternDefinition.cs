using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
[CreateAssetMenu(
    fileName = "Pattern_New",
    menuName = "Admirals/Pattern Definition"
)]
public class PatternDefinition : ScriptableObject
{
    // Display Name Of The Pattern
    [SerializeField] private string _PatternName;

    // Unique ID Used To Identify The Pattern
    [SerializeField] private string _PatternID;

    // Ordered Layers That Make Up The Pattern
    [SerializeField] private PatternLayer[] _Layers;


    // Read-Only Access For Other Systems
    public string PatternName
    {
        get { return _PatternName; }
    }

    public string PatternID
    {
        get { return _PatternID; }
    }

    public PatternLayer[] Layers
    {
        get { return _Layers; }
    }
}