using UnityEngine;
using UnityEngine.Tilemaps;

// ScriptableObject Asset - Not Attached To A GameObject
// Defines An Onboard System Capable Of Deploying Persistent Naval Mines
[CreateAssetMenu(
    fileName = "MineDeploymentSystem_New",
    menuName = "Admirals/Systems/Mine Deployment System"
)]
public class MineDeploymentSystemDefinition :
    OnboardSystemDefinition
{
    [Header("Deployment")]
    [ScriptableObjectPicker]
    [SerializeField] private PatternDefinition _DeploymentPattern;

    [SerializeField] private TerrainType[] _DeployableTerrainTypes;

    [SerializeField] private TurnPhase[] _UsablePhases;


    [Header("Mine")]
    [ScriptableObjectPicker]
    [SerializeField] private Domains[] _AffectedDomains;

    [SerializeField] private TileBase _MineTile;

    [SerializeField] private MineRollRange[] _RollRanges;


    public PatternDefinition DeploymentPattern
    {
        get { return _DeploymentPattern; }
    }

    public TerrainType[] DeployableTerrainTypes
    {
        get { return _DeployableTerrainTypes; }
    }

    public TurnPhase[] UsablePhases
    {
        get { return _UsablePhases; }
    }

    public Domains[] AffectedDomains
    {
        get { return _AffectedDomains; }
    }

    public TileBase MineTile
    {
        get { return _MineTile; }
    }

    public MineRollRange[] RollRanges
    {
        get { return _RollRanges; }
    }


    public bool CanDeployOnTerrain(
        TerrainType Terrain
    )
    {
        for (
            int i = 0;
            i < _DeployableTerrainTypes.Length;
            i++
        )
        {
            if (_DeployableTerrainTypes[i] ==
                Terrain)
            {
                return true;
            }
        }

        return false;
    }


    public bool CanTriggerDomain(
        Domains Domain
    )
    {
        if (Domain == null)
        {
            return false;
        }


        for (
            int i = 0;
            i < _AffectedDomains.Length;
            i++
        )
        {
            if (_AffectedDomains[i] ==
                Domain)
            {
                return true;
            }
        }


        return false;
    }
}