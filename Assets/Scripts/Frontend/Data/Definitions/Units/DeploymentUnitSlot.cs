using System;
using UnityEngine;

// Embedded Serializable Data - Not A ScriptableObject
// Defines One Unit Type Carried By A Specific Deployment System Installation
[Serializable]
public class DeploymentUnitSlot
{
    [SerializeField] private UnitDefinition _Unit;

    [SerializeField]
    [Min(1)]
    private int _Count = 1;


    public UnitDefinition Unit
    {
        get { return _Unit; }
    }

    public int Count
    {
        get { return _Count; }
    }
}