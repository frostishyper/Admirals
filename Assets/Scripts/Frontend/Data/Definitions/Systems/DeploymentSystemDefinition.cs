using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
// Defines An Onboard System Capable Of Deploying Compatible Units
[CreateAssetMenu(
    fileName = "DeploymentSystem_New",
    menuName = "Admirals/Deployment System Definition"
)]
public class DeploymentSystemDefinition : OnboardSystemDefinition
{
    // A Unit Is Compatible If Its Unit Type Matches Any Accepted Type
    [ScriptableObjectPicker]
    [SerializeField] private UnitTypeDefinition[] _AcceptedUnitTypes;

    [ScriptableObjectPicker]
    [SerializeField] private EffectDefinition _DeploymentEffect;


    public UnitTypeDefinition[] AcceptedUnitTypes
    {
        get { return _AcceptedUnitTypes; }
    }

    public EffectDefinition DeploymentEffect
    {
        get { return _DeploymentEffect; }
    }


    public bool AcceptsUnit(UnitDefinition Unit)
    {
        if (Unit == null ||
            Unit.UnitType == null ||
            _AcceptedUnitTypes == null)
        {
            return false;
        }


        foreach (UnitTypeDefinition AcceptedType in _AcceptedUnitTypes)
        {
            if (AcceptedType == Unit.UnitType)
            {
                return true;
            }
        }


        return false;
    }
}