using System;
using UnityEngine;

// Embedded Serializable Effect Action - Not A ScriptableObject
// Deals Health Damage When The Effect Action Executes
[Serializable]
public class DealDamageAction : EffectAction
{
    [SerializeField]
    [Min(0)]
    private int _Damage;

    public int Damage
    {
        get { return _Damage; }
    }
}