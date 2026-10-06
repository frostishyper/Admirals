using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
// Defines An Onboard System Used To Search For Units
[CreateAssetMenu(
    fileName = "SearchSystem_New",
    menuName = "Admirals/Search System Definition"
)]
public class SearchSystemDefinition : OnboardSystemDefinition
{
    // Tile Pattern Used When Performing A Search
    [SerializeField] private PatternDefinition _SearchPattern;

    // Domains This Search System Is Capable Of Detecting
    [SerializeField] private Domain[] _DetectableDomains;


    // Read-Only Access For Other Systems
    public PatternDefinition SearchPattern
    {
        get { return _SearchPattern; }
    }

    public Domain[] DetectableDomains
    {
        get { return _DetectableDomains; }
    }
}