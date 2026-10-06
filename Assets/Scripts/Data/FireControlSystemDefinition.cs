using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
// Defines The Area In Which A Unit Can Engage Detected Targets
[CreateAssetMenu(
    fileName = "FireControl_New",
    menuName = "Admirals/Fire Control System Definition"
)]
public class FireControlSystemDefinition : OnboardSystemDefinition
{
    // Tile Pattern Defining The Unit's Engagement Area
    [SerializeField] private PatternDefinition _EngagementPattern;


    // Read-Only Access For Other Systems
    public PatternDefinition EngagementPattern
    {
        get { return _EngagementPattern; }
    }
}