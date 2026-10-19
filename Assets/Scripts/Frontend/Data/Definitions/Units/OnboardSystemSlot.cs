using System;
using UnityEngine;

// Embedded Serializable Data - Not A ScriptableObject
// Defines One Onboard System Installation On A Unit
[Serializable]
public class OnboardSystemSlot
{
    [ScriptableObjectPicker]
    [SerializeField] private OnboardSystemDefinition _System;

    [SerializeField]
    [Min(0)]
    private int _MaxCharges;

    // Used Only By DeploymentSystemDefinition Installations
    [SerializeField] private DeploymentUnitSlot[] _DeploymentLoadout;


    public OnboardSystemDefinition System
    {
        get { return _System; }
    }

    public int MaxCharges
    {
        get { return _MaxCharges; }
    }

    public DeploymentUnitSlot[] DeploymentLoadout
    {
        get { return _DeploymentLoadout; }
    }
}