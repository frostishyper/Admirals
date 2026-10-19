using System;
using UnityEngine;

// Embedded Serializable Data - Not A ScriptableObject
// Defines One Weapon Installation On A Unit
[Serializable]
public class WeaponSlot
{
    [ScriptableObjectPicker]
    [SerializeField] private WeaponDefinition _Weapon;

    [SerializeField]
    [Min(0)]
    private int _MaxCharges;


    public WeaponDefinition Weapon
    {
        get { return _Weapon; }
    }

    public int MaxCharges
    {
        get { return _MaxCharges; }
    }
}