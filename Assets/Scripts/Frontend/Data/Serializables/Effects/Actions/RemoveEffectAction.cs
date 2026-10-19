using System;
using UnityEngine;

// Embedded Serializable Effect Action - Not A ScriptableObject
// Removes A Specific Active Effect From The Target Unit
[Serializable]
public class RemoveEffectAction : EffectAction
{
    [ScriptableObjectPicker]
    [SerializeField] private EffectDefinition _EffectToRemove;


    public EffectDefinition EffectToRemove
    {
        get { return _EffectToRemove; }
    }
}