using System;
using UnityEngine;

// Embedded Serializable Effect Modifier - Not A ScriptableObject
// Modifies Attack Rolls Made Against The Affected Unit
[Serializable]
public class IncomingAttackRollModifier : EffectModifier
{
    [SerializeField] private int _Amount;


    public int Amount
    {
        get { return _Amount; }
    }
}