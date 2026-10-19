using System;
using UnityEngine;

// Embedded Serializable Effect Modifier - Not A ScriptableObject
// Modifies Attack Rolls Made By The Affected Unit
[Serializable]
public class OutgoingAttackRollModifier : EffectModifier
{
    [SerializeField] private int _Amount;


    public int Amount
    {
        get { return _Amount; }
    }
}