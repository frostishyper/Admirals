using System;
using UnityEngine;

// Serializable Roll Range Used By WeaponDefinition
// Supplies Offensive Result Data To Combat Resolution
[Serializable]
public class WeaponRollRange
{
    [SerializeField] private string _ResultName;

    [SerializeField]
    [Range(1, 20)]
    private int _MinimumRoll = 1;

    [SerializeField]
    [Range(1, 20)]
    private int _MaximumRoll = 20;

    [SerializeField]
    [Min(0)]
    private int _Damage;

    [SerializeField] private bool _IsMiss;

    // Additional Properties Carried By This Attack Result
    [SerializeField] private AttackProperty[] _AttackProperties;


    public string ResultName
    {
        get { return _ResultName; }
    }

    public int MinimumRoll
    {
        get { return _MinimumRoll; }
    }

    public int MaximumRoll
    {
        get { return _MaximumRoll; }
    }

    public int Damage
    {
        get { return _Damage; }
    }

    public bool IsMiss
    {
        get { return _IsMiss; }
    }

    public AttackProperty[] AttackProperties
    {
        get { return _AttackProperties; }
    }
}