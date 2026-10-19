using System;
using UnityEngine;

// Embedded Serializable Data - Not A ScriptableObject
// Defines One Unit Type And Quantity In A GameMode Starting Roster
[Serializable]
public class StartingUnitEntry
{
    [ScriptableObjectPicker]
    [SerializeField] private UnitDefinition _Unit;

    [Min(1)]
    [SerializeField] private int _Count = 1;


    public UnitDefinition Unit
    {
        get
        {
            return _Unit;
        }
    }


    public int Count
    {
        get
        {
            return _Count;
        }
    }
}